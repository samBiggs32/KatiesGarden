using KatiesGarden.Models.Shop;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace KatiesGarden.Web.Client.Services;

public class AdminProductService(HttpClient http)
{
    public Task<List<ProductSummaryDto>?> GetProductsAsync()
        => http.GetFromJsonAsync<List<ProductSummaryDto>>("api/admin/products");

    public Task<ProductDetailDto?> GetProductAsync(Guid id)
        => http.GetFromJsonAsync<ProductDetailDto>($"api/admin/products/{id}");

    public Task<List<CollectionSummaryDto>?> GetCollectionsAsync()
        => http.GetFromJsonAsync<List<CollectionSummaryDto>>("api/admin/collections");

    public Task<CollectionDetailDto?> GetCollectionAsync(Guid id)
        => http.GetFromJsonAsync<CollectionDetailDto>($"api/admin/collections/{id}");

    public async Task<ProductSummaryDto?> CreateProductAsync(CreateProductRequest request)
    {
        var response = await http.PostAsJsonAsync("api/admin/products", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ProductSummaryDto>()
            : null;
    }

    public async Task<ProductSummaryDto?> UpdateProductAsync(Guid id, UpdateProductRequest request)
    {
        var response = await http.PutAsJsonAsync($"api/admin/products/{id}", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ProductSummaryDto>()
            : null;
    }

    public Task<HttpResponseMessage> DeleteProductAsync(Guid id)
        => http.DeleteAsync($"api/admin/products/{id}");

    public async Task<CollectionSummaryDto?> CreateCollectionAsync(CreateCollectionRequest request)
    {
        var response = await http.PostAsJsonAsync("api/admin/collections", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CollectionSummaryDto>()
            : null;
    }

    public async Task<CollectionSummaryDto?> UpdateCollectionAsync(Guid id, UpdateCollectionRequest request)
    {
        var response = await http.PutAsJsonAsync($"api/admin/collections/{id}", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CollectionSummaryDto>()
            : null;
    }

    public Task<HttpResponseMessage> DeleteCollectionAsync(Guid id)
        => http.DeleteAsync($"api/admin/collections/{id}");

    public async Task<DeliverySettingsDto?> GetDeliverySettingsAsync()
        => await http.GetFromJsonAsync<DeliverySettingsDto>("api/admin/delivery-settings");

    public async Task<DeliverySettingsDto?> UpdateDeliverySettingsAsync(DeliverySettingsDto settings)
    {
        var response = await http.PutAsJsonAsync("api/admin/delivery-settings", new
        {
            settings.LocalDeliveryFee,
            settings.FreeDeliveryThreshold,
            settings.DeliveryAreaDescription,
            settings.CollectionAddress,
            settings.CollectionInstructions
        });
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<DeliverySettingsDto>()
            : null;
    }

    public async Task<string?> UploadImageAsync(Stream imageStream, string contentType)
    {
        using var content = new StreamContent(imageStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var response = await http.PostAsync("api/admin/images", content);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<ImageUploadResponse>();
        return result?.Url;
    }

    private record ImageUploadResponse(string Url);
}
