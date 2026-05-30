namespace KatiesGarden.Models.Shop;

public record ProductSearchResultDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    int? StockQuantity,
    bool IsAvailable,
    bool CanLocalDeliver,
    string? ImageUrl,
    int DisplayOrder,
    string? CollectionTitle,
    string? CollectionSlug);
