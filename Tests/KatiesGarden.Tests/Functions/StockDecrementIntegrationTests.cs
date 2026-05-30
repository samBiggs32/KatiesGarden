using FluentAssertions;
using KatiesGarden.Api.Data;
using KatiesGarden.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KatiesGarden.Tests.Functions;

/// <summary>
/// Integration tests for the atomic stock-decrement query in StripeWebhookFunction,
/// running against a real Postgres container.
///
/// The query under test is:
///   db.Products
///     .Where(p =&gt; p.Id == line.ProductId
///              &amp;&amp; p.StockQuantity != null
///              &amp;&amp; p.StockQuantity &gt;= line.Quantity)
///     .ExecuteUpdateAsync(s =&gt; s.SetProperty(p =&gt; p.StockQuantity, p =&gt; p.StockQuantity! - line.Quantity), ct);
///
/// Postgres row-level locking makes the UPDATE atomic — the WHERE guard prevents
/// oversell even when two webhooks race for the last unit. These tests verify both
/// the single-threaded semantics and the concurrent case.
/// </summary>
[Trait("Category", "Integration")]
[Collection("Postgres")]
public class StockDecrementIntegrationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Decrement_SufficientStock_DecrementsByQuantity()
    {
        var ct = TestContext.Current.CancellationToken;
        var productId = await SeedProduct(stock: 5, ct);

        await using var db = fixture.CreateDbContext();
        var rowsUpdated = await RunDecrement(db, productId, qty: 2, ct);

        rowsUpdated.Should().Be(1);
        var stock = await GetStock(productId, ct);
        stock.Should().Be(3);
    }

    [Fact]
    public async Task Decrement_InsufficientStock_ReturnsZeroAndLeavesStockUnchanged()
    {
        var ct = TestContext.Current.CancellationToken;
        var productId = await SeedProduct(stock: 1, ct);

        await using var db = fixture.CreateDbContext();
        var rowsUpdated = await RunDecrement(db, productId, qty: 2, ct);

        rowsUpdated.Should().Be(0);
        (await GetStock(productId, ct)).Should().Be(1);
    }

    [Fact]
    public async Task Decrement_NullStock_IsIgnoredByWhereGuard()
    {
        var ct = TestContext.Current.CancellationToken;
        var productId = await SeedProduct(stock: null, ct);

        await using var db = fixture.CreateDbContext();
        var rowsUpdated = await RunDecrement(db, productId, qty: 1, ct);

        rowsUpdated.Should().Be(0);
        (await GetStock(productId, ct)).Should().BeNull();
    }

    [Fact]
    public async Task Decrement_ToZero_FollowUpUpdateMarksProductUnavailable()
    {
        var ct = TestContext.Current.CancellationToken;
        var productId = await SeedProduct(stock: 1, ct);

        await using (var db = fixture.CreateDbContext())
        {
            await RunDecrement(db, productId, qty: 1, ct);
            await db.Products
                .Where(p => p.Id == productId && p.StockQuantity == 0)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsAvailable, false), ct);
        }

        await using var verify = fixture.CreateDbContext();
        var product = await verify.Products.FindAsync([productId], ct);
        product!.StockQuantity.Should().Be(0);
        product.IsAvailable.Should().BeFalse();
    }

    /// <summary>
    /// The whole point of using ExecuteUpdateAsync with a WHERE guard rather than
    /// find-then-update: under concurrent webhooks the row-level lock ensures
    /// exactly one UPDATE succeeds. This test fires the decrement in parallel and
    /// asserts the total rows updated equals the available stock — never more.
    /// </summary>
    [Fact]
    public async Task Decrement_ManyConcurrent_NeverOversells()
    {
        var ct = TestContext.Current.CancellationToken;
        const int initialStock = 5;
        const int concurrentRequests = 20;
        var productId = await SeedProduct(stock: initialStock, ct);

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(async _ =>
            {
                await using var db = fixture.CreateDbContext();
                return await RunDecrement(db, productId, qty: 1, ct);
            })
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var successfulDecrements = results.Sum();

        successfulDecrements.Should().Be(initialStock,
            "exactly the initial stock should be sold; the WHERE guard must reject the rest");
        (await GetStock(productId, ct)).Should().Be(0);
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private async Task<Guid> SeedProduct(int? stock, CancellationToken ct = default)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Flower",
            Slug = $"test-flower-{Guid.NewGuid():N}",
            Price = 5.00m,
            StockQuantity = stock,
            IsAvailable = true,
            ImageUrls = []
        };
        await using var db = fixture.CreateDbContext();
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return product.Id;
    }

    private async Task<int?> GetStock(Guid productId, CancellationToken ct = default)
    {
        await using var db = fixture.CreateDbContext();
        var product = await db.Products.AsNoTracking().FirstAsync(p => p.Id == productId, ct);
        return product.StockQuantity;
    }

    private static Task<int> RunDecrement(AppDbContext db, Guid productId, int qty, CancellationToken ct = default) =>
        db.Products
            .Where(p => p.Id == productId
                     && p.StockQuantity != null
                     && p.StockQuantity >= qty)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.StockQuantity, p => p.StockQuantity!.Value - qty), ct);
}
