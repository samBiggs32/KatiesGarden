using KatiesGarden.Models.Auth;
using Microsoft.Azure.Functions.Worker.Http;

namespace KatiesGarden.Api.Auth;

public static class SwaAuth
{
    public static ClientPrincipal? GetPrincipal(HttpRequestData req)
    {
        if (!req.Headers.TryGetValues("x-ms-client-principal", out var values))
            return null;

        return ClientPrincipal.Decode(values.First());
    }

    public static bool IsAdmin(HttpRequestData req)
        => GetPrincipal(req)?.IsAdmin ?? false;

    public static string? GetUserId(HttpRequestData req)
        => GetPrincipal(req)?.UserId;
}
