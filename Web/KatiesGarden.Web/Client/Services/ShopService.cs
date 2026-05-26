using KatiesGarden.Models.Shop;
using System.Net.Http.Json;

namespace KatiesGarden.Web.Client.Services;

public class ShopService(HttpClient http)
{
    public Task<List<CollectionSummaryDto>?> GetCollectionsAsync()
        => http.GetFromJsonAsync<List<CollectionSummaryDto>>("api/shop/collections");

    public Task<CollectionDetailDto?> GetCollectionAsync(string slug)
        => http.GetFromJsonAsync<CollectionDetailDto>($"api/shop/collections/{slug}");

    public Task<ProductDetailDto?> GetProductAsync(string slug)
        => http.GetFromJsonAsync<ProductDetailDto>($"api/shop/products/{slug}");

    public Task<DeliverySettingsDto?> GetDeliverySettingsAsync()
        => http.GetFromJsonAsync<DeliverySettingsDto>("api/shop/delivery-settings");
}
