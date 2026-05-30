using KatiesGarden.Models.Auth;
using Microsoft.Azure.Functions.Worker.Http;

namespace KatiesGarden.Api.Auth;

public static class SwaAuth
{
    // When ASPNETCORE_ENVIRONMENT=Development the app runs via Aspire on localhost.
    // The SWA /.auth/* layer doesn't exist there, so no x-ms-client-principal header
    // is ever injected. Bypass the check so the admin API is usable locally without
    // the SWA CLI. In production SWA never sets this variable, so the bypass is inert.
    private static readonly bool _isDev =
        string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Development",
            StringComparison.OrdinalIgnoreCase);

    public static ClientPrincipal? GetPrincipal(HttpRequestData req)
    {
        if (!req.Headers.TryGetValues("x-ms-client-principal", out var values))
            return null;

        return ClientPrincipal.Decode(values.First());
    }

    public static bool IsAdmin(HttpRequestData req) =>
        _isDev || (GetPrincipal(req)?.IsAdmin ?? false);
}
