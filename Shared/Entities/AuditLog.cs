namespace KatiesGarden.Models.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? ActorEmail { get; set; }
    public string? ActorName { get; set; }
    public string? Details { get; set; }
}
