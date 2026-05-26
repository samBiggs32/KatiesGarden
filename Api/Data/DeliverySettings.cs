namespace KatiesGarden.Api.Data;

public class DeliverySettings
{
    public int Id { get; set; } = 1;
    public decimal LocalDeliveryFee { get; set; } = 5.00m;
    public decimal? FreeDeliveryThreshold { get; set; }
    public string DeliveryAreaDescription { get; set; } = "Within 10 miles of Milverton, Somerset";
    public string CollectionAddress { get; set; } = "Milverton, Somerset, TA4";
    public string CollectionInstructions { get; set; } = string.Empty;
}
