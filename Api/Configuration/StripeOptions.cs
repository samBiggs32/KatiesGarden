namespace KatiesGarden.Api.Configuration;

public class StripeOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string SiteUrl { get; set; } = "https://www.katiesgarden.uk";

    // True only when a real key is present. The Aspire AppHost injects
    // "sk_test_placeholder" for local dev so the host can boot without a
    // Stripe account — that is treated as "not configured", not a failure,
    // so diagnostics/health stay green locally.
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SecretKey) &&
        !SecretKey.Contains("placeholder", StringComparison.OrdinalIgnoreCase);
}
