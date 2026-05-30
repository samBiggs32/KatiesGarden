namespace KatiesGarden.Models.Shop;

public record CollectionDetailDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? CoverImageUrl { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public List<ProductSummaryDto> Products { get; init; } = [];
}
