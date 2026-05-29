using KatiesGarden.Api.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace KatiesGarden.Tests.Functions;

/// <summary>
/// Shared Postgres container fixture for integration tests.
///
/// Spins up a single Postgres container per test assembly run via xUnit's
/// ICollectionFixture. Each test class that needs the DB declares
/// [Collection("Postgres")] so they share the same container instance.
///
/// Use <see cref="CreateDbContext"/> to get a fresh DbContext bound to the
/// container; tests should clean up the data they insert (or use unique IDs).
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("katiesgarden_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        // Create schema once — same path Program.cs uses on cold start.
        using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }
}

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
