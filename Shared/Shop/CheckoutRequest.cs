using KatiesGarden.Models.Entities;

namespace KatiesGarden.Models.Shop;

public class CheckoutRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DeliveryType DeliveryType { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryPostcode { get; set; }
    public string? Notes { get; set; }
    public List<CartItemRequest> Items { get; set; } = [];
}

public class CartItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
