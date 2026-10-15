using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Database.Implementations.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Database.Testing.Conformance;

/// <summary>
/// Kit row: the provider's purge, which a restore and the reset of a reused test database run, empties exactly the
/// tables it is given and starts their identity columns again at 1; and a database the backend hands out, fresh or
/// not, holds only the seed data, with every identity column starting at 1.
/// </summary>
public abstract class PurgeConformance : ConformanceTestBase
{
    [ConformanceFact]
    public async Task PurgeDatabase_EveryTableOfTheModel_EmptiesThemAndStartsEveryIdentityAtOne()
    {
        var tables = Tables();
        foreach (var table in tables)
        {
            var rows = ModelRowFactory.IdentityKey(table) is null ? 1 : 3;
            for (var row = 0; row < rows; row++)
            {
                Rows.Save(table);
            }
        }

        AssertNone(tables.Where(table => DescribeRows(table).Count == 0).Select(table => $"{table.GetTableName()} is empty").ToList());

        await using (var context = CreateDbContext())
        {
            await DatabaseProvider.PurgeDatabase(context, tables.Select(table => table.GetTableName()!));
        }

        AssertNone(tables.Where(table => DescribeRows(table).Count > 0).Select(table => $"{table.GetTableName()} still has rows").ToList());
        AssertIdentitiesStartAtOne(CreateDbContext);
    }

    [ConformanceFact]
    public async Task PurgeDatabase_TableThatHeldRowsAndTableThatNeverDid_BothStartTheirIdentityAtOne()
    {
        var activityLogs = Table<ActivityLog>();
        var apiKeys = Table<ApiKey>();
        for (var row = 0; row < 3; row++)
        {
            Rows.Save(activityLogs);
        }

        await using (var context = CreateDbContext())
        {
            await DatabaseProvider.PurgeDatabase(context, [activityLogs.GetTableName()!, apiKeys.GetTableName()!]);
        }

        Assert.Empty(DescribeRows(activityLogs));
        Assert.Equal(1, NextIdentity(activityLogs));
        Assert.Equal(1, NextIdentity(apiKeys));
    }

    [ConformanceFact]
    public async Task PurgeDatabase_SomeTables_LeavesTheOtherTablesAndTheirIdentitiesAsTheyWere()
    {
        var activityLogs = Table<ActivityLog>();
        var apiKeys = Table<ApiKey>();
        Rows.Save(activityLogs);
        Rows.Save(apiKeys);
        Rows.Save(apiKeys);
        var keptRows = DescribeRows(apiKeys);

        await using (var context = CreateDbContext())
        {
            await DatabaseProvider.PurgeDatabase(context, [activityLogs.GetTableName()!]);
        }

        Assert.Empty(DescribeRows(activityLogs));
        Assert.Equal(keptRows, DescribeRows(apiKeys));
        Assert.Equal(3, NextIdentity(apiKeys));
    }

    [ConformanceFact]
    public async Task PurgeDatabase_NoTables_ChangesNothing()
    {
        var tables = Tables();
        foreach (var table in tables)
        {
            Rows.Save(table);
        }

        var before = tables.ToDictionary(table => table.Name, table => DescribeRows(table), StringComparer.Ordinal);

        await using (var context = CreateDbContext())
        {
            await DatabaseProvider.PurgeDatabase(context, []);
        }

        Assert.All(tables, table => Assert.Equal(before[table.Name], DescribeRows(table)));
    }

    [ConformanceTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreatedDatabase_HoldsOnlyTheSeedDataAndStartsEveryIdentityAtOne(bool fresh)
    {
        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Migrations, [], fresh);
        var provider = database.CreateProvider();
        JellyfinDbContext Create() => new(database.Options, NullLogger<JellyfinDbContext>.Instance, provider, new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

        using (var context = Create())
        {
            var seedModel = context.GetService<IDesignTimeModel>().Model;
            var unexpected = new List<string>();
            foreach (var table in ModelRowFactory.Tables(context.Model))
            {
                var expected = seedModel.FindEntityType(table.Name)!.GetSeedData().Count();
                var actual = ModelRowFactory.DescribeRows(context, table).Count;
                if (actual != expected)
                {
                    unexpected.Add($"{table.GetTableName()}: {actual} row(s), the seed data has {expected}");
                }
            }

            AssertNone(unexpected);
        }

        AssertIdentitiesStartAtOne(Create);
    }

    /// <summary>
    /// Saves a row to every table with an identity column, together with the rows they point at, and checks that the
    /// identities of each table run from 1 without a gap.
    /// </summary>
    /// <param name="createContext">Creates a context on the database.</param>
    private void AssertIdentitiesStartAtOne(Func<JellyfinDbContext> createContext)
    {
        var factory = new ModelRowFactory(createContext);
        var tables = TablesWithIdentity();
        foreach (var table in tables)
        {
            factory.Save(table);
        }

        var gaps = new List<string>();
        foreach (var table in tables)
        {
            var identities = IdentityValues(table, createContext);
            if (!identities.SequenceEqual(Enumerable.Range(1, identities.Count).Select(value => (long)value)))
            {
                gaps.Add($"{table.GetTableName()}: {string.Join(", ", identities)}");
            }
        }

        AssertNone(gaps);
    }
}
