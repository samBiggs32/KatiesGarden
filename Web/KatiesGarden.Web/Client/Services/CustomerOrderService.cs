using KatiesGarden.Models.Shop;
using System.Net;
using System.Net.Http.Json;

namespace KatiesGarden.Web.Client.Services;

public class CustomerOrderService(HttpClient http)
{
    public async Task<List<CustomerOrderSummaryDto>> GetMyOrdersAsync()
    {
        try
        {
            return await http.GetFromJsonAsync<List<CustomerOrderSummaryDto>>("api/customer/orders")
                   ?? [];
        }
        catch { return []; }
    }

    public async Task<CustomerOrderDetailDto?> GetOrderAsync(string orderNumber)
    {
        try
        {
            return await http.GetFromJsonAsync<CustomerOrderDetailDto>($"api/customer/orders/{orderNumber}");
        }
        catch { return null; }
    }

    public async Task<CustomerOrderDetailDto?> LookupOrderAsync(string orderNumber, string email)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/customer/link-order",
                new { orderNumber, email });

            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CustomerOrderDetailDto>();
        }
        catch { return null; }
    }
}
