namespace KatiesGarden.Api.Configuration;

// Typed Brevo (REST API) configuration. Both values are optional —
// the subscribe endpoint degrades gracefully when they're absent
// (the DB write still happens; the Brevo contact-list sync is skipped).
public class BrevoOptions
{
    public string? ApiKey { get; set; }
    public int? ListId { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && ListId is not null;
}
