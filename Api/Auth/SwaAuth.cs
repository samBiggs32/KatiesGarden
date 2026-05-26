using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KatiesGarden.Api.Auth;

public static class SwaAuth
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ClientPrincipal? GetPrincipal(HttpRequestData req)
    {
        if (!req.Headers.TryGetValues("x-ms-client-principal", out var values))
            return null;

        try
        {
            var encoded = values.First();
            var json = Convert.FromBase64String(encoded);
            return JsonSerializer.Deserialize<ClientPrincipal>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static bool IsAdmin(HttpRequestData req)
    {
        var principal = GetPrincipal(req);
        return principal?.UserRoles?.Contains("admin", StringComparer.OrdinalIgnoreCase) ?? false;
    }

    public static string? GetUserId(HttpRequestData req)
        => GetPrincipal(req)?.UserId;
}

public record ClientPrincipal(
    [property: JsonPropertyName("identityProvider")] string IdentityProvider,
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("userDetails")] string UserDetails,
    [property: JsonPropertyName("userRoles")] string[] UserRoles);
