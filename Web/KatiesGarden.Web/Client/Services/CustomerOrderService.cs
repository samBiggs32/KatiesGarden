using KatiesGarden.Models.Shop;
using System.Net;
using System.Net.Http.Json;

namespace KatiesGarden.Web.Client.Services;

public class CustomerOrderService(HttpClient http)
{
    // Throws on a transient/server failure so the page can show a retryable error state
    // rather than masking it as "No orders yet".
    public async Task<List<CustomerOrderSummaryDto>> GetMyOrdersAsync() =>
        await http.GetFromJsonAsync<List<CustomerOrderSummaryDto>>("api/customer/orders") ?? [];

    public async Task<CustomerOrderDetailDto?> GetOrderAsync(string orderNumber)
    {
        try
        {
            return await http.GetFromJsonAsync<CustomerOrderDetailDto>($"api/customer/orders/{orderNumber}");
        }
        catch { return null; }
    }

    // Returns null only for a genuine "not found" (404). Transient/server failures throw so
    // the page can tell "no matching order" apart from "something went wrong, try again".
    public async Task<CustomerOrderDetailDto?> LookupOrderAsync(string orderNumber, string email, string total)
    {
        var response = await http.PostAsJsonAsync("api/customer/link-order",
            new { orderNumber, email, total });

        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerOrderDetailDto>();
    }
}
