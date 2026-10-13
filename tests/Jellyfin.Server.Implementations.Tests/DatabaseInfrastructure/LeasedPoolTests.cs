using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
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
    public void Dispose_LeaseTwice_DoesNothingTheSecondTime()
    {
        var database = _backend.Create(TestSchemaSource.Model, []);
        using var other = _backend.Create(TestSchemaSource.Model, []);

        database.Dispose();
        database.Dispose();

        Assert.Single(Directory.GetFiles(_backend.DatabaseDirectory));
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
