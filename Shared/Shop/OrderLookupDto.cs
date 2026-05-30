namespace KatiesGarden.Models.Shop;

public record OrderLookupDto(
    string OrderNumber,
    decimal Total,
    decimal DeliveryFee,
    string DeliveryType,
    DateTime CreatedAt,
    List<OrderLookupLineDto> Lines);

public record OrderLookupLineDto(string ProductName, int Quantity, decimal LineTotal);
