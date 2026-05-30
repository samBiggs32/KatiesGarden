using FluentAssertions;
using KatiesGarden.Api.Data;
using KatiesGarden.Models.Entities;
using KatiesGarden.Models.Shop;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace KatiesGarden.Tests.Api;

[Trait("Category", "Integration")]
[Collection("AspireApi")]
public class AdminOrderApiTests(AspireApiFixture fixture)
{
    [Fact]
    public async Task ListOrders_Unauthenticated_Returns401()
    {
        var response = await fixture.HttpClient.GetAsync("/api/manage/orders");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListOrders_Admin_FiltersByStatus()
    {
        using var admin = fixture.CreateAdminClient();
        var pending = await SeedOrder(OrderStatus.Pending);
        var confirmed = await SeedOrder(OrderStatus.Confirmed);

        var resp = await admin.GetAsync("/api/manage/orders?status=Confirmed");
        resp.IsSuccessStatusCode.Should().BeTrue();
        var list = await resp.Content.ReadFromJsonAsync<List<OrderSummaryDto>>();

        list.Should().NotBeNull();
        list!.Any(o => o.Id == confirmed.Id).Should().BeTrue();
        list.Any(o => o.Id == pending.Id).Should().BeFalse();
    }

    [Fact]
    public async Task GetOrder_Admin_ReturnsDetailWithLines()
    {
        using var admin = fixture.CreateAdminClient();
        var order = await SeedOrder(OrderStatus.Confirmed, includeLine: true);

        var resp = await admin.GetAsync($"/api/manage/orders/{order.Id}");
        resp.IsSuccessStatusCode.Should().BeTrue();
        var dto = await resp.Content.ReadFromJsonAsync<OrderDetailDto>();

        dto.Should().NotBeNull();
        dto!.OrderNumber.Should().Be(order.OrderNumber);
        dto.Lines.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOrder_UnknownId_ReturnsNotFound()
    {
        using var admin = fixture.CreateAdminClient();

        var resp = await admin.GetAsync($"/api/manage/orders/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateStatus_Admin_PersistsNewStatus()
    {
        using var admin = fixture.CreateAdminClient();
        var order = await SeedOrder(OrderStatus.Pending);

        var resp = await admin.PatchAsJsonAsync(
            $"/api/manage/orders/{order.Id}/status",
            new UpdateOrderStatusRequest("Processing"));
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var db = fixture.CreateDbContext();
        var refetched = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id);
        refetched.Status.Should().Be(OrderStatus.Processing);
    }

    [Fact]
    public async Task UpdateStatus_InvalidStatus_ReturnsBadRequest()
    {
        using var admin = fixture.CreateAdminClient();
        var order = await SeedOrder(OrderStatus.Pending);

        var resp = await admin.PatchAsJsonAsync(
            $"/api/manage/orders/{order.Id}/status",
            new UpdateOrderStatusRequest("NotARealStatus"));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateNotes_Admin_PersistsNotes()
    {
        using var admin = fixture.CreateAdminClient();
        var order = await SeedOrder(OrderStatus.Confirmed);

        var resp = await admin.PutAsJsonAsync(
            $"/api/manage/orders/{order.Id}/notes",
            new { Notes = "Customer prefers Friday collection" });
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var db = fixture.CreateDbContext();
        var refetched = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id);
        refetched.AdminNotes.Should().Be("Customer prefers Friday collection");
    }

    private async Task<Order> SeedOrder(OrderStatus status, bool includeLine = false)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"KG-T-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            CustomerFirstName = "Test",
            CustomerLastName = "Customer",
            CustomerEmail = "test@example.com",
            CustomerPhone = "07700 900000",
            DeliveryType = DeliveryType.Collection,
            Subtotal = 10m,
            DeliveryFee = 0m,
            Total = 10m,
            Status = status
        };
        if (includeLine)
        {
            order.Lines.Add(new OrderLine
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Test product",
                UnitPrice = 10m,
                Quantity = 1,
                LineTotal = 10m
            });
        }
        await using var db = fixture.CreateDbContext();
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }
}

internal static class HttpClientPatchExtensions
{
    public static Task<HttpResponseMessage> PatchAsJsonAsync<T>(
        this HttpClient client, string requestUri, T value)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, requestUri)
        {
            Content = JsonContent.Create(value)
        };
        return client.SendAsync(request);
    }
}
