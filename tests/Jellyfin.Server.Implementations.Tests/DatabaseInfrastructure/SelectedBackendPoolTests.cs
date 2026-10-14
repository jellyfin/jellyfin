using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

/// <summary>
/// Holds the pooled backend of the selected engine to the expectations that the pool is held to on SQLite files. The
/// tests need a server of the MySQL family, which is the only family that is pooled.
/// </summary>
public class SelectedBackendPoolTests
{
    private static ITestDatabaseBackend Backend => TestDatabaseEnvironment.Backend;

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public void Dispose_ReturnedLease_LeavesEveryTableAsInAFreshDatabase()
    {
        PoolChecks.AssertResetMatchesFreshDatabase(Backend, TestSchemaSource.Model);
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public void Dispose_ReturnedLease_RestartsIdentityAtOneOnEveryTable()
    {
        PoolChecks.AssertIdentityRestartsAtOne(Backend, TestSchemaSource.Model);
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public void Dispose_ReturnedLease_RestoresSeedData()
    {
        PoolChecks.AssertSeedDataIsRestored(Backend);
    }

    [ProviderTheory(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    [InlineData(SchemaChange.Table)]
    [InlineData(SchemaChange.Index)]
    [InlineData(SchemaChange.Trigger)]
    [InlineData(SchemaChange.Column)]
    public void Dispose_LeaseThatChangedTheSchema_IsDroppedInsteadOfReused(SchemaChange change)
    {
        PoolChecks.AssertChangedSchemaIsNotReused(Backend, change);
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public void Create_AfterLeaseWasReturned_ReusesTheDatabase()
    {
        string first;
        using (var database = Backend.Create(TestSchemaSource.Model, []))
        {
            first = PoolChecks.DatabaseOf(database);
        }

        using var again = Backend.Create(TestSchemaSource.Model, []);

        Assert.Equal(first, PoolChecks.DatabaseOf(again));
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public void Create_TwoLeasesAtTheSameTime_GetDifferentDatabases()
    {
        using var first = Backend.Create(TestSchemaSource.Model, []);
        using var second = Backend.Create(TestSchemaSource.Model, []);

        Assert.NotEqual(PoolChecks.DatabaseOf(first), PoolChecks.DatabaseOf(second));
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public async Task Create_ManyLeasesFromManyThreads_UseDifferentDatabases()
    {
        const int Leases = 6;
        var databases = await Task.WhenAll(
            Enumerable.Range(0, Leases).Select(_ => Task.Run(() => Backend.Create(TestSchemaSource.Model, []), TestContext.Current.CancellationToken)))
            .ConfigureAwait(true);
        try
        {
            Assert.Equal(Leases, databases.Select(PoolChecks.DatabaseOf).Distinct().Count());
        }
        finally
        {
            foreach (var database in databases)
            {
                database.Dispose();
            }
        }
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public void CreateEmpty_HasNoSchemaAndIsDroppedWhenDisposed()
    {
        var database = Backend.CreateEmpty();
        using (var context = PoolChecks.CreateContext(database))
        {
            Assert.False(context.GetService<IRelationalDatabaseCreator>().HasTables());
            context.Database.EnsureCreated();
            Assert.True(context.GetService<IRelationalDatabaseCreator>().HasTables());
        }

        database.Dispose();

        Assert.False(CanConnect(database));
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public void Create_Fresh_ReturnsANewDatabaseThatIsDroppedAfterwards()
    {
        string pooled;
        using (var database = Backend.Create(TestSchemaSource.Model, []))
        {
            pooled = PoolChecks.DatabaseOf(database);
        }

        TestDatabase fresh;
        using (fresh = Backend.Create(TestSchemaSource.Model, [], fresh: true))
        {
            Assert.NotEqual(pooled, PoolChecks.DatabaseOf(fresh));
            Assert.True(CanConnect(fresh));
        }

        Assert.False(CanConnect(fresh));
        using var reused = Backend.Create(TestSchemaSource.Model, []);
        Assert.Equal(pooled, PoolChecks.DatabaseOf(reused));
    }

    internal static bool CanConnect(TestDatabase database)
    {
        using var context = PoolChecks.CreateContext(database);
        return context.Database.CanConnect();
    }
}
