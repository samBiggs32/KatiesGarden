using FluentAssertions;
using KatiesGarden.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KatiesGarden.Tests.Functions;

/// <summary>
/// Verifies the atomic stock-decrement queries in StripeWebhookFunction.
///
/// The webhook uses ExecuteUpdateAsync with a WHERE guard to prevent oversell:
///   db.Products
///     .Where(p => p.Id == line.ProductId
///              && p.StockQuantity != null
///              && p.StockQuantity >= line.Quantity)
///     .ExecuteUpdateAsync(...)
///
/// Uses SQLite in-memory because ExecuteUpdateAsync requires a relational provider.
/// </summary>
public class StockDecrementTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public StockDecrementTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    // ── sufficient stock ──────────────────────────────────────────────────

    [Fact]
    public async Task Decrement_SufficientStock_DecrementsByQuantity()
    {
        var product = await SeedProduct(stock: 5);

        var rowsUpdated = await RunDecrement(product.Id, qty: 2);

        rowsUpdated.Should().Be(1);
        var updated = await _db.Products.FindAsync(product.Id);
        updated!.StockQuantity.Should().Be(3);
    }

    [Fact]
    public async Task Decrement_ExactStock_DecrementsToZero()
    {
        var product = await SeedProduct(stock: 3);

        var rowsUpdated = await RunDecrement(product.Id, qty: 3);

        rowsUpdated.Should().Be(1);
        var updated = await _db.Products.FindAsync(product.Id);
        updated!.StockQuantity.Should().Be(0);
    }

    // ── insufficient stock (oversell guard) ───────────────────────────────

    [Fact]
    public async Task Decrement_InsufficientStock_ReturnsZeroAndLeavesStockUnchanged()
    {
        var product = await SeedProduct(stock: 1);

        var rowsUpdated = await RunDecrement(product.Id, qty: 2);

        rowsUpdated.Should().Be(0, "WHERE guard should block when stock < quantity");
        var unchanged = await _db.Products.FindAsync(product.Id);
        unchanged!.StockQuantity.Should().Be(1);
    }

    [Fact]
    public async Task Decrement_ZeroStock_ReturnsZeroAndLeavesStockAtZero()
    {
        var product = await SeedProduct(stock: 0);

        var rowsUpdated = await RunDecrement(product.Id, qty: 1);

        rowsUpdated.Should().Be(0);
        var unchanged = await _db.Products.FindAsync(product.Id);
        unchanged!.StockQuantity.Should().Be(0);
    }

    // ── untracked stock (null) ─────────────────────────────────────────────

    [Fact]
    public async Task Decrement_NullStock_ReturnsZeroAndLeavesNullUnchanged()
    {
        var product = await SeedProduct(stock: null);

        var rowsUpdated = await RunDecrement(product.Id, qty: 1);

        rowsUpdated.Should().Be(0, "WHERE guard filters out null StockQuantity");
        var unchanged = await _db.Products.FindAsync(product.Id);
        unchanged!.StockQuantity.Should().BeNull();
    }

    // ── mark unavailable when stock reaches zero ──────────────────────────

    [Fact]
    public async Task Decrement_ToZero_SecondPassMarksProductUnavailable()
    {
        var product = await SeedProduct(stock: 1);

        await RunDecrement(product.Id, qty: 1);

        // Second ExecuteUpdateAsync: mark unavailable when StockQuantity == 0
        await _db.Products
            .Where(p => p.Id == product.Id && p.StockQuantity == 0)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsAvailable, false));

        var sold = await _db.Products.FindAsync(product.Id);
        sold!.IsAvailable.Should().BeFalse();
        sold.StockQuantity.Should().Be(0);
    }

    [Fact]
    public async Task Decrement_StockRemainsAboveZero_DoesNotMarkUnavailable()
    {
        var product = await SeedProduct(stock: 3);

        await RunDecrement(product.Id, qty: 1);

        // Second ExecuteUpdateAsync should find no rows to update
        var rowsMarked = await _db.Products
            .Where(p => p.Id == product.Id && p.StockQuantity == 0)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsAvailable, false));

        rowsMarked.Should().Be(0);
        var still = await _db.Products.FindAsync(product.Id);
        still!.IsAvailable.Should().BeTrue();
        still.StockQuantity.Should().Be(2);
    }

    // ── concurrent oversell simulation ────────────────────────────────────

    [Fact]
    public async Task Decrement_TwoCallsOnLastUnit_OnlyOneSucceeds()
    {
        var product = await SeedProduct(stock: 1);

        // Both webhooks see stock=1 and attempt to decrement by 1 concurrently.
        // With ExecuteUpdateAsync the WHERE check is atomic, so only one UPDATE matches.
        var rows1 = await RunDecrement(product.Id, qty: 1);
        var rows2 = await RunDecrement(product.Id, qty: 1); // stock now 0; WHERE guard blocks

        (rows1 + rows2).Should().Be(1, "exactly one of the two concurrent decrements should succeed");
        var final = await _db.Products.FindAsync(product.Id);
        final!.StockQuantity.Should().Be(0);
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private async Task<Product> SeedProduct(int? stock)
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
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear(); // ensure subsequent FindAsync hits the DB
        return product;
    }

    private Task<int> RunDecrement(Guid productId, int qty) =>
        _db.Products
            .Where(p => p.Id == productId
                     && p.StockQuantity != null
                     && p.StockQuantity >= qty)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.StockQuantity, p => p.StockQuantity! - qty));

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
