using FluentAssertions;
using KatiesGarden.Api.Data;
using KatiesGarden.Models.Entities;
using KatiesGarden.Tests.Functions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace KatiesGarden.Tests.Data;

/// <summary>
/// Verifies that the EF Core migration strategy works correctly against a real
/// Postgres container. Two scenarios are tested:
///
///   1. Fresh database — MigrateAsync() creates the full schema and records the
///      migration in __EFMigrationsHistory.
///
///   2. Idempotency — running MigrateAsync() a second time is a no-op (the IF
///      NOT EXISTS guards mean no error and no duplicate rows).
///
/// Also tests the stripe_processed_events unique constraint that powers
/// event-level idempotency in StripeWebhookFunction.
/// </summary>
[Trait("Category", "Integration")]
[Collection("Postgres")]
public class MigrationIntegrationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task MigrateAsync_OnFreshDatabase_CreatesAllTables()
    {
        using var db = fixture.CreateDbContext();

        await db.Database.MigrateAsync();

        // Verify a cross-section of tables exist — each CountAsync throws if the table is missing
        await db.Subscribers.CountAsync();
        await db.Orders.CountAsync();
        await db.AuditLogs.CountAsync();
        await db.StripeProcessedEvents.CountAsync();
    }

    [Fact]
    public async Task MigrateAsync_CalledTwice_IsIdempotent()
    {
        using var db = fixture.CreateDbContext();

        await db.Database.MigrateAsync();

        // Second call must not throw — IF NOT EXISTS guards prevent DDL conflicts
        var act = async () => await db.Database.MigrateAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MigrateAsync_RecordsMigrationInHistory()
    {
        using var db = fixture.CreateDbContext();
        await db.Database.MigrateAsync();

        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();

        applied.Should().ContainSingle(m => m.Contains("InitialSchema"),
            because: "the InitialSchema migration must be recorded in __EFMigrationsHistory");
    }

    [Fact]
    public async Task StripeProcessedEvents_UniqueConstraint_PreventsReplay()
    {
        using var db = fixture.CreateDbContext();
        await db.Database.MigrateAsync();

        const string eventId = "evt_test_unique_constraint";

        // First insert succeeds
        db.StripeProcessedEvents.Add(new StripeProcessedEvent { EventId = eventId });
        await db.SaveChangesAsync();

        // Second insert with the same EventId must violate the primary key / unique constraint
        using var db2 = fixture.CreateDbContext();
        db2.StripeProcessedEvents.Add(new StripeProcessedEvent { EventId = eventId });

        var act = async () => await db2.SaveChangesAsync();

        var exception = await act.Should().ThrowAsync<DbUpdateException>();
        var pgEx = exception.Which.InnerException as PostgresException;
        pgEx.Should().NotBeNull(because: "inner exception should be a PostgresException");
        pgEx!.SqlState.Should().Be("23505",
            because: "duplicate Stripe event IDs must raise a unique constraint violation (SQLSTATE 23505)");
    }
}
