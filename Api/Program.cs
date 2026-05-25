using FluentValidation;
using KatiesGarden.Api.Configuration;
using KatiesGarden.Api.Data;
using KatiesGarden.Api.Email;
using KatiesGarden.Models;
using KatiesGarden.Models.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var dbUrl = context.Configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(dbUrl))
            services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(dbUrl));

        services.AddHttpClient();

        services.AddSingleton<IValidator<ContactUsForm>, ContactUsFormValidator>();
        services.AddSingleton<IValidator<SubscribeRequest>, SubscribeRequestValidator>();

        // SMTP — required for the contact form. Fail at host startup, not
        // on the first request, when a key is missing or empty.
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

        // Brevo REST — optional. Subscribe endpoint degrades to "DB only"
        // when these are absent, so no Validate() calls here.
        services.AddOptions<BrevoOptions>()
            .Configure<IConfiguration>((opts, config) =>
            {
                opts.ApiKey = config["BREVO_API_KEY"];
                opts.ListId = int.TryParse(config["BREVO_LIST_ID"], out var id) ? id : null;
            });

        services.AddSingleton<IEmailSender, MailKitEmailSender>();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetService<AppDbContext>();
        db?.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database initialisation failed — subscribe endpoint will be unavailable");
    }
}

await host.RunAsync();
