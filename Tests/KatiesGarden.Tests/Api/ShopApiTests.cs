using FluentAssertions;
using KatiesGarden.Api.Data;
using KatiesGarden.Models.Entities;
using KatiesGarden.Models.Shop;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace KatiesGarden.Tests.Api;

[Trait("Category", "Integration")]
[Collection("AspireApi")]
public class ShopApiTests(AspireApiFixture fixture)
{
    [Fact]
    public async Task GetCollections_ReturnsOnlyActiveCollections()
    {
        var ct = TestContext.Current.CancellationToken;
        var active = await SeedCollection(isActive: true, ct);
        var inactive = await SeedCollection(isActive: false, ct);

        var response = await fixture.HttpClient.GetAsync("/api/shop/collections", ct);
        response.IsSuccessStatusCode.Should().BeTrue();

        var dtos = await response.Content.ReadFromJsonAsync<List<CollectionSummaryDto>>(ct);
        dtos.Should().NotBeNull();
        dtos!.Any(c => c.Id == active.Id).Should().BeTrue();
        dtos.Any(c => c.Id == inactive.Id).Should().BeFalse("inactive collections must not leak to the shop");
    }

    [Fact]
    public async Task GetCollection_ExistingSlug_ReturnsCollectionWithProducts()
    {
        var ct = TestContext.Current.CancellationToken;
        var collection = await SeedCollection(ct: ct);
        var product = await SeedProduct(collectionId: collection.Id, ct: ct);

        var response = await fixture.HttpClient.GetAsync($"/api/shop/collections/{collection.Slug}", ct);
        response.IsSuccessStatusCode.Should().BeTrue();

        var dto = await response.Content.ReadFromJsonAsync<CollectionDetailDto>(ct);
        dto.Should().NotBeNull();
        dto!.Slug.Should().Be(collection.Slug);
        dto.Products.Should().Contain(p => p.Id == product.Id);
    }

    [Fact]
    public async Task GetCollection_UnknownSlug_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await fixture.HttpClient.GetAsync($"/api/shop/collections/does-not-exist-{Guid.NewGuid():N}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProduct_ExistingSlug_ReturnsDetail()
    {
        var ct = TestContext.Current.CancellationToken;
        var product = await SeedProduct(ct: ct);

        var response = await fixture.HttpClient.GetAsync($"/api/shop/products/{product.Slug}", ct);
        response.IsSuccessStatusCode.Should().BeTrue();

        var dto = await response.Content.ReadFromJsonAsync<ProductDetailDto>(ct);
        dto.Should().NotBeNull();
        dto!.Id.Should().Be(product.Id);
        dto.Name.Should().Be(product.Name);
        dto.DisplayOrder.Should().Be(product.DisplayOrder);
    }

    [Fact]
    public async Task GetProduct_UnknownSlug_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await fixture.HttpClient.GetAsync($"/api/shop/products/does-not-exist-{Guid.NewGuid():N}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDeliverySettings_ReturnsDtoEvenWhenNoRowExists()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await fixture.HttpClient.GetAsync("/api/shop/delivery-settings", ct);
        response.IsSuccessStatusCode.Should().BeTrue();

        var dto = await response.Content.ReadFromJsonAsync<DeliverySettingsDto>(ct);
        dto.Should().NotBeNull();
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private async Task<Collection> SeedCollection(bool isActive = true, CancellationToken ct = default)
    {
        var collection = new Collection
        {
            Id = Guid.NewGuid(),
            Title = $"Test Collection {Guid.NewGuid():N}",
            Slug = $"test-coll-{Guid.NewGuid():N}",
            Description = "Seeded by ShopApiTests",
            IsActive = isActive,
            StartDate = DateTime.UtcNow.AddDays(-1)
        };
        await using var db = fixture.CreateDbContext();
        db.Collections.Add(collection);
        await db.SaveChangesAsync(ct);
        return collection;
    }

    private async Task<Product> SeedProduct(Guid? collectionId = null, CancellationToken ct = default)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = $"Test Plant {Guid.NewGuid():N}",
            Slug = $"test-plant-{Guid.NewGuid():N}",
            Price = 9.99m,
            IsAvailable = true,
            CollectionId = collectionId,
            DisplayOrder = 5,
            ImageUrls = []
        };
        await using var db = fixture.CreateDbContext();
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return product;
    }
}
