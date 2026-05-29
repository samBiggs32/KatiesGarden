using KatiesGarden.Web.Client.Models;
using System.Net.Http.Json;

namespace KatiesGarden.Web.Client.Services;

public class AuthService(HttpClient http)
{
    private ClientPrincipal? _principal;
    private bool _loaded;

    public async Task<ClientPrincipal?> GetPrincipalAsync()
    {
        if (_loaded) return _principal;
        try
        {
            var wrapper = await http.GetFromJsonAsync<ClientPrincipalWrapper>("/.auth/me");
            _principal = wrapper?.ClientPrincipal;
        }
        catch
        {
            _principal = null;
        }
        _loaded = true;
        return _principal;
    }

    public async Task<bool> IsAdminAsync()
    {
        var principal = await GetPrincipalAsync();
        return principal?.UserRoles?.Contains("admin", StringComparer.OrdinalIgnoreCase) ?? false;
    }
}
