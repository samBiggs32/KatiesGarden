namespace KatiesGarden.Models.Entities;

public class StripeProcessedEvent
{
    public string EventId { get; set; } = string.Empty; // Stripe evt_xxx — PK
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
