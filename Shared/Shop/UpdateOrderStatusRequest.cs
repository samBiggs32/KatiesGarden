using KatiesGarden.Models.Entities;

namespace KatiesGarden.Models.Shop;

public record UpdateOrderStatusRequest(OrderStatus Status, string? Note = null);
