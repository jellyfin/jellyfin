using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

public sealed class LeasedPoolTests : IDisposable
{
    private const string MigrationHistoryTable = "__EFMigrationsHistory";

    private static readonly Guid _placeholderId = Guid.Parse("00000000-0000-0000-0000-000000000001");

#pragma warning disable RS0030 // The pool is tested on SQLite files whichever provider is selected
    private readonly SqliteFileTestBackend _backend = new();
#pragma warning restore RS0030

    public void Dispose()
    {
        _backend.Dispose();
    }

    [Theory]
    [InlineData(TestSchemaSource.Model)]
    [InlineData(TestSchemaSource.Migrations)]
    public void Create_AnySchemaSource_ProducesUsableDatabase(TestSchemaSource source)
    {
        using var database = _backend.Create(source, []);
        using var context = CreateContext(database);

        context.ActivityLogs.Add(new ActivityLog("entry", "type", Guid.Empty));
        context.SaveChanges();

        Assert.Single(context.ActivityLogs);
    }

    [Fact]
    public void Create_LeasesAtTheSameTime_DoNotShareData()
    {
        using var first = _backend.Create(TestSchemaSource.Model, []);
        using var second = _backend.Create(TestSchemaSource.Model, []);

        using (var context = CreateContext(first))
        {
            context.ActivityLogs.Add(new ActivityLog("entry", "type", Guid.Empty));
            context.SaveChanges();
        }

        using var other = CreateContext(second);
        Assert.Empty(other.ActivityLogs);
    }

    [Fact]
    public async Task Create_LeasesAtTheSameTimeFromManyThreads_UseDifferentDatabases()
    {
        const int Leases = 8;
        var databases = await Task.WhenAll(
            Enumerable.Range(0, Leases).Select(_ => Task.Run(() => _backend.Create(TestSchemaSource.Model, []), TestContext.Current.CancellationToken)))
            .ConfigureAwait(true);
        try
        {
            Assert.Equal(Leases, databases.Select(DatabaseOf).Distinct().Count());
            Assert.Equal(Leases, Directory.GetFiles(_backend.DatabaseDirectory).Length);
        }
        finally
        {
            foreach (var database in databases)
            {
                database.Dispose();
            }
        }
    }

    [Fact]
    public void CreateEmpty_HasNoSchemaAndIsDroppedWhenDisposed()
    {
        var database = _backend.CreateEmpty();
        using (var context = CreateContext(database))
        {
            Assert.False(context.GetService<IRelationalDatabaseCreator>().HasTables());
            context.Database.EnsureCreated();
            Assert.True(context.GetService<IRelationalDatabaseCreator>().HasTables());
        }

        database.Dispose();

        Assert.Empty(Directory.GetFiles(_backend.DatabaseDirectory));
    }

    [Fact]
    public void Dispose_Backend_DropsTheDatabasesThatAreStillLeased()
    {
        var database = _backend.Create(TestSchemaSource.Model, []);
        var empty = _backend.CreateEmpty();

        _backend.Dispose();

        Assert.False(Directory.Exists(_backend.DatabaseDirectory));
        database.Dispose();
        empty.Dispose();
    }

    [Fact]
    public void Dispose_Backend_RejectsLaterLeases()
    {
        _backend.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _backend.Create(TestSchemaSource.Model, []));
        Assert.Throws<ObjectDisposedException>(() => _backend.CreateEmpty());
    }

    [Fact]
    public void Dispose_BackendTwice_DoesNothingTheSecondTime()
    {
        _backend.Dispose();

        _backend.Dispose();
    }

    [Fact]
    public void Dispose_LeaseTwice_ReturnsTheDatabaseOnce()
    {
        var database = _backend.Create(TestSchemaSource.Model, []);
        var name = DatabaseOf(database);

        database.Dispose();
        database.Dispose();

        using var first = _backend.Create(TestSchemaSource.Model, []);
        using var second = _backend.Create(TestSchemaSource.Model, []);
        Assert.Equal(name, DatabaseOf(first));
        Assert.NotEqual(name, DatabaseOf(second));
    }

    [Fact]
    public void Create_AfterLeaseWasReturned_ReusesTheDatabase()
    {
        string first;
        using (var database = _backend.Create(TestSchemaSource.Model, []))
        {
            first = DatabaseOf(database);
        }

        using var again = _backend.Create(TestSchemaSource.Model, []);

        Assert.Equal(first, DatabaseOf(again));
        Assert.Single(Directory.GetFiles(_backend.DatabaseDirectory));
    }

    [Fact]
    public void Create_AfterLeaseOfOtherSchemaSourceWasReturned_BuildsAnotherDatabase()
    {
        string first;
        using (var database = _backend.Create(TestSchemaSource.Model, []))
        {
            first = DatabaseOf(database);
        }

        using var other = _backend.Create(TestSchemaSource.Migrations, []);

        Assert.NotEqual(first, DatabaseOf(other));
    }

    [Fact]
    public void Create_LeasedDatabaseIsNotHandedOutAgainUntilReturned()
    {
        using var first = _backend.Create(TestSchemaSource.Model, []);
        var second = _backend.Create(TestSchemaSource.Model, []);
        var secondName = DatabaseOf(second);
        second.Dispose();

        using var third = _backend.Create(TestSchemaSource.Model, []);

        Assert.NotEqual(DatabaseOf(first), DatabaseOf(third));
        Assert.Equal(secondName, DatabaseOf(third));
    }

    [Fact]
    public void Create_TwoLeasesWhileOneDatabaseIsWaiting_GetDifferentDatabases()
    {
        _backend.Create(TestSchemaSource.Model, []).Dispose();

        using var first = _backend.Create(TestSchemaSource.Model, []);
        using var second = _backend.Create(TestSchemaSource.Model, []);

        Assert.NotEqual(DatabaseOf(first), DatabaseOf(second));
    }

    [Theory]
    [InlineData(TestSchemaSource.Model)]
    [InlineData(TestSchemaSource.Migrations)]
    public void Dispose_ReturnedLease_LeavesEveryTableAsInAFreshDatabase(TestSchemaSource source)
    {
        Dictionary<string, int> expected;
        using (var fresh = _backend.Create(source, [], fresh: true))
        {
            expected = CountRows(fresh);
        }

        Assert.Equal(source == TestSchemaSource.Migrations, expected.ContainsKey(MigrationHistoryTable));
        Assert.All(expected.Where(table => table.Key == MigrationHistoryTable), table => Assert.True(table.Value > 0));

        using (var database = _backend.Create(source, []))
        {
            using var context = CreateContext(database);
            ModelRows.Fill(context, 3);

            var dirty = CountRows(database);
            Assert.All(ModelRows.Tables(context), entityType => Assert.True(dirty[entityType.GetTableName()!] > 0, entityType.GetTableName()));
        }

        using var reused = _backend.Create(source, []);

        Assert.Equal(expected, CountRows(reused));
    }

    [Theory]
    [InlineData(TestSchemaSource.Model)]
    [InlineData(TestSchemaSource.Migrations)]
    public void Dispose_ReturnedLease_RestartsIdentityAtOneOnEveryTable(TestSchemaSource source)
    {
        using (var database = _backend.Create(source, []))
        {
            using var context = CreateContext(database);
            ModelRows.Fill(context, 3);
        }

        using var reused = _backend.Create(source, []);
        using var reusedContext = CreateContext(reused);
        var withIdentity = ModelRows.TablesWithIdentity(reusedContext);
        Assert.NotEmpty(withIdentity);

        var added = ModelRows.Fill(reusedContext, 1);

        Assert.All(
            added.Where(row => withIdentity.Contains(row.EntityType)),
            row => Assert.Equal(1, Convert.ToInt64(row.Entry.Property(ModelRows.GetIdentityKey(row.EntityType)!.Name).CurrentValue, CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Dispose_ReturnedLease_RestoresSeedData()
    {
        using var fresh = _backend.Create(TestSchemaSource.Model, [], fresh: true);
        using (var freshContext = CreateContext(fresh))
        {
            Assert.Single(freshContext.BaseItems);
        }

        using (var database = _backend.Create(TestSchemaSource.Model, []))
        {
            using var context = CreateContext(database);
            context.BaseItems.ExecuteDelete();
            Assert.Empty(context.BaseItems);
        }

        using var reused = _backend.Create(TestSchemaSource.Model, []);
        using var reusedContext = CreateContext(reused);

        var seeded = Assert.Single(reusedContext.BaseItems);
        Assert.Equal(_placeholderId, seeded.Id);
    }

    [Fact]
    public void Dispose_ReturnedLeaseWithContextThatWasNotDisposed_ReturnsTheLease()
    {
        string name;
        using (var database = _backend.Create(TestSchemaSource.Model, []))
        {
            name = DatabaseOf(database);
            var context = CreateContext(database);
            context.ActivityLogs.Add(new ActivityLog("entry", "type", Guid.Empty));
            context.SaveChanges();
        }

        using var reused = _backend.Create(TestSchemaSource.Model, []);
        using var reusedContext = CreateContext(reused);

        Assert.Equal(name, DatabaseOf(reused));
        Assert.Empty(reusedContext.ActivityLogs);
    }

    [Fact]
    public void Dispose_LeaseOfFailedTest_ReturnsTheLease()
    {
        void FailingTest()
        {
            using var database = _backend.Create(TestSchemaSource.Model, []);
            using var context = CreateContext(database);
            context.ActivityLogs.Add(new ActivityLog("entry", "type", Guid.Empty));
            context.SaveChanges();
            throw new InvalidOperationException("The test failed.");
        }

        Assert.Throws<InvalidOperationException>(FailingTest);

        using var reused = _backend.Create(TestSchemaSource.Model, []);
        using var reusedContext = CreateContext(reused);

        Assert.Single(Directory.GetFiles(_backend.DatabaseDirectory));
        Assert.Empty(reusedContext.ActivityLogs);
    }

    [Fact]
    public void Dispose_LeaseThatCannotBeReset_DropsTheDatabaseAndRethrows()
    {
        var database = _backend.Create(TestSchemaSource.Model, []);
        var name = DatabaseOf(database);
        File.Delete(Directory.GetFiles(_backend.DatabaseDirectory).Single());

        Assert.ThrowsAny<Exception>(database.Dispose);

        Assert.Empty(Directory.GetFiles(_backend.DatabaseDirectory));
        using var next = _backend.Create(TestSchemaSource.Model, []);
        Assert.NotEqual(name, DatabaseOf(next));
    }

    [Fact]
    public void Create_Fresh_ReturnsANewDatabaseThatIsGoneAfterwards()
    {
        string pooled;
        using (var database = _backend.Create(TestSchemaSource.Model, []))
        {
            pooled = DatabaseOf(database);
        }

        string fresh;
        using (var database = _backend.Create(TestSchemaSource.Model, [], fresh: true))
        {
            fresh = DatabaseOf(database);
            Assert.NotEqual(pooled, fresh);
            Assert.Equal(2, Directory.GetFiles(_backend.DatabaseDirectory).Length);
        }

        Assert.Single(Directory.GetFiles(_backend.DatabaseDirectory));
        using var reused = _backend.Create(TestSchemaSource.Model, []);
        Assert.Equal(pooled, DatabaseOf(reused));
    }

    [Theory]
    [InlineData("CREATE TABLE \"Scratch\" (\"Id\" INTEGER)")]
    [InlineData("CREATE INDEX \"IX_Scratch\" ON \"ActivityLogs\" (\"Name\")")]
    [InlineData("CREATE TRIGGER \"Scratch\" AFTER INSERT ON \"ActivityLogs\" BEGIN SELECT 1; END")]
    [InlineData("ALTER TABLE \"ActivityLogs\" ADD COLUMN \"Scratch\" INTEGER")]
    public void Dispose_LeaseThatChangedTheSchema_IsDroppedInsteadOfReused(string statement)
    {
        string changed;
        using (var database = _backend.Create(TestSchemaSource.Model, []))
        {
            changed = DatabaseOf(database);
            using var context = CreateContext(database);
            context.Database.ExecuteSqlRaw(statement);
        }

        using var next = _backend.Create(TestSchemaSource.Model, []);
        using var nextContext = CreateContext(next);

        Assert.NotEqual(changed, DatabaseOf(next));
        Assert.Single(Directory.GetFiles(_backend.DatabaseDirectory));
        Assert.DoesNotContain("Scratch", TableNames(nextContext));
    }

    [Fact]
    public void Dispose_LeaseThatChangedOnlyTheMigrationHistory_IsReused()
    {
        string name;
        using (var database = _backend.Create(TestSchemaSource.Migrations, []))
        {
            name = DatabaseOf(database);
            using var context = CreateContext(database);
            context.Database.ExecuteSqlRaw($"DELETE FROM \"{MigrationHistoryTable}\"");
        }

        using var next = _backend.Create(TestSchemaSource.Migrations, []);

        Assert.Equal(name, DatabaseOf(next));
    }

    [Fact]
    public async Task Create_ManyLeasesFromManyThreads_NeverShareADatabaseAndAreAllReturned()
    {
        const int Workers = 8;
        const int Rounds = 5;
        var inUse = new ConcurrentDictionary<string, byte>();

        await Task.WhenAll(Enumerable.Range(0, Workers).Select(worker => Task.Run(
            () =>
            {
                for (var round = 0; round < Rounds; round++)
                {
                    using var database = _backend.Create(TestSchemaSource.Model, []);
                    var name = DatabaseOf(database);
                    Assert.True(inUse.TryAdd(name, 0), "A database was leased twice at the same time.");
                    try
                    {
                        using var context = CreateContext(database);
                        Assert.Empty(context.ActivityLogs);
                        context.ActivityLogs.Add(new ActivityLog("entry", "type", Guid.Empty));
                        context.SaveChanges();
                    }
                    finally
                    {
                        inUse.TryRemove(name, out _);
                    }
                }
            },
            TestContext.Current.CancellationToken))).ConfigureAwait(true);

        Assert.InRange(Directory.GetFiles(_backend.DatabaseDirectory).Length, 1, Workers);
    }

    [Fact]
    public void Dispose_Backend_DropsTheDatabasesThatAreWaitingForALease()
    {
        _backend.Create(TestSchemaSource.Model, []).Dispose();
        Assert.Single(Directory.GetFiles(_backend.DatabaseDirectory));

        _backend.Dispose();

        Assert.False(Directory.Exists(_backend.DatabaseDirectory));
    }

    private static List<string> TableNames(JellyfinDbContext context)
    {
        return context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'").ToList();
    }

    private static Dictionary<string, int> CountRows(TestDatabase database)
    {
        using var context = CreateContext(database);
        var counts = new Dictionary<string, int>();
        foreach (var table in context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'").ToList())
        {
#pragma warning disable EF1002, CA2100 // The table names come from the database.
            counts[table] = context.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM \"{table}\"").Single();
#pragma warning restore EF1002, CA2100
        }

        return counts;
    }

    private static string DatabaseOf(TestDatabase database)
    {
        return RelationalOptionsExtension.Extract(database.Options).ConnectionString!;
    }

    private static JellyfinDbContext CreateContext(TestDatabase database)
    {
        return new JellyfinDbContext(
            database.Options,
            NullLogger<JellyfinDbContext>.Instance,
            database.CreateProvider(),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }
}
