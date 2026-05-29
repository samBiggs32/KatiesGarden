using KatiesGarden.Web.Client.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http.Json;

namespace KatiesGarden.Web.Client.Services;

public class AuthService(HttpClient http, IWebAssemblyHostEnvironment env)
{
    private ClientPrincipal? _principal;
    private bool _loaded;

    public async Task<ClientPrincipal?> GetPrincipalAsync()
    {
        if (_loaded) return _principal;

        // In local development (Aspire) the SWA /.auth/* layer doesn't exist.
        // Return a fake admin principal so the admin area is accessible without
        // the SWA CLI. In production env.IsDevelopment() is always false.
        if (env.IsDevelopment())
        {
            _principal = new ClientPrincipal
            {
                IdentityProvider = "dev",
                UserId = "local-dev",
                UserDetails = "local-dev",
                UserRoles = ["anonymous", "authenticated", "admin"]
            };
            _loaded = true;
            return _principal;
        }

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
