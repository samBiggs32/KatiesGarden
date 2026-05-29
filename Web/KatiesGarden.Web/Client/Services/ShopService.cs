using KatiesGarden.Models.Shop;
using System.Net.Http.Json;

namespace KatiesGarden.Web.Client.Services;

public class ShopService(HttpClient http)
{
    public Task<List<CollectionSummaryDto>?> GetCollectionsAsync()
        => http.GetFromJsonAsync<List<CollectionSummaryDto>>("api/shop/collections");

    public async Task<CollectionDetailDto?> GetCollectionAsync(string slug)
    {
        var response = await http.GetAsync($"api/shop/collections/{slug}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CollectionDetailDto>()
            : null;
    }

    public async Task<ProductDetailDto?> GetProductAsync(string slug)
    {
        var response = await http.GetAsync($"api/shop/products/{slug}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ProductDetailDto>()
            : null;
    }

    public Task<DeliverySettingsDto?> GetDeliverySettingsAsync()
        => http.GetFromJsonAsync<DeliverySettingsDto>("api/shop/delivery-settings");
}
