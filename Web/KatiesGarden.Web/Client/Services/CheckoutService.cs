using KatiesGarden.Models.Shop;
using System.Net.Http.Json;

namespace KatiesGarden.Web.Client.Services;

public class CheckoutService(HttpClient http)
{
    public async Task<string?> CreateSessionAsync(CheckoutRequest request)
    {
        var response = await http.PostAsJsonAsync("api/checkout/create-session", request);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<CheckoutSessionResponse>();
        return result?.Url;
    }

    private record CheckoutSessionResponse(string Url);
}
