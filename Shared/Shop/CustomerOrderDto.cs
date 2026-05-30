namespace KatiesGarden.Models.Shop;

public record CustomerOrderSummaryDto(
    Guid Id,
    string OrderNumber,
    decimal Total,
    string Status,
    string DeliveryType,
    DateTime CreatedAt);

public record CustomerOrderDetailDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerFirstName { get; init; } = string.Empty;
    public string CustomerLastName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string DeliveryType { get; init; } = string.Empty;
    public string? DeliveryAddress { get; init; }
    public string? DeliveryPostcode { get; init; }
    public decimal Subtotal { get; init; }
    public decimal DeliveryFee { get; init; }
    public decimal Total { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<OrderLineDto> Lines { get; init; } = [];
    public List<OrderStatusHistoryDto> StatusHistory { get; init; } = [];
}
