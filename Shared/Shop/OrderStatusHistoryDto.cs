namespace KatiesGarden.Models.Shop;

public record OrderStatusHistoryDto(
    string Status,
    string? Note,
    DateTime ChangedAt);
