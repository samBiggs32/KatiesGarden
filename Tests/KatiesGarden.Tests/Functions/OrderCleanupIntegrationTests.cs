using FluentAssertions;
using KatiesGarden.Api.Data;
using KatiesGarden.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KatiesGarden.Tests.Functions;

/// <summary>
/// Integration tests for the orphan-order cleanup path in CheckoutFunction.
///
/// When sessionService.CreateAsync throws a StripeException, the catch block does:
///   db.Orders.Remove(order);
///   await db.SaveChangesAsync(ct);
///
/// These tests run against real Postgres to verify the cascade behaviour and
/// transactional semantics that the in-memory provider can't faithfully replicate.
/// </summary>
[Trait("Category", "Integration")]
[Collection("Postgres")]
public class OrderCleanupIntegrationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task OrphanOrder_WithLines_RemoveCascadesToOrderLines()
    {
        var ct = TestContext.Current.CancellationToken;

        // Create a real product first — order_lines.ProductId has a RESTRICT FK.
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Cascade Test Product",
            Slug = $"cascade-test-{Guid.NewGuid():N}",
            Price = 5m
        };
        await using (var db = fixture.CreateDbContext())
        {
            db.Products.Add(product);
            await db.SaveChangesAsync(ct);
        }

        var order = BuildPendingOrder("KG-INT-AAAA");
        order.Lines.Add(new OrderLine
        {
            ProductId = product.Id,
            ProductName = "Test product",
            UnitPrice = 5m,
            Quantity = 1,
            LineTotal = 5m
        });

        await using (var db = fixture.CreateDbContext())
        {
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);
        }

        await using (var db = fixture.CreateDbContext())
        {
            var loaded = await db.Orders.Include(o => o.Lines).FirstAsync(o => o.Id == order.Id, ct);
            db.Orders.Remove(loaded);
            await db.SaveChangesAsync(ct);
        }

        await using var verify = fixture.CreateDbContext();
        (await verify.Orders.FindAsync([order.Id], ct)).Should().BeNull();
        var orphanLines = await verify.OrderLines
            .Where(l => l.OrderId == order.Id)
            .CountAsync(ct);
        orphanLines.Should().Be(0, "ON DELETE CASCADE should remove order_lines with the order");
    }

    [Fact]
    public async Task OrphanCleanup_OnlyRemovesTargetedOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var kept = BuildPendingOrder("KG-INT-BBBB");
        var orphan = BuildPendingOrder("KG-INT-CCCC");

        await using (var db = fixture.CreateDbContext())
        {
            db.Orders.AddRange(kept, orphan);
            await db.SaveChangesAsync(ct);
        }

        await using (var db = fixture.CreateDbContext())
        {
            var loaded = await db.Orders.FirstAsync(o => o.Id == orphan.Id, ct);
            db.Orders.Remove(loaded);
            await db.SaveChangesAsync(ct);
        }

        await using var verify = fixture.CreateDbContext();
        (await verify.Orders.FindAsync([kept.Id], ct)).Should().NotBeNull();
        (await verify.Orders.FindAsync([orphan.Id], ct)).Should().BeNull();
    }

    private static Order BuildPendingOrder(string orderNumber) => new()
    {
        Id = Guid.NewGuid(),
        OrderNumber = $"{orderNumber}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}",
        CustomerFirstName = "Test",
        CustomerLastName = "User",
        CustomerEmail = "test@example.com",
        CustomerPhone = "07700 900000",
        DeliveryType = DeliveryType.Collection,
        Subtotal = 10m,
        DeliveryFee = 0m,
        Total = 10m,
        Status = OrderStatus.Pending
    };
}
