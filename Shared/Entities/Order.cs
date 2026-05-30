namespace KatiesGarden.Models.Entities;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerFirstName { get; set; } = string.Empty;
    public string CustomerLastName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public DeliveryType DeliveryType { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryPostcode { get; set; }
    public string? CustomerNotes { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? StripeSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<OrderLine> Lines { get; set; } = [];
}

public enum DeliveryType
{
    Collection,
    LocalDelivery
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Processing,
    ReadyForCollection,
    Dispatched,
    Delivered,
    Cancelled,
    Refunded
}
