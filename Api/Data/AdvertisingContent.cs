namespace KatiesGarden.Api.Data;

public class AdvertisingContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public Guid? CollectionId { get; set; }
    public Guid[] FeaturedProductIds { get; set; } = [];
    public string[] GeneratedImageUrls { get; set; } = [];
    public string SuggestedCaption { get; set; } = string.Empty;
    public string Hashtags { get; set; } = string.Empty;
    public AdvertisingStatus Status { get; set; } = AdvertisingStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PostedAt { get; set; }
    // Meta API fields — populated when Phase 2b Meta integration is added
    public string? MetaCampaignId { get; set; }
    public string? MetaAdSetId { get; set; }
    public string? MetaAdId { get; set; }
    public string? MetaStatus { get; set; }
}

public enum AdvertisingStatus
{
    Draft,
    ReadyToPost,
    Posted,
    MetaActive
}
