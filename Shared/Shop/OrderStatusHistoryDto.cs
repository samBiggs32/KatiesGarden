using KatiesGarden.Models.Entities;

namespace KatiesGarden.Models.Shop;

public record OrderStatusHistoryDto(
    OrderStatus Status,
    string? Note,
    DateTime ChangedAt);
