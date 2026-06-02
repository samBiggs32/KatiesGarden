namespace KatiesGarden.Models.Shop;

public class ProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? StockQuantity { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool CanLocalDeliver { get; set; } = true;
    public string[] ImageUrls { get; set; } = [];
    public Guid? CollectionId { get; set; }
    public string? HowToBuyNote { get; set; }
    public int DisplayOrder { get; set; }
}
