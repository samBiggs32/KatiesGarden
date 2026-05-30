namespace KatiesGarden.Models.Entities;

public class Subscriber
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public DateTimeOffset SubscribedAt { get; set; } = DateTimeOffset.UtcNow;
}
