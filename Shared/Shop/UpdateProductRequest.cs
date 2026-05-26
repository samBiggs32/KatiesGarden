namespace KatiesGarden.Models.Shop;

public class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? StockQuantity { get; set; }
    public bool IsAvailable { get; set; }
    public bool CanLocalDeliver { get; set; }
    public string[] ImageUrls { get; set; } = [];
    public Guid? CollectionId { get; set; }
    public string? HowToBuyNote { get; set; }
    public int DisplayOrder { get; set; }
}
