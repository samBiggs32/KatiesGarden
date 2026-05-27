using FluentAssertions;
using KatiesGarden.Api.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KatiesGarden.Tests.Functions;

/// <summary>
/// Verifies the orphan-order cleanup that CheckoutFunction performs when
/// Stripe's SessionService.CreateAsync throws a StripeException.
///
/// The catch block does:
///   db.Orders.Remove(order);
///   await db.SaveChangesAsync(ct);
///
/// These tests confirm that the EF Core operations backing that code path
/// behave correctly end-to-end.
/// </summary>
public class OrderOrphanCleanupTests : IDisposable
{
    private readonly AppDbContext _db;

    public OrderOrphanCleanupTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
    }

    [Fact]
    public async Task OrphanOrder_WhenRemovedAfterCreate_IsGone()
    {
        var order = BuildPendingOrder("KG-20250101-AAAA");
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // Confirm it was persisted
        var orderId = order.Id;
        (await _db.Orders.FindAsync(orderId)).Should().NotBeNull();

        // Simulate CheckoutFunction catch block
        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();

        (await _db.Orders.FindAsync(orderId)).Should().BeNull();
    }

    [Fact]
    public async Task OrphanOrder_WhenRemovedAfterCreate_DoesNotAppearInQuery()
    {
        var order = BuildPendingOrder("KG-20250101-BBBB");
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();

        var count = await _db.Orders
            .Where(o => o.OrderNumber == order.OrderNumber)
            .CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task OrphanOrder_WhenRemovedAfterCreate_OtherOrdersUnaffected()
    {
        var kept = BuildPendingOrder("KG-20250101-CCCC");
        var orphan = BuildPendingOrder("KG-20250101-DDDD");
        _db.Orders.AddRange(kept, orphan);
        await _db.SaveChangesAsync();

        // Only remove the orphan
        _db.Orders.Remove(orphan);
        await _db.SaveChangesAsync();

        (await _db.Orders.FindAsync(kept.Id)).Should().NotBeNull();
        (await _db.Orders.FindAsync(orphan.Id)).Should().BeNull();
    }

    [Fact]
    public async Task PendingOrders_CountReflectsCleanup()
    {
        // Simulate 3 checkout attempts where 2 fail Stripe and are cleaned up
        var confirmed = BuildPendingOrder("KG-20250101-EEEE");
        var orphan1 = BuildPendingOrder("KG-20250101-FFFF");
        var orphan2 = BuildPendingOrder("KG-20250101-GGGG");
        _db.Orders.AddRange(confirmed, orphan1, orphan2);
        await _db.SaveChangesAsync();

        _db.Orders.Remove(orphan1);
        _db.Orders.Remove(orphan2);
        await _db.SaveChangesAsync();

        var pendingCount = await _db.Orders
            .CountAsync(o => o.Status == OrderStatus.Pending);
        pendingCount.Should().Be(1);
    }

    private static Order BuildPendingOrder(string orderNumber) => new()
    {
        OrderNumber = orderNumber,
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

    public void Dispose() => _db.Dispose();
}
