namespace KatiesGarden.Models.Shop;

public record CollectionSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    string? CoverImageUrl,
    DateTime StartDate,
    DateTime? EndDate,
    int ProductCount,
    int DisplayOrder,
    bool IsActive = true);
