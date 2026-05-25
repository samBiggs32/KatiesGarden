using KatiesGarden.Models.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace KatiesGarden.Api.Email;

public class MailKitEmailSender(IConfiguration config) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var host = Required("SMTP_HOST");
        var port = int.Parse(config["SMTP_PORT"] ?? "587");
        var username = Required("SMTP_USERNAME");
        var password = Required("SMTP_PASSWORD");

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(message.FromName, message.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToAddress));
        mime.ReplyTo.Add(new MailboxAddress(message.ReplyToName, message.ReplyToAddress));
        mime.Subject = message.Subject;
        mime.Body = new TextPart("plain") { Text = message.BodyText };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(username, password, cancellationToken);
        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private string Required(string key) =>
        config[key] ?? throw new InvalidOperationException($"Required configuration '{key}' is not set.");
}
