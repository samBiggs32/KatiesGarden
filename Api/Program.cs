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
    .ConfigureServices((context, services) =>
    {
        var dbUrl = context.Configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(dbUrl))
            services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(dbUrl));

        services.AddHttpClient();

        // Existing validators
        services.AddSingleton<IValidator<ContactUsForm>, ContactUsFormValidator>();
        services.AddSingleton<IValidator<SubscribeRequest>, SubscribeRequestValidator>();

        // Shop validators
        services.AddSingleton<IValidator<CreateProductRequest>, CreateProductRequestValidator>();
        services.AddSingleton<IValidator<CreateCollectionRequest>, CreateCollectionRequestValidator>();
        services.AddSingleton<IValidator<CheckoutRequest>, CheckoutRequestValidator>();

        // SMTP — required, fail at startup if missing
        services.AddOptions<SmtpOptions>()
            .Configure<IConfiguration>((opts, config) =>
            {
                opts.Host = config["SMTP_HOST"] ?? "";
                opts.Port = int.TryParse(config["SMTP_PORT"], out var p) ? p : 587;
                opts.Username = config["SMTP_USERNAME"] ?? "";
                opts.Password = config["SMTP_PASSWORD"] ?? "";
                opts.SenderEmail = config["SENDER_EMAIL"];
                opts.RecipientEmail = config["RECIPIENT_EMAIL"] ?? "";
            })
            .Validate(o => !string.IsNullOrWhiteSpace(o.Host), "SMTP_HOST must be set")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Username), "SMTP_USERNAME must be set")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Password), "SMTP_PASSWORD must be set")
            .Validate(o => !string.IsNullOrWhiteSpace(o.RecipientEmail), "RECIPIENT_EMAIL must be set")
            .ValidateOnStart();

        // Brevo REST — optional
        services.AddOptions<BrevoOptions>()
            .Configure<IConfiguration>((opts, config) =>
            {
                opts.ApiKey = config["BREVO_API_KEY"];
                opts.ListId = int.TryParse(config["BREVO_LIST_ID"], out var id) ? id : null;
            });

        // Stripe — optional keys; missing keys cause graceful failures at call time, not startup
        services.AddOptions<StripeOptions>()
            .Configure<IConfiguration>((opts, config) =>
            {
                opts.SecretKey = config["STRIPE_SECRET_KEY"] ?? "";
                opts.WebhookSecret = config["STRIPE_WEBHOOK_SECRET"] ?? "";
                opts.SiteUrl = config["SITE_URL"] ?? "https://www.katiesgarden.uk";
            });

        services.AddSingleton<IEmailSender, MailKitEmailSender>();

        // Stripe services — singletons because they carry no per-request state
        services.AddSingleton<SessionService>();

        // Azure Blob Storage — optional, skipped gracefully if not configured
        var storageConn = context.Configuration["AZURE_STORAGE_CONNECTION_STRING"];
        if (!string.IsNullOrWhiteSpace(storageConn))
            services.AddSingleton(new BlobServiceClient(storageConn));
        else
            services.AddSingleton<BlobServiceClient?>(_ => null);

        // Push notification service — scoped because it uses AppDbContext
        services.AddScoped<IPushNotificationService, PushNotificationService>();
    })
    .Build();

// Set Stripe API key once at startup — avoids mutating a global static on every request
var stripeKey = host.Services.GetRequiredService<IConfiguration>()["STRIPE_SECRET_KEY"];
if (!string.IsNullOrWhiteSpace(stripeKey))
    StripeConfiguration.ApiKey = stripeKey;

using (var scope = host.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetService<AppDbContext>();
        if (db is not null)
        {
            db.Database.EnsureCreated();

            // Ensure store tables exist on pre-existing databases (EnsureCreated only creates
            // tables when the DB is brand new; this is idempotent and safe to run every cold start)
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SqlMigrations.EnsureNewTablesExist;
            await cmd.ExecuteNonQueryAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database initialisation failed — store and subscribe endpoints will be unavailable");
    }
}

await host.RunAsync();
