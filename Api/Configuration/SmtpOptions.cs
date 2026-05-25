namespace KatiesGarden.Api.Configuration;

// Typed SMTP configuration. Bound from flat environment variables in
// Program.cs because Azure Functions reads local.settings.json Values
// and Azure SWA app settings as a flat IConfiguration.
public class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    // Optional override. Falls back to Username if not set — some
    // providers (e.g. SendGrid) use "apikey" as the SMTP username, so
    // it can't double as the visible From address.
    public string? SenderEmail { get; set; }

    public string RecipientEmail { get; set; } = "";

    public string EffectiveSenderEmail =>
        string.IsNullOrWhiteSpace(SenderEmail) ? Username : SenderEmail;
}
