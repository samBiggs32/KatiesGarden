using KatiesGarden.Models.Shop;
using System.Net.Http.Json;

namespace KatiesGarden.Web.Client.Services;

public class AdminOrderService(HttpClient http)
{
    public Task<List<OrderSummaryDto>?> GetOrdersAsync(string? status = null)
    {
        var url = string.IsNullOrWhiteSpace(status)
            ? "api/admin/orders"
            : $"api/admin/orders?status={Uri.EscapeDataString(status)}";
        return http.GetFromJsonAsync<List<OrderSummaryDto>>(url);
    }

    public async Task<OrderDetailDto?> GetOrderAsync(Guid id)
    {
        var response = await http.GetAsync($"api/admin/orders/{id}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<OrderDetailDto>()
            : null;
    }

    public Task<HttpResponseMessage> UpdateStatusAsync(Guid id, string status)
        => http.PatchAsJsonAsync($"api/admin/orders/{id}/status", new UpdateOrderStatusRequest(status));

    public Task<HttpResponseMessage> UpdateNotesAsync(Guid id, string? notes)
        => http.PutAsJsonAsync($"api/admin/orders/{id}/notes", new { notes });
}
