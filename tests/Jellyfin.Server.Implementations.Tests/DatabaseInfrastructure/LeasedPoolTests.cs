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

    [Theory]
    [InlineData(TestSchemaSource.Model)]
    [InlineData(TestSchemaSource.Migrations)]
    public void Create_AfterLeaseWasReturned_ReusesTheDatabase(TestSchemaSource source)
    {
        PoolChecks.AssertReturnedDatabaseIsReused(_backend, source);

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
        PoolChecks.AssertResetMatchesFreshDatabase(_backend, source);
    }

    [Theory]
    [InlineData(TestSchemaSource.Model)]
    [InlineData(TestSchemaSource.Migrations)]
    public void Dispose_ReturnedLease_RestartsIdentityAtOneOnEveryTable(TestSchemaSource source)
    {
        PoolChecks.AssertIdentityRestartsAtOne(_backend, source);
    }

    [Fact]
    public void Dispose_ReturnedLease_RestoresSeedData()
    {
        PoolChecks.AssertSeedDataIsRestored(_backend);
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
    [InlineData(SchemaChange.Table)]
    [InlineData(SchemaChange.Index)]
    [InlineData(SchemaChange.Trigger)]
    [InlineData(SchemaChange.Column)]
    public void Dispose_LeaseThatChangedTheSchema_IsDroppedInsteadOfReused(SchemaChange change)
    {
        PoolChecks.AssertChangedSchemaIsNotReused(_backend, change);

        Assert.Single(Directory.GetFiles(_backend.DatabaseDirectory));
    }

    [Fact]
    public void Dispose_LeaseThatDeletedTheMigrationHistory_IsDroppedInsteadOfReused()
    {
        PoolChecks.AssertChangedMigrationHistoryIsNotReused(_backend, PoolChecks.DeleteMigrationHistory);

        Assert.Single(Directory.GetFiles(_backend.DatabaseDirectory));
    }

    [Fact]
    public void Dispose_LeaseThatRecordedAnotherMigration_IsDroppedInsteadOfReused()
    {
        PoolChecks.AssertChangedMigrationHistoryIsNotReused(_backend, PoolChecks.RecordAnotherMigration);

        Assert.Single(Directory.GetFiles(_backend.DatabaseDirectory));
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

    private static string DatabaseOf(TestDatabase database) => PoolChecks.DatabaseOf(database);

    private static JellyfinDbContext CreateContext(TestDatabase database) => PoolChecks.CreateContext(database);
}
