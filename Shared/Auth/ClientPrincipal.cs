using System.Text.Json;
using System.Text.Json.Serialization;

namespace KatiesGarden.Models.Auth;

public record ClientPrincipal(
    [property: JsonPropertyName("identityProvider")] string IdentityProvider,
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("userDetails")] string UserDetails,
    [property: JsonPropertyName("userRoles")] string[] UserRoles)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static ClientPrincipal? Decode(string base64)
    {
        try
        {
            var json = Convert.FromBase64String(base64);
            return JsonSerializer.Deserialize<ClientPrincipal>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public bool IsAdmin => UserRoles?.Contains("admin", StringComparer.OrdinalIgnoreCase) ?? false;
}
