using KatiesGarden.Models.Entities;
using KatiesGarden.Models.Shop;
using System.Net.Http.Json;

namespace KatiesGarden.Web.Client.Services;

public class AdminOrderService(HttpClient http)
{
    public Task<List<OrderSummaryDto>?> GetOrdersAsync(OrderStatus? status = null)
    {
        var url = status.HasValue
            ? $"api/manage/orders?status={Uri.EscapeDataString(status.Value.ToString())}"
            : "api/manage/orders";
        return http.GetFromJsonAsync<List<OrderSummaryDto>>(url);
    }

    public async Task<OrderDetailDto?> GetOrderAsync(Guid id)
    {
        var response = await http.GetAsync($"api/manage/orders/{id}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<OrderDetailDto>()
            : null;
    }

    public Task<HttpResponseMessage> UpdateStatusAsync(Guid id, OrderStatus status)
        => http.PatchAsJsonAsync($"api/manage/orders/{id}/status", new UpdateOrderStatusRequest(status));

    public Task<HttpResponseMessage> UpdateNotesAsync(Guid id, string? notes)
        => http.PutAsJsonAsync($"api/manage/orders/{id}/notes", new { notes });

    public Task<HttpResponseMessage> RefundAsync(Guid id)
        => http.PostAsync($"api/manage/orders/{id}/refund", null);
}
