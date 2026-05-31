using KatiesGarden.Models.Shop;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace KatiesGarden.Web.Client.Services;

public class AdminProductService(HttpClient http)
{
    public Task<List<ProductSummaryDto>?> GetProductsAsync()
        => http.GetFromJsonAsync<List<ProductSummaryDto>>("api/manage/products");

    public async Task<ProductDetailDto?> GetProductAsync(Guid id)
    {
        var response = await http.GetAsync($"api/manage/products/{id}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ProductDetailDto>()
            : null;
    }

    public Task<List<CollectionSummaryDto>?> GetCollectionsAsync()
        => http.GetFromJsonAsync<List<CollectionSummaryDto>>("api/manage/collections");

    public async Task<CollectionDetailDto?> GetCollectionAsync(Guid id)
    {
        var response = await http.GetAsync($"api/manage/collections/{id}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CollectionDetailDto>()
            : null;
    }

    public async Task<ProductSummaryDto?> CreateProductAsync(CreateProductRequest request)
    {
        var response = await http.PostAsJsonAsync("api/manage/products", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ProductSummaryDto>()
            : null;
    }

    public async Task<ProductSummaryDto?> UpdateProductAsync(Guid id, UpdateProductRequest request)
    {
        var response = await http.PutAsJsonAsync($"api/manage/products/{id}", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ProductSummaryDto>()
            : null;
    }

    public Task<HttpResponseMessage> DeleteProductAsync(Guid id)
        => http.DeleteAsync($"api/manage/products/{id}");

    public async Task<CollectionSummaryDto?> CreateCollectionAsync(CreateCollectionRequest request)
    {
        var response = await http.PostAsJsonAsync("api/manage/collections", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CollectionSummaryDto>()
            : null;
    }

    public async Task<CollectionSummaryDto?> UpdateCollectionAsync(Guid id, UpdateCollectionRequest request)
    {
        var response = await http.PutAsJsonAsync($"api/manage/collections/{id}", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CollectionSummaryDto>()
            : null;
    }

    public Task<HttpResponseMessage> DeleteCollectionAsync(Guid id)
        => http.DeleteAsync($"api/manage/collections/{id}");

    public async Task<DeliverySettingsDto?> GetDeliverySettingsAsync()
        => await http.GetFromJsonAsync<DeliverySettingsDto>("api/manage/delivery-settings");

    public async Task<DeliverySettingsDto?> UpdateDeliverySettingsAsync(DeliverySettingsDto settings)
    {
        var response = await http.PutAsJsonAsync("api/manage/delivery-settings", settings);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<DeliverySettingsDto>()
            : null;
    }

    public async Task<string?> UploadImageAsync(Stream imageStream, string contentType)
    {
        using var content = new StreamContent(imageStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var response = await http.PostAsync("api/manage/images", content);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<ImageUploadResponse>();
        return result?.Url;
    }
}
