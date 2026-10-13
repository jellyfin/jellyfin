using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// The migration that adds the columns with the normalized original title and the lower case name, on the provider
/// the tests run on: it is applied to a database of the previous migration that holds rows, which the routine then
/// fills in.
/// </summary>
public sealed class AddNormalisedMatchColumnsMigrationTests : IDisposable
{
    private const string Target = "20261013120000_AddNormalisedMatchColumns";

    private static readonly Guid _itemWithTitle = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid _itemWithoutTitle = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
    private static readonly Guid _person = Guid.Parse("00000000-0000-0000-0000-0000000000b1");

    private readonly TestDatabase _database = TestDatabaseEnvironment.Backend.CreateEmpty();

    private static string ColumnCountSql => TestDatabaseEnvironment.Provider == TestDbProvider.Sqlite
        ? """
            SELECT (SELECT count(*) FROM pragma_table_info('Peoples') WHERE name = 'NameLower')
                 + (SELECT count(*) FROM pragma_table_info('BaseItems') WHERE name = 'CleanOriginalTitle') AS "Value"
            """
        : """
            SELECT count(*)::int AS "Value" FROM information_schema.columns
            WHERE table_schema = current_schema()
              AND ((table_name = 'Peoples' AND column_name = 'NameLower') OR (table_name = 'BaseItems' AND column_name = 'CleanOriginalTitle'))
            """;

    public void Dispose()
    {
        _database.Dispose();
    }

    [Fact]
    public async Task Migrate_DatabaseOfThePreviousMigrationWithRows_AddsTheColumnsAndKeepsTheRows()
    {
        await using var context = CreateContext();
        var previous = GetPreviousMigration(context);
        await Migrator(context).MigrateAsync(previous, TestContext.Current.CancellationToken);
        await SeedPreviousSchemaAsync(context);

        await Migrator(context).MigrateAsync(Target, TestContext.Current.CancellationToken);

        var items = await SeededItemsAsync(context);
        Assert.Equal(["Item with title", "Item without title"], items.Select(e => e.Name));
        Assert.Equal(["Émile Zola", null], items.Select(e => e.OriginalTitle));
        Assert.Equal([null, null], items.Select(e => e.CleanOriginalTitle));
        var person = await context.Peoples.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Émile Zola", person.Name);
        Assert.Equal(string.Empty, person.NameLower);
        Assert.Contains(Target, await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken));
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task Migrate_ThenTheRoutine_FillsTheColumnsOfTheExistingRows()
    {
        await using var context = CreateContext();
        await Migrator(context).MigrateAsync(GetPreviousMigration(context), TestContext.Current.CancellationToken);
        await SeedPreviousSchemaAsync(context);
        await Migrator(context).MigrateAsync(Target, TestContext.Current.CancellationToken);

        await new BackfillNormalisedMatchColumns(
            new StartupLogger<BackfillNormalisedMatchColumns>(NullLogger<BackfillNormalisedMatchColumns>.Instance),
            new TestDbContextFactory(CreateContext)).PerformAsync(TestContext.Current.CancellationToken);

        await using var check = CreateContext();
        var items = await SeededItemsAsync(check);
        Assert.Equal(["emile zola", null], items.Select(e => e.CleanOriginalTitle));
        var person = await check.Peoples.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("émile zola", person.NameLower);
    }

    [Fact]
    public async Task Migrate_NameLowerIndex_IsAPlainIndexOnTheColumn()
    {
        await using var context = CreateContext();

        await Migrator(context).MigrateAsync(Target, TestContext.Current.CancellationToken);

        var definition = await IndexDefinitionAsync(context);
        Assert.Contains("\"NameLower\"", definition, StringComparison.Ordinal);
        Assert.DoesNotContain("lower(", definition, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Migrate_BackToThePreviousMigration_RestoresTheExpressionIndexAndDropsTheColumns()
    {
        await using var context = CreateContext();
        var previous = GetPreviousMigration(context);
        await Migrator(context).MigrateAsync(Target, TestContext.Current.CancellationToken);

        await Migrator(context).MigrateAsync(previous, TestContext.Current.CancellationToken);

        var definition = await IndexDefinitionAsync(context);
        Assert.Contains("lower(\"Name\")", definition, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            0,
            await context.Database
                .SqlQueryRaw<int>(ColumnCountSql)
                .SingleAsync(TestContext.Current.CancellationToken));
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public async Task GenerateScript_OnSqlite_AddsTheColumnsWithoutRebuildingATable()
    {
        await using var context = CreateContext();

        var script = Migrator(context).GenerateScript(GetPreviousMigration(context), Target);

        Assert.Contains("ALTER TABLE \"BaseItems\" ADD \"CleanOriginalTitle\" TEXT NULL;", script, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE \"Peoples\" ADD \"NameLower\" TEXT NOT NULL DEFAULT '';", script, StringComparison.Ordinal);
        Assert.DoesNotContain("CREATE TABLE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RENAME TO", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PRAGMA foreign_keys", script, StringComparison.OrdinalIgnoreCase);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task GenerateScript_OnPostgres_AddsTheColumnsWithoutRewritingATable()
    {
        await using var context = CreateContext();

        var script = Migrator(context).GenerateScript(GetPreviousMigration(context), Target);

        Assert.Contains("ALTER TABLE \"BaseItems\" ADD \"CleanOriginalTitle\" text COLLATE \"C\";", script, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE \"Peoples\" ADD \"NameLower\" text COLLATE \"C\" NOT NULL DEFAULT '';", script, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP TABLE", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ALTER TABLE \"Peoples\" ALTER COLUMN \"NameLower\" DROP DEFAULT;", script, StringComparison.Ordinal);
        Assert.DoesNotContain("TYPE", script, StringComparison.Ordinal);
    }

    private static IMigrator Migrator(DbContext context) => context.GetService<IMigrator>();

    private static string GetPreviousMigration(DbContext context)
    {
        var migrations = context.Database.GetMigrations().ToList();
        var index = migrations.IndexOf(Target);
        Assert.True(index > 0, $"{Target} is not one of the migrations of the provider.");
        return migrations[index - 1];
    }

    private static Task<List<BaseItemEntity>> SeededItemsAsync(JellyfinDbContext context) => context.BaseItems
        .AsNoTracking()
        .Where(e => e.Id.Equals(_itemWithTitle) || e.Id.Equals(_itemWithoutTitle))
        .OrderBy(e => e.Name)
        .ToListAsync(TestContext.Current.CancellationToken);

    private static async Task<string> IndexDefinitionAsync(DbContext context)
    {
        var sql = TestDatabaseEnvironment.Provider == TestDbProvider.Sqlite
            ? """SELECT sql AS "Value" FROM sqlite_master WHERE type = 'index' AND name = 'IX_Peoples_NameLower'"""
            : """SELECT indexdef AS "Value" FROM pg_indexes WHERE schemaname = current_schema() AND indexname = 'IX_Peoples_NameLower'""";
        return await context.Database.SqlQueryRaw<string>(sql).SingleAsync(TestContext.Current.CancellationToken);
    }

    private static async Task SeedPreviousSchemaAsync(DbContext context)
    {
        var token = TestContext.Current.CancellationToken;
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "BaseItems" ("Id", "Type", "Name", "OriginalTitle", "IsFolder", "IsInMixedFolder", "IsLocked", "IsMovie", "IsRepeat", "IsSeries", "IsVirtualItem")
            VALUES ({_itemWithTitle}, {"Movie"}, {"Item with title"}, {"Émile Zola"}, false, false, false, true, false, false, false)
            """,
            token);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "BaseItems" ("Id", "Type", "Name", "IsFolder", "IsInMixedFolder", "IsLocked", "IsMovie", "IsRepeat", "IsSeries", "IsVirtualItem")
            VALUES ({_itemWithoutTitle}, {"Movie"}, {"Item without title"}, false, false, false, true, false, false, false)
            """,
            token);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""INSERT INTO "Peoples" ("Id", "Name", "PersonType") VALUES ({_person}, {"Émile Zola"}, {"Actor"})""",
            token);
    }

    private JellyfinDbContext CreateContext() => new(
        _database.Options,
        NullLogger<JellyfinDbContext>.Instance,
        _database.CreateProvider(),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
}
