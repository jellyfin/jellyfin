using System;
using System.Data;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

/// <summary>
/// The exceptions here are raised by a real server, so the SQLSTATEs the provider classifies on are the ones
/// PostgreSQL reports, not ones assumed by the test.
/// </summary>
public sealed class PostgresWriteConflictViolationTests : DatabaseTestFixture
{
    private readonly IJellyfinDatabaseProvider _provider = new PostgresDatabaseProvider(new CollectingLogger<PostgresDatabaseProvider>());

    [ProviderTheory(TestDbProvider.Postgres)]
    [InlineData("""INSERT INTO "Children" VALUES (2, 'alpha', 'x', 1, 1)""", "23505", true)]
    [InlineData("""INSERT INTO "Children" VALUES (1, 'beta', 'x', 1, 1)""", "23505", true)]
    [InlineData("""INSERT INTO "Children" VALUES (2, 'beta', 'x', 2, 1)""", "23503", false)]
    [InlineData("""INSERT INTO "Children" VALUES (2, 'beta', NULL, 1, 1)""", "23502", false)]
    [InlineData("""INSERT INTO "Children" VALUES (2, 'beta', 'x', 1, -1)""", "23514", false)]
    public async Task IsRetryableWriteConflict_ConstraintViolation_MatchesTheViolatedConstraint(
        string statement,
        string expectedSqlState,
        bool expectedRetryable)
    {
        await CreateChildren();

        await using var context = CreateDbContext();
        var exception = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync(statement));
        Assert.Equal(expectedSqlState, exception.SqlState);

        Assert.Equal(expectedRetryable, _provider.IsRetryableWriteConflict(exception));
        Assert.Equal(expectedRetryable, _provider.IsRetryableWriteConflict(new DbUpdateException("save failed", exception)));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task IsRetryableWriteConflict_DuplicateRowSavedThroughTheContext_IsRetryable()
    {
        const string accessToken = "duplicate";
        await using (var context = CreateDbContext())
        {
            context.ApiKeys.Add(new ApiKey("first") { AccessToken = accessToken });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = CreateDbContext())
        {
            context.ApiKeys.Add(new ApiKey("second") { AccessToken = accessToken });
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));

            Assert.Equal("23505", Assert.IsType<PostgresException>(exception.InnerException).SqlState);
            Assert.True(_provider.IsRetryableWriteConflict(exception));
        }
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task IsRetryableWriteConflict_MissingParentSavedThroughTheContext_IsNotRetryable()
    {
        await using var context = CreateDbContext();
        context.Devices.Add(new Device(Guid.NewGuid(), "app", "1.0", "name", "device"));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));

        Assert.Equal("23503", Assert.IsType<PostgresException>(exception.InnerException).SqlState);
        Assert.False(_provider.IsRetryableWriteConflict(exception));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task IsRetryableWriteConflict_ConflictingSerializableTransactions_IsRetryable()
    {
        await CreateCounters();

        await using var first = CreateDbContext();
        await using var second = CreateDbContext();
        await using var firstTransaction = await first.Database.BeginTransactionAsync(IsolationLevel.Serializable, TestContext.Current.CancellationToken);
        await using var secondTransaction = await second.Database.BeginTransactionAsync(IsolationLevel.Serializable, TestContext.Current.CancellationToken);

        await ReadTotal(first);
        await ReadTotal(second);
        await first.Database.ExecuteSqlRawAsync("""UPDATE "Counters" SET "Value" = 1 WHERE "Id" = 1""", TestContext.Current.CancellationToken);
        await second.Database.ExecuteSqlRawAsync("""UPDATE "Counters" SET "Value" = 1 WHERE "Id" = 2""", TestContext.Current.CancellationToken);
        await firstTransaction.CommitAsync(TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<PostgresException>(() => secondTransaction.CommitAsync(TestContext.Current.CancellationToken));

        Assert.Equal("40001", exception.SqlState);
        Assert.True(_provider.IsRetryableWriteConflict(exception));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task IsRetryableWriteConflict_WritersLockingRowsInOppositeOrder_IsRetryable()
    {
        await CreateCounters();

        await using var first = CreateDbContext();
        await using var second = CreateDbContext();
        await using var firstTransaction = await first.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await using var secondTransaction = await second.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await first.Database.ExecuteSqlRawAsync("""UPDATE "Counters" SET "Value" = 1 WHERE "Id" = 1""", TestContext.Current.CancellationToken);
        await second.Database.ExecuteSqlRawAsync("""UPDATE "Counters" SET "Value" = 1 WHERE "Id" = 2""", TestContext.Current.CancellationToken);

        var firstWait = TryUpdate(first, 2);
        var secondWait = TryUpdate(second, 1);
        var outcomes = await Task.WhenAll(firstWait, secondWait);

        var failure = Assert.Single(outcomes, e => e is not null);
        var exception = Assert.IsType<PostgresException>(failure);
        Assert.Equal("40P01", exception.SqlState);
        Assert.True(_provider.IsRetryableWriteConflict(exception));
    }

    private static async Task<Exception?> TryUpdate(JellyfinDbContext context, int id)
    {
        try
        {
            await context.Database.ExecuteSqlAsync($"""UPDATE "Counters" SET "Value" = 2 WHERE "Id" = {id}""", TestContext.Current.CancellationToken);
            await context.Database.CurrentTransaction!.CommitAsync(TestContext.Current.CancellationToken);
            return null;
        }
        catch (PostgresException ex)
        {
            await context.Database.CurrentTransaction!.RollbackAsync(TestContext.Current.CancellationToken);
            return ex;
        }
    }

    private static Task<int> ReadTotal(JellyfinDbContext context)
        => context.Database.SqlQueryRaw<int>("""SELECT CAST(sum("Value") AS integer) AS "Value" FROM "Counters" """).SingleAsync(TestContext.Current.CancellationToken);

    private async Task CreateChildren()
    {
        await using var context = CreateDbContext();
        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "Parents" ("Id" integer NOT NULL PRIMARY KEY);
            CREATE TABLE "Children" (
                "Id" integer NOT NULL PRIMARY KEY,
                "Name" text NOT NULL UNIQUE,
                "Label" text NOT NULL,
                "ParentId" integer NOT NULL REFERENCES "Parents" ("Id"),
                "Size" integer NOT NULL CHECK ("Size" >= 0));
            INSERT INTO "Parents" VALUES (1);
            INSERT INTO "Children" VALUES (1, 'alpha', 'x', 1, 1);
            """,
            TestContext.Current.CancellationToken);
    }

    private async Task CreateCounters()
    {
        await using var context = CreateDbContext();
        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE "Counters" ("Id" integer NOT NULL PRIMARY KEY, "Value" integer NOT NULL);
            INSERT INTO "Counters" VALUES (1, 0), (2, 0);
            """,
            TestContext.Current.CancellationToken);
    }
}
