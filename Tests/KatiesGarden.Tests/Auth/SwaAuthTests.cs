using FluentAssertions;
using KatiesGarden.Models.Auth;
using System.Text;
using System.Text.Json;
using Xunit;

namespace KatiesGarden.Tests.Auth;

public class SwaAuthTests
{
    private static string Encode(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static string AdminPrincipal() => Encode(new
    {
        identityProvider = "github",
        userId = "user-123",
        userDetails = "testuser",
        userRoles = new[] { "anonymous", "authenticated", "admin" }
    });

    private static string NonAdminPrincipal() => Encode(new
    {
        identityProvider = "github",
        userId = "user-456",
        userDetails = "otheruser",
        userRoles = new[] { "anonymous", "authenticated" }
    });

    private static string EmptyRolesPrincipal() => Encode(new
    {
        identityProvider = "github",
        userId = "user-789",
        userDetails = "noroles",
        userRoles = Array.Empty<string>()
    });

    [Fact]
    public void Decode_ValidAdminHeader_ReturnsAdminPrincipal()
    {
        var principal = ClientPrincipal.Decode(AdminPrincipal());

        principal.Should().NotBeNull();
        principal!.UserId.Should().Be("user-123");
        principal.IdentityProvider.Should().Be("github");
        principal.UserRoles.Should().Contain("admin");
    }

    [Fact]
    public void Decode_AdminRoles_IsAdminTrue()
    {
        var principal = ClientPrincipal.Decode(AdminPrincipal());
        principal!.IsAdmin.Should().BeTrue();
    }

    [Fact]
    public void Decode_NonAdminRoles_IsAdminFalse()
    {
        var principal = ClientPrincipal.Decode(NonAdminPrincipal());
        principal!.IsAdmin.Should().BeFalse();
    }

    [Fact]
    public void Decode_EmptyRoles_IsAdminFalse()
    {
        var principal = ClientPrincipal.Decode(EmptyRolesPrincipal());
        principal!.IsAdmin.Should().BeFalse();
    }

    [Fact]
    public void Decode_InvalidBase64_ReturnsNull()
    {
        var result = ClientPrincipal.Decode("!!!not-valid-base64!!!");
        result.Should().BeNull();
    }

    [Fact]
    public void Decode_ValidBase64ButNotJson_ReturnsNull()
    {
        var notJson = Convert.ToBase64String(Encoding.UTF8.GetBytes("this is not json"));
        var result = ClientPrincipal.Decode(notJson);
        result.Should().BeNull();
    }

    [Fact]
    public void Decode_EmptyString_ReturnsNull()
    {
        var result = ClientPrincipal.Decode(string.Empty);
        result.Should().BeNull();
    }

    [Fact]
    public void Decode_AdminRoleCaseInsensitive_IsAdminTrue()
    {
        var encoded = Encode(new
        {
            identityProvider = "google",
            userId = "u1",
            userDetails = "u",
            userRoles = new[] { "Admin" }  // capital A
        });

        var principal = ClientPrincipal.Decode(encoded);
        principal!.IsAdmin.Should().BeTrue();
    }
}
