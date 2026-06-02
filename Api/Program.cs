using Azure.Storage.Blobs;
using FluentValidation;
using KatiesGarden.Api.Auditing;
using KatiesGarden.Api.Configuration;
using KatiesGarden.Api.Data;
using KatiesGarden.Models.Entities;
using KatiesGarden.Api.Email;
using KatiesGarden.Api.Services;
using KatiesGarden.Api.Telemetry;
using KatiesGarden.Models;
using KatiesGarden.Models.Shop;
using KatiesGarden.Models.Validators;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureLogging(logging =>
    {
        // AddConsole ensures startup errors reach stdout even before the
        // Functions host log pipeline is fully wired up.
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        // Application Insights — no-op when APPLICATIONINSIGHTS_CONNECTION_STRING is
        // absent (local dev), live telemetry in production without any code change.
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        // Scrub PII (emails, search terms) from telemetry before it leaves the process.
        services.AddSingleton<ITelemetryInitializer, PiiScrubbingInitializer>();

        // Database — AppDbContext is ALWAYS registered so every function that
        // depends on it can be constructed by the DI container. Most functions
        // inject AppDbContext non-nullably; if it were only registered when
        // DATABASE_URL is set, running the API standalone (no Aspire, no DB) would
        // fail the whole host with "Some services are not able to be constructed".
        // EF Core defers connecting until the first query, so a placeholder
        // connection string is harmless at startup — DB-backed endpoints simply
        // return 503/500 at call time when the database is unreachable.
        var dbUrl = config["DATABASE_URL"];
        services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(
            string.IsNullOrWhiteSpace(dbUrl)
                ? "Host=localhost;Database=katiesgarden_unconfigured"
                : dbUrl));

        services.AddHttpClient();

        // Validators
        services.AddSingleton<IValidator<ContactUsForm>, ContactUsFormValidator>();
        services.AddSingleton<IValidator<SubscribeRequest>, SubscribeRequestValidator>();
        services.AddSingleton<IValidator<ProductRequest>, ProductRequestValidator>();
        services.AddSingleton<IValidator<CollectionRequest>, CollectionRequestValidator>();
        services.AddSingleton<IValidator<CheckoutRequest>, CheckoutRequestValidator>();

        // SMTP — bound from env vars; send failures are caught and logged at call time,
        // not at startup, so missing/wrong credentials never prevent the host from starting.
        services.Configure<SmtpOptions>(opts =>
        {
            opts.Host = config["SMTP_HOST"] ?? "";
            opts.Port = int.TryParse(config["SMTP_PORT"], out var p) ? p : 587;
            opts.Username = config["SMTP_USERNAME"] ?? "";
            opts.Password = config["SMTP_PASSWORD"] ?? "";
            opts.SenderEmail = config["SENDER_EMAIL"];
            opts.RecipientEmail = config["RECIPIENT_EMAIL"] ?? "";
        });
        services.AddSingleton<IEmailSender, MailKitEmailSender>();

        // Brevo REST — optional; subscribe endpoint degrades gracefully when absent
        services.Configure<BrevoOptions>(opts =>
        {
            opts.ApiKey = config["BREVO_API_KEY"];
            opts.ListId = int.TryParse(config["BREVO_LIST_ID"], out var id) ? id : null;
        });

        // Stripe — optional; placeholder keys report "not_configured", real keys are
        // verified at call time (no startup validation that could mask other errors)
        services.Configure<StripeOptions>(opts =>
        {
            opts.SecretKey = config["STRIPE_SECRET_KEY"] ?? "";
            opts.WebhookSecret = config["STRIPE_WEBHOOK_SECRET"] ?? "";
            opts.SiteUrl = config["SITE_URL"] ?? "https://www.katiesgarden.uk";
        });
        // Stripe services — singletons because they carry no per-request state
        services.AddSingleton<SessionService>();
        services.AddSingleton<Stripe.RefundService>();

        // Azure Blob Storage — only registered when a connection string is present so
        // GetService<BlobServiceClient>() returns null (not a nullable singleton) when unconfigured
        var storageConn = config["AZURE_STORAGE_CONNECTION_STRING"];
        if (!string.IsNullOrWhiteSpace(storageConn))
            services.AddSingleton(new BlobServiceClient(storageConn));

        // VAPID / push notifications
        services.Configure<PushOptions>(opts =>
        {
            opts.PublicKey = config["VAPID_PUBLIC_KEY"];
            opts.PrivateKey = config["VAPID_PRIVATE_KEY"];
            opts.Subject = config["VAPID_SUBJECT"] ?? "mailto:sales@katiesgarden.uk";
        });
        services.AddScoped<IPushNotificationService, PushNotificationService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IOrderService, OrderService>();

        // Azure Blob Storage container config
        services.Configure<BlobOptions>(opts =>
        {
            opts.Container = config["AZURE_STORAGE_CONTAINER"] ?? "product-images";
        });
    })
    .Build();

// ── Startup diagnostics ────────────────────────────────────────────────────
// All checks run before host.RunAsync() so any misconfiguration is visible in
// the Aspire dashboard resource logs and in the terminal immediately on start.
using (var scope = host.Services.CreateScope())
{
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    log.LogInformation("Katie's Garden API starting up");

    // ── Stripe ──────────────────────────────────────────────────────────────
    var stripeKey = config["STRIPE_SECRET_KEY"] ?? "";
    if (string.IsNullOrWhiteSpace(stripeKey))
    {
        log.LogWarning("STRIPE_SECRET_KEY not set — Stripe endpoints unavailable");
    }
    else if (stripeKey.Contains("placeholder", StringComparison.OrdinalIgnoreCase))
    {
        log.LogInformation("Stripe: placeholder key (not_configured) — OK for local dev");
    }
    else
    {
        StripeConfiguration.ApiKey = stripeKey;
        log.LogInformation("Stripe: live key configured ({Prefix}...)", stripeKey[..Math.Min(8, stripeKey.Length)]);
    }

    // ── Blob Storage ────────────────────────────────────────────────────────
    var storageConn = config["AZURE_STORAGE_CONNECTION_STRING"] ?? "";
    if (string.IsNullOrWhiteSpace(storageConn))
        log.LogInformation("Blob Storage: not configured — image uploads unavailable");
    else
        log.LogInformation("Blob Storage: configured");

    // ── SMTP ────────────────────────────────────────────────────────────────
    var smtpHost = config["SMTP_HOST"] ?? "";
    var smtpUser = config["SMTP_USERNAME"] ?? "";
    if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser))
        log.LogWarning("SMTP_HOST or SMTP_USERNAME not set — email sending will fail");
    else
        log.LogInformation("SMTP: {Host} / {User}", smtpHost, smtpUser);

    // ── Database ────────────────────────────────────────────────────────────
    // MigrateAsync runs all pending EF Core migrations on startup. The initial
    // migration uses IF NOT EXISTS guards, so it is safe on pre-existing databases
    // that were created before migrations were introduced — they just get the
    // __EFMigrationsHistory table created and the InitialSchema row inserted.
    // Bounded by a hard 30-second timeout: an unreachable database must never hang
    // the host long enough for the Functions launcher to kill the process.
    //
    // DATABASE_URL_MIGRATE, when set, is a least-privilege connection string for
    // the kg_migrate role (DDL access). The runtime kg_app role in DATABASE_URL
    // has DML-only access and cannot run schema changes. See infra/sql/roles.sql.
    var dbUrl = config["DATABASE_URL"];
    if (string.IsNullOrWhiteSpace(dbUrl))
    {
        log.LogWarning("DATABASE_URL not set — database-backed endpoints will return 503/500. " +
            "Run the API via Aspire (dotnet run --project AppHost) to provision Postgres automatically.");
    }
    else
    {
        AppDbContext? ownedMigrateDb = null;
        try
        {
            var migrateUrl = config["DATABASE_URL_MIGRATE"];
            AppDbContext migrateDb;
            if (!string.IsNullOrWhiteSpace(migrateUrl))
            {
                // Use the DDL-privileged role for schema migrations only
                var migrateOpts = new DbContextOptionsBuilder<AppDbContext>()
                    .UseNpgsql(migrateUrl)
                    .Options;
                ownedMigrateDb = new AppDbContext(migrateOpts);
                migrateDb = ownedMigrateDb;
                log.LogInformation("Database: using DATABASE_URL_MIGRATE role for schema migration");
            }
            else
            {
                migrateDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            }

            using var dbInitTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await migrateDb.Database.MigrateAsync(dbInitTimeout.Token);
            log.LogInformation("Database schema up to date ({Migrations} migration(s) applied)",
                (await migrateDb.Database.GetAppliedMigrationsAsync(dbInitTimeout.Token)).Count());

            // Seed uses the runtime (kg_app) context from DI
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await CollectionSeeder.SeedAsync(db, log, dbInitTimeout.Token);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Database migration failed or timed out — host will still start; DB-backed endpoints will return 500 until the database is reachable");
        }
        finally
        {
            if (ownedMigrateDb is not null)
                await ownedMigrateDb.DisposeAsync();
        }
    }

    log.LogInformation("Startup complete — listening for requests");
}

await host.RunAsync();
