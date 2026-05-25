using KatiesGarden.Api;
using MailKit.Net.Smtp;
using MimeKit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                    ?? ["https://www.katiesgarden.uk"])
            .AllowAnyHeader()
            .AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

app.MapGet("/api/health", () => Results.Text("OK"));

app.MapPost("/api/contact", async (ContactFormRequest request, IConfiguration config, ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(request.FirstName) ||
        string.IsNullOrWhiteSpace(request.LastName) ||
        string.IsNullOrWhiteSpace(request.EmailAddress) ||
        string.IsNullOrWhiteSpace(request.ContactNumber) ||
        string.IsNullOrWhiteSpace(request.EmailSubject) ||
        string.IsNullOrWhiteSpace(request.EmailBody))
    {
        return Results.BadRequest("All fields are required.");
    }

    try
    {
        await SendEmailAsync(request, config);
        logger.LogInformation("Contact form email sent from {FirstName} {LastName}",
            request.FirstName, request.LastName);
        return Results.Ok("Message sent.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to send contact form email");
        return Results.Problem("Failed to send message. Please try again later.", statusCode: 500);
    }
});

app.Run();

static async Task SendEmailAsync(ContactFormRequest request, IConfiguration config)
{
    var host     = Require(config, "Smtp:Host");
    var port     = int.Parse(config["Smtp:Port"] ?? "587");
    var username = Require(config, "Smtp:Username");
    var password = Require(config, "Smtp:Password");
    var recipient = config["Smtp:RecipientEmail"] ?? "team@katiesgarden.uk";

    var message = new MimeMessage();
    message.From.Add(new MailboxAddress("Katie's Garden Website", username));
    message.To.Add(new MailboxAddress("Katie's Garden", recipient));
    message.ReplyTo.Add(new MailboxAddress($"{request.FirstName} {request.LastName}", request.EmailAddress));
    message.Subject = $"[Website Enquiry] {request.EmailSubject}";
    message.Body = new TextPart("plain")
    {
        Text = $"Dear Katie,\n\n{request.EmailBody}\n\n" +
               $"--\nMany thanks\n{request.FirstName} {request.LastName}\n" +
               $"Email: {request.EmailAddress}\nPhone: {request.ContactNumber}"
    };

    using var client = new SmtpClient();
    await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
    await client.AuthenticateAsync(username, password);
    await client.SendAsync(message);
    await client.DisconnectAsync(true);
}

static string Require(IConfiguration config, string key) =>
    config[key] ?? throw new InvalidOperationException($"Required config key '{key}' is not set.");
