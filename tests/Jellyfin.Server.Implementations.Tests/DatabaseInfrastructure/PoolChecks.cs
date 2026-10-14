using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

/// <summary>
/// The kinds of change to the structure of a database that a test can make, and that a pool must not hand out again.
/// </summary>
public enum SchemaChange
{
    Table,
    Index,
    Trigger,
    Column
}

/// <summary>
/// Checks that hold for the databases of any pool, whatever the engine behind it, so that the pool on SQLite files and
/// the pool of the selected server are held to the same expectations.
/// </summary>
internal static class PoolChecks
{
    public const string MigrationHistoryTable = "__EFMigrationsHistory";

    private static readonly Guid _placeholderId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static string DatabaseOf(TestDatabase database)
    {
        return RelationalOptionsExtension.Extract(database.Options).ConnectionString!;
    }

    public static JellyfinDbContext CreateContext(TestDatabase database)
    {
        return new JellyfinDbContext(
            database.Options,
            NullLogger<JellyfinDbContext>.Instance,
            database.CreateProvider(),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }

    public static string GetSchemaChangeSql(TestDbProvider provider, SchemaChange change)
    {
        return provider switch
        {
            TestDbProvider.Sqlite => change switch
            {
                SchemaChange.Table => "CREATE TABLE \"Scratch\" (\"Id\" INTEGER)",
                SchemaChange.Index => "CREATE INDEX \"IX_Scratch\" ON \"ActivityLogs\" (\"Name\")",
                SchemaChange.Trigger => "CREATE TRIGGER \"Scratch\" AFTER INSERT ON \"ActivityLogs\" BEGIN SELECT 1; END",
                SchemaChange.Column => "ALTER TABLE \"ActivityLogs\" ADD COLUMN \"Scratch\" INTEGER",
                _ => throw new ArgumentOutOfRangeException(nameof(change), change, null)
            },
            TestDbProvider.MySql or TestDbProvider.MariaDb => change switch
            {
                SchemaChange.Table => "CREATE TABLE `Scratch` (`Id` int)",
                SchemaChange.Index => "CREATE INDEX `IX_Scratch` ON `ActivityLogs` (`LogSeverity`)",
                SchemaChange.Trigger => "CREATE TRIGGER `Scratch` BEFORE INSERT ON `ActivityLogs` FOR EACH ROW SET NEW.`Name` = NEW.`Name`",
                SchemaChange.Column => "ALTER TABLE `ActivityLogs` ADD COLUMN `Scratch` int NULL",
                _ => throw new ArgumentOutOfRangeException(nameof(change), change, null)
            },
            _ => throw new NotSupportedException($"There is no statement that changes the schema of {provider}.")
        };
    }

    public static List<string> GetTableNames(JellyfinDbContext context, TestDbProvider provider)
    {
        var sql = provider switch
        {
            TestDbProvider.Sqlite => "SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'",
            TestDbProvider.MySql or TestDbProvider.MariaDb => "SELECT TABLE_NAME AS Value FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'",
            _ => throw new NotSupportedException($"The tables of {provider} cannot be listed.")
        };
        return context.Database.SqlQueryRaw<string>(sql).ToList();
    }

    public static Dictionary<string, long> CountRows(TestDatabase database, TestDbProvider provider)
    {
        using var context = CreateContext(database);
        var helper = context.GetService<ISqlGenerationHelper>();
        var counts = new Dictionary<string, long>();
        foreach (var table in GetTableNames(context, provider))
        {
#pragma warning disable EF1002, CA2100 // The table names come from the database.
            counts[table] = context.Database.SqlQueryRaw<long>($"SELECT COUNT(*) AS Value FROM {helper.DelimitIdentifier(table)}").Single();
#pragma warning restore EF1002, CA2100
        }

        return counts;
    }

    public static void DeleteMigrationHistory(JellyfinDbContext context)
    {
        var table = context.GetService<ISqlGenerationHelper>().DelimitIdentifier(MigrationHistoryTable);
#pragma warning disable EF1002, CA2100 // The table name is a constant.
        context.Database.ExecuteSqlRaw($"DELETE FROM {table}");
#pragma warning restore EF1002, CA2100
    }

    public static void AssertResetMatchesFreshDatabase(ITestDatabaseBackend backend, TestSchemaSource source)
    {
        Dictionary<string, long> expected;
        using (var fresh = backend.Create(source, [], fresh: true))
        {
            expected = CountRows(fresh, backend.Provider);
        }

        Assert.Equal(source == TestSchemaSource.Migrations, expected.ContainsKey(MigrationHistoryTable));
        Assert.All(expected.Where(table => table.Key == MigrationHistoryTable), table => Assert.True(table.Value > 0));

        using (var database = backend.Create(source, []))
        {
            using var context = CreateContext(database);
            ModelRows.Fill(context, 3);

            var dirty = CountRows(database, backend.Provider);
            Assert.All(ModelRows.Tables(context), entityType => Assert.True(dirty[entityType.GetTableName()!] > 0, entityType.GetTableName()));
        }

        using var reused = backend.Create(source, []);

        Assert.Equal(expected, CountRows(reused, backend.Provider));
    }

    public static void AssertIdentityRestartsAtOne(ITestDatabaseBackend backend, TestSchemaSource source)
    {
        using (var database = backend.Create(source, []))
        {
            using var context = CreateContext(database);
            ModelRows.Fill(context, 3);
        }

        using var reused = backend.Create(source, []);
        using var reusedContext = CreateContext(reused);
        var withIdentity = ModelRows.TablesWithIdentity(reusedContext);
        Assert.NotEmpty(withIdentity);

        var added = ModelRows.Fill(reusedContext, 1);

        Assert.All(
            added.Where(row => withIdentity.Contains(row.EntityType)),
            row => Assert.Equal(1, Convert.ToInt64(row.Entry.Property(ModelRows.GetIdentityKey(row.EntityType)!.Name).CurrentValue, CultureInfo.InvariantCulture)));
    }

    public static void AssertSeedDataIsRestored(ITestDatabaseBackend backend)
    {
        using var fresh = backend.Create(TestSchemaSource.Model, [], fresh: true);
        using (var freshContext = CreateContext(fresh))
        {
            Assert.Single(freshContext.BaseItems);
        }

        using (var database = backend.Create(TestSchemaSource.Model, []))
        {
            using var context = CreateContext(database);
            context.BaseItems.ExecuteDelete();
            Assert.Empty(context.BaseItems);
        }

        using var reused = backend.Create(TestSchemaSource.Model, []);
        using var reusedContext = CreateContext(reused);

        var seeded = Assert.Single(reusedContext.BaseItems);
        Assert.Equal(_placeholderId, seeded.Id);
    }

    public static void AssertChangedSchemaIsNotReused(ITestDatabaseBackend backend, SchemaChange change)
    {
        string changed;
        using (var database = backend.Create(TestSchemaSource.Model, []))
        {
            changed = DatabaseOf(database);
            using var context = CreateContext(database);
            context.Database.ExecuteSqlRaw(GetSchemaChangeSql(backend.Provider, change));
        }

        using var next = backend.Create(TestSchemaSource.Model, []);
        using var nextContext = CreateContext(next);

        Assert.NotEqual(changed, DatabaseOf(next));
        Assert.DoesNotContain("Scratch", GetTableNames(nextContext, backend.Provider));
        Assert.DoesNotContain(
            nextContext.Database.SqlQueryRaw<string>(GetSchemaObjectNamesSql(backend.Provider)).ToList(),
            name => name.Contains("Scratch", StringComparison.Ordinal));
    }

    private static string GetSchemaObjectNamesSql(TestDbProvider provider)
    {
        return provider switch
        {
            TestDbProvider.Sqlite => "SELECT name AS Value FROM sqlite_master",
            TestDbProvider.MySql or TestDbProvider.MariaDb =>
                "SELECT INDEX_NAME AS Value FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() "
                + "UNION SELECT TRIGGER_NAME FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA = DATABASE() "
                + "UNION SELECT COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()",
            _ => throw new NotSupportedException($"The schema objects of {provider} cannot be listed.")
        };
    }
}
