namespace KatiesGarden.Web.Client.Models;

public class CartItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? ImageUrl { get; set; }
    public bool CanLocalDeliver { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}
