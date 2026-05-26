namespace KatiesGarden.Models.Shop;

public record DeliverySettingsDto(
    decimal LocalDeliveryFee,
    decimal? FreeDeliveryThreshold,
    string DeliveryAreaDescription,
    string CollectionAddress,
    string CollectionInstructions);
