namespace KatiesGarden.Models.Shop;

public class CreateCollectionRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public int DisplayOrder { get; set; } = 0;
}
