namespace KatiesGarden.Api.Configuration;

public class PushOptions
{
    public string? PublicKey { get; set; }
    public string? PrivateKey { get; set; }
    public string Subject { get; set; } = "mailto:sales@katiesgarden.uk";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(PrivateKey);
}
