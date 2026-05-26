using System.Text.Json.Serialization;

namespace KatiesGarden.Web.Client.Models;

public class ClientPrincipalWrapper
{
    [JsonPropertyName("clientPrincipal")]
    public ClientPrincipal? ClientPrincipal { get; set; }
}

public class ClientPrincipal
{
    [JsonPropertyName("identityProvider")]
    public string IdentityProvider { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("userDetails")]
    public string UserDetails { get; set; } = string.Empty;

    [JsonPropertyName("userRoles")]
    public string[] UserRoles { get; set; } = [];
}
