using KatiesGarden.Web.Client.Models;
using KatiesGarden.Web.Client.Models.Validators;
using MailKit.Net.Smtp;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Net;

namespace KatiesGarden.Api;

public class ContactFormFunction
{
    private readonly ILogger _logger;

    public ContactFormFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ContactFormFunction>();
    }

    [Function("ContactForm")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact")] HttpRequestData req)
    {
        ContactUsForm? request;
        try
        {
            request = await req.ReadFromJsonAsync<ContactUsForm>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialise contact form request");
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid request body.");
            return bad;
        }

        if (request is null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Request body is required.");
            return bad;
        }

        var validation = new ContactUsFormValidator().Validate(request);
        if (!validation.IsValid)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync(validation.Errors.First().ErrorMessage);
            return bad;
        }

        try
        {
            await SendEmailAsync(request);
            _logger.LogInformation("Contact form email sent from {FirstName} {LastName}", request.FirstName, request.LastName);
            return req.CreateResponse(HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact form email");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync("Failed to send message. Please try again later.");
            return error;
        }
    }

    private static async Task SendEmailAsync(ContactUsForm request)
    {
        var smtpHost = Env("SMTP_HOST");
        var smtpPort = int.Parse(Env("SMTP_PORT", "587"));
        var smtpUsername = Env("SMTP_USERNAME");
        var smtpPassword = Env("SMTP_PASSWORD");
        var senderEmail = Env("SENDER_EMAIL", smtpUsername);
        var recipientEmail = Env("RECIPIENT_EMAIL", "team@katiesgarden.uk");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Katie's Garden Website", senderEmail));
        message.To.Add(new MailboxAddress("Katie's Garden", recipientEmail));
        message.ReplyTo.Add(new MailboxAddress($"{request.FirstName} {request.LastName}", request.EmailAddress));
        message.Subject = $"[Website Enquiry] {request.EmailSubject}";
        message.Body = new TextPart("plain")
        {
            Text = $"Dear Katie,\n\n{request.EmailBody}\n\n" +
                   $"--\nMany thanks\n{request.FirstName} {request.LastName}" +
                   $"\nEmail: {request.EmailAddress}\nPhone: {request.ContactNumber}"
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(smtpUsername, smtpPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private static string Env(string key, string? fallback = null) =>
        Environment.GetEnvironmentVariable(key)
            ?? fallback
            ?? throw new InvalidOperationException($"Required environment variable '{key}' is not set.");
}
