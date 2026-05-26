namespace KatiesGarden.Models.Shop;

public class OrderDetailDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerFirstName { get; init; } = string.Empty;
    public string CustomerLastName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string DeliveryType { get; init; } = string.Empty;
    public string? DeliveryAddress { get; init; }
    public string? DeliveryPostcode { get; init; }
    public string? CustomerNotes { get; init; }
    public decimal Subtotal { get; init; }
    public decimal DeliveryFee { get; init; }
    public decimal Total { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? AdminNotes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<OrderLineDto> Lines { get; init; } = [];
}
