using System;
using System.Data.Common;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers the conflict-tolerant ItemValues get-or-create used by
/// <c>ItemPersistenceService.UpdateOrInsertItems</c>. The concurrency race itself cannot be
/// reproduced on the in-memory SQLite test harness (SQLite serialises writes, which is exactly
/// why the bug does not occur there), but these tests verify the two facts the fix relies on
/// against the real generated schema: the unique (Type, Value) index is enforced, and the
/// ON CONFLICT upsert the fix emits is portable to SQLite and idempotent.
/// </summary>
public sealed class ItemValueUpsertTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;

    public ItemValueUpsertTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public void PlainInsert_DuplicateTypeValue_ViolatesUniqueConstraint()
    {
        using var context = CreateDbContext();

        InsertPlain(context, Guid.NewGuid(), ItemValueType.Genre, "Drama");

        // Without conflict handling, a second item introducing the same (Type, Value)
        // violates IX_ItemValues_Type_Value. This is the failure reported in
        // JPVenson/Jellyfin.Pgsql#19 when two items race to create a shared value.
        Assert.ThrowsAny<DbException>(() => InsertPlain(context, Guid.NewGuid(), ItemValueType.Genre, "Drama"));
    }

    [Fact]
    public void Upsert_DuplicateTypeValue_IsIdempotentAndDoesNotThrow()
    {
        using var context = CreateDbContext();

        var inserted = Upsert(context, Guid.NewGuid(), ItemValueType.Genre, "Drama");
        var conflicted = Upsert(context, Guid.NewGuid(), ItemValueType.Genre, "Drama");

        Assert.Equal(1, inserted);   // first writer creates the row
        Assert.Equal(0, conflicted); // second writer is a no-op, not a constraint violation

        var rows = context.ItemValues
            .Where(e => e.Type == ItemValueType.Genre && e.Value == "Drama")
            .ToList();
        Assert.Single(rows);
    }

    private static void InsertPlain(JellyfinDbContext context, Guid id, ItemValueType type, string value)
        => context.Database.ExecuteSql(
            $"""INSERT INTO "ItemValues" ("ItemValueId", "Type", "Value", "CleanValue") VALUES ({id}, {(int)type}, {value}, {value})""");

    // Mirrors the upsert in ItemPersistenceService.UpdateOrInsertItems.
    private static int Upsert(JellyfinDbContext context, Guid id, ItemValueType type, string value)
        => context.Database.ExecuteSql(
            $"""INSERT INTO "ItemValues" ("ItemValueId", "Type", "Value", "CleanValue") VALUES ({id}, {(int)type}, {value}, {value}) ON CONFLICT ("Type", "Value") DO NOTHING""");

    private JellyfinDbContext CreateDbContext()
        => new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
}
