using FluentAssertions;
using KatiesGarden.Models.Shop;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace KatiesGarden.Tests.Api;

[Trait("Category", "Integration")]
[Collection("AspireApi")]
public class AdminCollectionApiTests(AspireApiFixture fixture)
{
    [Fact]
    public async Task ListCollections_Unauthenticated_Returns401()
    {
        var response = await fixture.HttpClient.GetAsync("/api/admin/collections");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListCollections_Admin_IncludesInactive()
    {
        using var admin = fixture.CreateAdminClient();

        // Create an inactive collection so we can confirm admin sees it
        var created = await CreateCollection(admin);
        // Deactivate it
        await admin.DeleteAsync($"/api/admin/collections/{created.Id}");

        var resp = await admin.GetAsync("/api/admin/collections");
        var list = await resp.Content.ReadFromJsonAsync<List<CollectionSummaryDto>>();

        list.Should().NotBeNull();
        list!.Any(c => c.Id == created.Id && !c.IsActive)
            .Should().BeTrue("admin listing must include inactive collections");
    }

    [Fact]
    public async Task GetUpdateDeleteCollection_Admin_FullLifecycle()
    {
        using var admin = fixture.CreateAdminClient();

        var created = await CreateCollection(admin);

        // Get detail
        var getResp = await admin.GetAsync($"/api/admin/collections/{created.Id}");
        getResp.IsSuccessStatusCode.Should().BeTrue();
        var detail = await getResp.Content.ReadFromJsonAsync<CollectionDetailDto>();
        detail!.Id.Should().Be(created.Id);

        // Update — change title and toggle inactive
        var update = new UpdateCollectionRequest
        {
            Title = $"Updated {Guid.NewGuid():N}",
            Description = "Updated body",
            IsActive = false,
            StartDate = DateTime.UtcNow.AddDays(-1),
            DisplayOrder = 99
        };
        var putResp = await admin.PutAsJsonAsync($"/api/admin/collections/{created.Id}", update);
        putResp.IsSuccessStatusCode.Should().BeTrue();
        var updated = await putResp.Content.ReadFromJsonAsync<CollectionSummaryDto>();
        updated!.IsActive.Should().BeFalse();

        // Delete = soft delete (deactivate)
        var deleteResp = await admin.DeleteAsync($"/api/admin/collections/{created.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateCollection_Admin_InvalidPayload_ReturnsBadRequest()
    {
        using var admin = fixture.CreateAdminClient();
        var body = new CreateCollectionRequest { Title = "", Description = "" };

        var resp = await admin.PostAsJsonAsync("/api/admin/collections", body);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<CollectionSummaryDto> CreateCollection(HttpClient admin)
    {
        var body = new CreateCollectionRequest
        {
            Title = $"Test collection {Guid.NewGuid():N}",
            Description = "Seeded",
            StartDate = DateTime.UtcNow.AddDays(-1),
            DisplayOrder = 1
        };
        var resp = await admin.PostAsJsonAsync("/api/admin/collections", body);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await resp.Content.ReadFromJsonAsync<CollectionSummaryDto>())!;
    }
}
