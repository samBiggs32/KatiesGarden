using FluentAssertions;
using KatiesGarden.Models.Shop;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace KatiesGarden.Tests.Api;

[Trait("Category", "Integration")]
[Collection("AspireApi")]
public class AdminProductApiTests(AspireApiFixture fixture)
{
    [Fact]
    public async Task ListProducts_Unauthenticated_Returns401()
    {
        var response = await fixture.HttpClient.GetAsync("/api/manage/products");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_Admin_PersistsAndReturnsSummary()
    {
        using var admin = fixture.CreateAdminClient();
        var name = $"Bug House {Guid.NewGuid():N}";
        var body = new CreateProductRequest
        {
            Name = name,
            Description = "Solid oak bug house",
            Price = 24.99m,
            StockQuantity = 10,
            DisplayOrder = 1
        };

        var response = await admin.PostAsJsonAsync("/api/manage/products", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<ProductSummaryDto>();
        dto.Should().NotBeNull();
        dto!.Name.Should().Be(name);
        dto.Price.Should().Be(24.99m);
    }

    [Fact]
    public async Task CreateProduct_Admin_InvalidPayload_ReturnsBadRequest()
    {
        using var admin = fixture.CreateAdminClient();
        var body = new CreateProductRequest { Name = "", Price = -1m };

        var response = await admin.PostAsJsonAsync("/api/manage/products", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUpdateDeleteProduct_Admin_FullLifecycle()
    {
        using var admin = fixture.CreateAdminClient();

        // Create
        var create = new CreateProductRequest
        {
            Name = $"Plant {Guid.NewGuid():N}",
            Description = "Initial description",
            Price = 5.50m,
            StockQuantity = 3,
            DisplayOrder = 7
        };
        var createResp = await admin.PostAsJsonAsync("/api/manage/products", create);
        var created = await createResp.Content.ReadFromJsonAsync<ProductSummaryDto>();
        created.Should().NotBeNull();

        // Get
        var getResp = await admin.GetAsync($"/api/manage/products/{created!.Id}");
        getResp.IsSuccessStatusCode.Should().BeTrue();
        var detail = await getResp.Content.ReadFromJsonAsync<ProductDetailDto>();
        detail!.DisplayOrder.Should().Be(7);

        // Update
        var update = new UpdateProductRequest
        {
            Name = create.Name,
            Description = "Updated description",
            Price = 6.50m,
            StockQuantity = 2,
            IsAvailable = true,
            CanLocalDeliver = true,
            DisplayOrder = 9
        };
        var updateResp = await admin.PutAsJsonAsync($"/api/manage/products/{created.Id}", update);
        updateResp.IsSuccessStatusCode.Should().BeTrue();
        var updated = await updateResp.Content.ReadFromJsonAsync<ProductSummaryDto>();
        updated!.Price.Should().Be(6.50m);

        // Verify DisplayOrder persisted on update (regression for the silent-reset bug)
        var refetch = await (await admin.GetAsync($"/api/manage/products/{created.Id}"))
            .Content.ReadFromJsonAsync<ProductDetailDto>();
        refetch!.DisplayOrder.Should().Be(9);

        // Delete
        var deleteResp = await admin.DeleteAsync($"/api/manage/products/{created.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDelete = await admin.GetAsync($"/api/manage/products/{created.Id}");
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProduct_UnknownId_ReturnsNotFound()
    {
        using var admin = fixture.CreateAdminClient();

        var response = await admin.GetAsync($"/api/manage/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Delivery settings ─────────────────────────────────────────────────

    [Fact]
    public async Task DeliverySettings_Admin_UpdateRoundTrip()
    {
        using var admin = fixture.CreateAdminClient();
        var update = new
        {
            LocalDeliveryFee = 7.50m,
            FreeDeliveryThreshold = (decimal?)50m,
            DeliveryAreaDescription = "Within 5 miles of Milverton",
            CollectionAddress = "1 Example Lane",
            CollectionInstructions = "Knock on the green door"
        };

        var putResp = await admin.PutAsJsonAsync("/api/manage/delivery-settings", update);
        putResp.IsSuccessStatusCode.Should().BeTrue();

        var getResp = await admin.GetAsync("/api/manage/delivery-settings");
        var dto = await getResp.Content.ReadFromJsonAsync<DeliverySettingsDto>();
        dto!.LocalDeliveryFee.Should().Be(7.50m);
        dto.CollectionAddress.Should().Be("1 Example Lane");
    }
}
