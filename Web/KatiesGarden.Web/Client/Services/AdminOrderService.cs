using KatiesGarden.Models.Shop;
using System.Net.Http.Json;

namespace KatiesGarden.Web.Client.Services;

public class AdminOrderService(HttpClient http)
{
    public Task<List<OrderSummaryDto>?> GetOrdersAsync(string? status = null)
    {
        var url = string.IsNullOrWhiteSpace(status)
            ? "api/manage/orders"
            : $"api/manage/orders?status={Uri.EscapeDataString(status)}";
        return http.GetFromJsonAsync<List<OrderSummaryDto>>(url);
    }

    public async Task<OrderDetailDto?> GetOrderAsync(Guid id)
    {
        var response = await http.GetAsync($"api/manage/orders/{id}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<OrderDetailDto>()
            : null;
    }

    public Task<HttpResponseMessage> UpdateStatusAsync(Guid id, string status)
        => http.PatchAsJsonAsync($"api/manage/orders/{id}/status", new UpdateOrderStatusRequest(status));

    public Task<HttpResponseMessage> UpdateNotesAsync(Guid id, string? notes)
        => http.PutAsJsonAsync($"api/manage/orders/{id}/notes", new { notes });
}
