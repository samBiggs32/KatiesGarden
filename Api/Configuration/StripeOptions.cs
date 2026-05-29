namespace KatiesGarden.Api.Configuration;

public class StripeOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string SiteUrl { get; set; } = "https://www.katiesgarden.uk";
}
