using KatiesGarden.Models.Entities;

namespace KatiesGarden.Models.Shop;

public record OrderLookupDto(
    string OrderNumber,
    decimal Total,
    decimal DeliveryFee,
    DeliveryType DeliveryType,
    DateTime CreatedAt,
    List<OrderLookupLineDto> Lines);

public record OrderLookupLineDto(string ProductName, int Quantity, decimal LineTotal);
