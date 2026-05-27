using FluentAssertions;
using KatiesGarden.Api.Data;
using KatiesGarden.Models.Shop;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace KatiesGarden.Tests.Api;

[Trait("Category", "Integration")]
[Collection("AspireApi")]
public class PushApiTests(AspireApiFixture fixture)
{
    [Fact]
    public async Task GetVapidPublicKey_Unauthenticated_Returns401()
    {
        var response = await fixture.HttpClient.GetAsync("/api/push/vapid-public-key");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetVapidPublicKey_Admin_Returns200()
    {
        using var admin = fixture.CreateAdminClient();

        var response = await admin.GetAsync("/api/push/vapid-public-key");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Subscribe_Admin_PersistsSubscription()
    {
        using var admin = fixture.CreateAdminClient();
        var endpoint = $"https://push.example.com/{Guid.NewGuid():N}";
        var body = new PushSubscribeRequest(endpoint, "p256dh-key", "auth-key");

        var response = await admin.PostAsJsonAsync("/api/push/subscribe", body);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var db = fixture.CreateDbContext();
        var saved = await db.PushSubscriptions.AsNoTracking().FirstOrDefaultAsync(s => s.Endpoint == endpoint);
        saved.Should().NotBeNull();
        saved!.P256dh.Should().Be("p256dh-key");
    }

    [Fact]
    public async Task Subscribe_Admin_SameEndpoint_UpsertsRatherThanDuplicates()
    {
        using var admin = fixture.CreateAdminClient();
        var endpoint = $"https://push.example.com/{Guid.NewGuid():N}";

        await admin.PostAsJsonAsync("/api/push/subscribe",
            new PushSubscribeRequest(endpoint, "first-p256dh", "first-auth"));
        await admin.PostAsJsonAsync("/api/push/subscribe",
            new PushSubscribeRequest(endpoint, "second-p256dh", "second-auth"));

        await using var db = fixture.CreateDbContext();
        var rows = await db.PushSubscriptions.Where(s => s.Endpoint == endpoint).ToListAsync();
        rows.Should().HaveCount(1);
        rows[0].P256dh.Should().Be("second-p256dh");
    }

    [Fact]
    public async Task Unsubscribe_Admin_RemovesSubscription()
    {
        using var admin = fixture.CreateAdminClient();
        var endpoint = $"https://push.example.com/{Guid.NewGuid():N}";

        await admin.PostAsJsonAsync("/api/push/subscribe",
            new PushSubscribeRequest(endpoint, "p256dh", "auth"));

        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/push/subscribe")
        {
            Content = JsonContent.Create(new PushSubscribeRequest(endpoint, "p256dh", "auth"))
        };
        var response = await admin.SendAsync(deleteRequest);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var db = fixture.CreateDbContext();
        (await db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint))
            .Should().BeNull();
    }
}
