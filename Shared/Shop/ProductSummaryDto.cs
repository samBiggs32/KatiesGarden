namespace KatiesGarden.Models.Shop;

public record ProductSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    int? StockQuantity,
    bool IsAvailable,
    bool CanLocalDeliver,
    string? CoverImageUrl,
    int DisplayOrder,
    int ImageCount = 0);
