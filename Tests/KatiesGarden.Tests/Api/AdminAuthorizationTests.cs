using FluentAssertions;
using System.Net;
using Xunit;

namespace KatiesGarden.Tests.Api;

/// <summary>
/// Cross-function guard: every admin endpoint must reject an authenticated user who
/// lacks the <c>admin</c> role, not only anonymous requests. The per-function test
/// files cover the anonymous (no principal) case; this covers the more dangerous
/// "logged-in but not admin" regression, where a broken <c>RequireAdmin</c> would
/// otherwise let any signed-in user into the management API.
/// </summary>
[Trait("Category", "Integration")]
[Collection("AspireApi")]
public class AdminAuthorizationTests(AspireApiFixture fixture)
{
    public static TheoryData<string> AdminGetEndpoints =>
    [
        "/api/manage/products",
        "/api/manage/collections",
        "/api/manage/orders",
        "/api/manage/delivery-settings",
        "/api/push/vapid-public-key",
    ];

    [Theory]
    [MemberData(nameof(AdminGetEndpoints))]
    public async Task AdminEndpoint_AuthenticatedNonAdmin_Returns401(string route)
    {
        using var nonAdmin = fixture.CreateNonAdminClient();

        var response = await nonAdmin.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "an authenticated user without the admin role must not reach {0}", route);
    }
}
