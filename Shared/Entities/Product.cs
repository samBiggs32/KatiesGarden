namespace KatiesGarden.Models.Entities;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? StockQuantity { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool CanLocalDeliver { get; set; } = true;
    public string[] ImageUrls { get; set; } = [];
    public Guid? CollectionId { get; set; }
    public Collection? Collection { get; set; }
    public string? HowToBuyNote { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? StripeProductId { get; set; }
    public string? StripePriceId { get; set; }
}
