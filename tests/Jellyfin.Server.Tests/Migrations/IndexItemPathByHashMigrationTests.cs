using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// The migration that makes the index on the path of an item a hash index on PostgreSQL, applied to a database of the
/// previous migration that holds items. The other providers keep their index, so their migration has no operations.
/// </summary>
public sealed class IndexItemPathByHashMigrationTests : IDisposable
{
    private const string Target = "20261016043615_IndexItemPathByHash";

    private static readonly Guid _first = Guid.Parse("00000000-0000-0000-0000-0000000000c1");
    private static readonly Guid _second = Guid.Parse("00000000-0000-0000-0000-0000000000c2");

    private readonly TestDatabase _database = TestDatabaseEnvironment.Backend.CreateEmpty();

    public void Dispose()
    {
        _database.Dispose();
    }

    [MigrationFact(Target)]
    public async Task Migrate_DatabaseOfThePreviousMigrationWithItems_KeepsThemAndFindsThemByPath()
    {
        await using var context = CreateContext();
        await Migrator(context).MigrateAsync(GetPreviousMigration(context), TestContext.Current.CancellationToken);
        await SeedAsync(_first, "/movies/first.mkv");
        await SeedAsync(_second, "/movies/second.mkv");

        await Migrator(context).MigrateAsync(Target, TestContext.Current.CancellationToken);

        var paths = new[] { "/movies/first.mkv", "/movies/second.mkv" };
        Assert.Equal(
            [_first],
            await context.BaseItems.Where(e => e.Path == "/movies/first.mkv").Select(e => e.Id).ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(
            [_first, _second],
            (await context.BaseItems.Where(e => e.Path != null && paths.Contains(e.Path)).Select(e => e.Id).ToListAsync(TestContext.Current.CancellationToken)).Order());
        Assert.Contains(Target, await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken));
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [MigrationFact(Target)]
    public async Task Migrate_ThenAPathOverTheBtreeEntryLimit_IsSaved()
    {
        await using var context = CreateContext();
        await Migrator(context).MigrateAsync(Target, TestContext.Current.CancellationToken);
        var path = LongPath();

        await SeedAsync(_first, path);

        Assert.Equal(
            [_first],
            await context.BaseItems.Where(e => e.Path == path).Select(e => e.Id).ToListAsync(TestContext.Current.CancellationToken));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task Migrate_OnPostgres_ReplacesTheBtreeIndexThatRefusedALongPathByAHashIndex()
    {
        await using var context = CreateContext();
        await Migrator(context).MigrateAsync(GetPreviousMigration(context), TestContext.Current.CancellationToken);
        Assert.Contains("USING btree (\"Path\" NULLS FIRST)", await PathIndexDefinitionAsync(context), StringComparison.Ordinal);
        var refused = await Assert.ThrowsAsync<DbUpdateException>(() => SeedAsync(_first, LongPath()));
        Assert.Equal("54000", Assert.IsType<PostgresException>(refused.InnerException).SqlState);

        await Migrator(context).MigrateAsync(Target, TestContext.Current.CancellationToken);

        Assert.EndsWith("USING hash (\"Path\")", await PathIndexDefinitionAsync(context), StringComparison.Ordinal);
        await SeedAsync(_first, LongPath());
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task Migrate_OnPostgresBackToThePreviousMigration_RestoresTheBtreeIndex()
    {
        await using var context = CreateContext();
        var previous = GetPreviousMigration(context);
        await Migrator(context).MigrateAsync(Target, TestContext.Current.CancellationToken);

        await Migrator(context).MigrateAsync(previous, TestContext.Current.CancellationToken);

        Assert.EndsWith("USING btree (\"Path\" NULLS FIRST)", await PathIndexDefinitionAsync(context), StringComparison.Ordinal);
    }

    [MigrationFact(Target)]
    public async Task GenerateScript_ChangesTheIndexOnPostgresOnly()
    {
        await using var context = CreateContext();

        var script = Migrator(context).GenerateScript(GetPreviousMigration(context), Target);

        if (TestDatabaseEnvironment.Provider == TestDbProvider.Postgres)
        {
            AssertInOrder(script, "DROP INDEX \"IX_BaseItems_Path\";", "CREATE INDEX \"IX_BaseItems_Path\" ON \"BaseItems\" USING hash (\"Path\");");
        }
        else
        {
            // The script only records the migration in the history table.
            Assert.DoesNotContain("CREATE INDEX", script, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DROP INDEX", script, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BaseItems", script, StringComparison.Ordinal);
            Assert.Contains(Target, script, StringComparison.Ordinal);
        }
    }

    [MigrationFact(Target)]
    public async Task GenerateScript_BackToThePreviousMigration_RestoresTheBtreeIndexOnPostgresOnly()
    {
        await using var context = CreateContext();

        var script = Migrator(context).GenerateScript(Target, GetPreviousMigration(context));

        if (TestDatabaseEnvironment.Provider == TestDbProvider.Postgres)
        {
            // The default access method, btree, is not named in the statement.
            AssertInOrder(script, "DROP INDEX \"IX_BaseItems_Path\";", "CREATE INDEX \"IX_BaseItems_Path\" ON \"BaseItems\" (\"Path\" NULLS FIRST);");
            Assert.DoesNotContain("USING hash", script, StringComparison.Ordinal);
        }
        else
        {
            // The script only removes the migration from the history table.
            Assert.DoesNotContain("CREATE INDEX", script, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DROP INDEX", script, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BaseItems", script, StringComparison.Ordinal);
            Assert.Contains(Target, script, StringComparison.Ordinal);
        }
    }

    private static void AssertInOrder(string script, string first, string second)
    {
        var firstAt = script.IndexOf(first, StringComparison.Ordinal);
        var secondAt = script.IndexOf(second, StringComparison.Ordinal);
        Assert.True(firstAt >= 0, $"The script does not contain {first}{Environment.NewLine}{script}");
        Assert.True(secondAt > firstAt, $"The script does not contain {second} after {first}{Environment.NewLine}{script}");
    }

    private static IMigrator Migrator(DbContext context) => context.GetService<IMigrator>();

    private static string GetPreviousMigration(DbContext context)
    {
        var migrations = context.Database.GetMigrations().ToList();
        var index = migrations.IndexOf(Target);
        Assert.True(index > 0, $"{Target} is not one of the migrations of the provider.");
        return migrations[index - 1];
    }

    // Letters in no order, so that the engine cannot compress the value under the limit of a btree index entry.
    private static string LongPath()
    {
        var random = new Random(5);
        return "/movies/" + new string(Enumerable.Range(0, 3016).Select(_ => (char)random.Next('a', 'z' + 1)).ToArray());
    }

    private static Task<string> PathIndexDefinitionAsync(DbContext context) => context.Database
        .SqlQueryRaw<string>("""SELECT indexdef AS "Value" FROM pg_indexes WHERE schemaname = current_schema() AND indexname = 'IX_BaseItems_Path'""")
        .SingleAsync(TestContext.Current.CancellationToken);

    private async Task SeedAsync(Guid id, string path)
    {
        // The migration changes an index only, so the previous migration has the columns of the model.
        await using var context = CreateContext();
        context.BaseItems.Add(new BaseItemEntity { Id = id, Type = "Movie", Name = "Movie", Path = path, IsMovie = true });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private JellyfinDbContext CreateContext() => new(
        _database.Options,
        NullLogger<JellyfinDbContext>.Instance,
        _database.CreateProvider(),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
}
