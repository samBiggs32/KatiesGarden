namespace KatiesGarden.Models.Shop;

public record ProductDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int? StockQuantity { get; init; }
    public bool IsAvailable { get; init; }
    public bool CanLocalDeliver { get; init; }
    public string[] ImageUrls { get; init; } = [];
    public string? HowToBuyNote { get; init; }
    public Guid? CollectionId { get; init; }
    public string? CollectionTitle { get; init; }
    public string? CollectionSlug { get; init; }
    public int DisplayOrder { get; init; }
}
