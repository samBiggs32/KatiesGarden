namespace KatiesGarden.Models.Shop;

public record UpdateOrderStatusRequest(string Status, string? Note = null);
