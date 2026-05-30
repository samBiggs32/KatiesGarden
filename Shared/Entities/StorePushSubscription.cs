namespace KatiesGarden.Models.Entities;

public class StorePushSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
