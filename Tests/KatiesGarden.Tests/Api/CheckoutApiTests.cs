using FluentAssertions;
using KatiesGarden.Api.Data;
using KatiesGarden.Models.Entities;
using KatiesGarden.Models.Shop;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace KatiesGarden.Tests.Api;

/// <summary>
/// CheckoutFunction calls Stripe. With STRIPE_SECRET_KEY set to a placeholder,
/// Stripe.SessionService.CreateAsync raises StripeException, exercising the
/// catch block that cleans up the orphan order. This is the most valuable thing
/// to test here — the happy path requires a real Stripe test key.
/// </summary>
[Trait("Category", "Integration")]
[Collection("AspireApi")]
public class CheckoutApiTests(AspireApiFixture fixture)
{
    [Fact]
    public async Task CreateSession_InvalidBody_ReturnsBadRequest()
    {
        var response = await fixture.HttpClient.PostAsJsonAsync(
            "/api/checkout/create-session",
            new CheckoutRequest());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSession_UnknownProductId_ReturnsBadRequest()
    {
        var body = ValidRequest();
        body.Items.Add(new CartItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 });

        var response = await fixture.HttpClient.PostAsJsonAsync("/api/checkout/create-session", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSession_InsufficientStock_ReturnsBadRequest()
    {
        var product = await SeedProduct(stock: 1);
        var body = ValidRequest();
        body.Items.Add(new CartItemRequest { ProductId = product.Id, Quantity = 5 });

        var response = await fixture.HttpClient.PostAsJsonAsync("/api/checkout/create-session", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSession_StripeFailure_CleansUpOrphanOrder()
    {
        // With a placeholder Stripe key, sessionService.CreateAsync throws;
        // the catch block removes the orphan order.
        var product = await SeedProduct(stock: 5);
        var body = ValidRequest();
        body.Items.Add(new CartItemRequest { ProductId = product.Id, Quantity = 1 });

        var ordersBefore = await CountPendingOrdersForEmail(body.Email);
        var response = await fixture.HttpClient.PostAsJsonAsync("/api/checkout/create-session", body);
        var ordersAfter = await CountPendingOrdersForEmail(body.Email);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Stripe rejection should return 400, not 500");
        ordersAfter.Should().Be(ordersBefore,
            "the orphan pending order must be removed after Stripe failure");
    }

    private async Task<Product> SeedProduct(int stock)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = $"Test Product {Guid.NewGuid():N}",
            Slug = $"test-prod-{Guid.NewGuid():N}",
            Price = 5m,
            StockQuantity = stock,
            IsAvailable = true,
            ImageUrls = []
        };
        await using var db = fixture.CreateDbContext();
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }

    private async Task<int> CountPendingOrdersForEmail(string email)
    {
        await using var db = fixture.CreateDbContext();
        return await db.Orders.CountAsync(o => o.CustomerEmail == email);
    }

    private static CheckoutRequest ValidRequest() => new()
    {
        FirstName = "Test",
        LastName = "Customer",
        Email = $"checkout-{Guid.NewGuid():N}@example.com",
        Phone = "07700 900000",
        DeliveryType = "Collection",
        Items = []
    };
}
