namespace KatiesGarden.Models.Shop;

public record OrderLineDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ProductImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);
