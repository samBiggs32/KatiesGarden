using Azure.Storage.Blobs;
using FluentValidation;
using KatiesGarden.Api.Configuration;
using KatiesGarden.Api.Data;
using KatiesGarden.Api.Email;
using KatiesGarden.Api.Services;
using KatiesGarden.Models;
using KatiesGarden.Models.Shop;
using KatiesGarden.Models.Validators;
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

        // Database — optional; endpoints that need it return 503 when not configured
        var dbUrl = config["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(dbUrl))
            services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(dbUrl));

        services.AddHttpClient();

        // Validators
        services.AddSingleton<IValidator<ContactUsForm>, ContactUsFormValidator>();
        services.AddSingleton<IValidator<SubscribeRequest>, SubscribeRequestValidator>();
        services.AddSingleton<IValidator<CreateProductRequest>, CreateProductRequestValidator>();
        services.AddSingleton<IValidator<CreateCollectionRequest>, CreateCollectionRequestValidator>();
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

        // Azure Blob Storage — only registered when a connection string is present so
        // GetService<BlobServiceClient>() returns null (not a nullable singleton) when unconfigured
        var storageConn = config["AZURE_STORAGE_CONNECTION_STRING"];
        if (!string.IsNullOrWhiteSpace(storageConn))
            services.AddSingleton(new BlobServiceClient(storageConn));

        // Push notifications
        services.AddScoped<IPushNotificationService, PushNotificationService>();
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
    var db = scope.ServiceProvider.GetService<AppDbContext>();
    if (db is null)
    {
        log.LogWarning("DATABASE_URL not set — all database-backed endpoints unavailable");
    }
    else
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            await db.Database.ExecuteSqlRawAsync(SqlMigrations.EnsureNewTablesExist);
            log.LogInformation("Database schema ready");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Database initialisation failed — DB-backed endpoints will return 500");
        }
    }

    log.LogInformation("Startup complete — listening for requests");
}

await host.RunAsync();
