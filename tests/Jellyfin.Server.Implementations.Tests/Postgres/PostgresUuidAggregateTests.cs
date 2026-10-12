using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Postgres.Migrations;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.FullSystemBackup;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.SystemBackupService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

public sealed class PostgresUuidAggregateTests : IDisposable
{
    private static readonly Guid _lowestId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid _middleId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid _highestId = Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffffe");

    private readonly List<IDisposable> _disposables = [];
    private readonly string _testRoot = Path.Combine(AppContext.BaseDirectory, "jellyfin-uuid-aggregate-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, true);
        }
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task MigratedDatabase_MinOverUuid_ReturnsTheLowestValue()
    {
        await using var context = CreateContext(Migrated());

        var lowest = await context.Database
            .SqlQueryRaw<Guid>("""SELECT min(v) AS "Value" FROM (VALUES ('00000000-0000-0000-0000-000000000002'::uuid), ('ffffffff-ffff-ffff-ffff-fffffffffffe'::uuid), ('00000000-0000-0000-0000-000000000001'::uuid)) AS t(v)""")
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(_lowestId, lowest);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task MigratedDatabase_MinOverUuid_IgnoresNullsAndIsNullWithoutRows()
    {
        await using var context = CreateContext(Migrated());

        var withNulls = await context.Database
            .SqlQueryRaw<Guid?>("""SELECT min(v) AS "Value" FROM (VALUES (NULL::uuid), ('00000000-0000-0000-0000-000000000002'::uuid), (NULL::uuid)) AS t(v)""")
            .SingleAsync(TestContext.Current.CancellationToken);
        var withoutRows = await context.Database
            .SqlQueryRaw<Guid?>("""SELECT min(v) AS "Value" FROM (SELECT NULL::uuid AS v WHERE false) AS t""")
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(_middleId, withNulls);
        Assert.Null(withoutRows);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task MigratedDatabase_MinOverUuid_IsParallelSafeAndHasASortOperator()
    {
        await using var context = CreateContext(Migrated());

        var definition = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT a.aggtransfn::text || ' ' || a.aggcombinefn::text || ' ' || p.proparallel::text || ' ' || (a.aggsortop <> 0)::text AS "Value"
                FROM pg_aggregate a
                JOIN pg_proc p ON p.oid = a.aggfnoid
                WHERE p.oid = to_regprocedure('min(uuid)')
                """)
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal($"{PostgresUuidAggregate.FunctionName} {PostgresUuidAggregate.FunctionName} s true", definition);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task Create_WhenMinOverUuidExists_ChangesNothing()
    {
        await using var context = CreateContext(Migrated());
        await context.Database.ExecuteSqlRawAsync(PostgresUuidAggregate.DropSql, TestContext.Current.CancellationToken);
        await CreateForeignAggregateAsync(context, "min");

        await context.Database.ExecuteSqlRawAsync(PostgresUuidAggregate.CreateSql, TestContext.Current.CancellationToken);

        Assert.False(await ExistsAsync(context, $"{PostgresUuidAggregate.FunctionName}(uuid, uuid)"));
        var result = await context.Database
            .SqlQueryRaw<Guid>("""SELECT min(v) AS "Value" FROM (VALUES ('00000000-0000-0000-0000-000000000001'::uuid), ('ffffffff-ffff-ffff-ffff-fffffffffffe'::uuid)) AS t(v)""")
            .SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(_highestId, result);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task Drop_RemovesTheFunctionAndItsAggregateAndNothingElse()
    {
        await using var context = CreateContext(Migrated());
        await CreateForeignAggregateAsync(context, "max");

        await context.Database.ExecuteSqlRawAsync(PostgresUuidAggregate.DropSql, TestContext.Current.CancellationToken);

        Assert.False(await ExistsAsync(context, $"{PostgresUuidAggregate.FunctionName}(uuid, uuid)"));
        Assert.False(await ExistsAsync(context, "min(uuid)"));
        Assert.True(await ExistsAsync(context, "max(uuid)"));
        Assert.True(await ExistsAsync(context, "foreign_larger(uuid, uuid)"));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task Drop_WhenMinOverUuidIsNotOurs_KeepsIt()
    {
        await using var context = CreateContext(Migrated());
        await context.Database.ExecuteSqlRawAsync(PostgresUuidAggregate.DropSql, TestContext.Current.CancellationToken);
        await CreateForeignAggregateAsync(context, "min");

        await context.Database.ExecuteSqlRawAsync(PostgresUuidAggregate.DropSql, TestContext.Current.CancellationToken);

        Assert.True(await ExistsAsync(context, "min(uuid)"));
        Assert.True(await ExistsAsync(context, "foreign_larger(uuid, uuid)"));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task Drop_WhenAViewUsesTheAggregate_FailsAndKeepsEverything()
    {
        await using var context = CreateContext(Migrated());
        await context.Database.ExecuteSqlRawAsync("""CREATE VIEW lowest_item AS SELECT min("Id") AS "Id" FROM "BaseItems" """, TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => context.Database.ExecuteSqlRawAsync(PostgresUuidAggregate.DropSql, TestContext.Current.CancellationToken));

        Assert.Equal(PostgresErrorCodes.DependentObjectsStillExist, exception.SqlState);
        Assert.True(await ExistsAsync(context, "min(uuid)"));
        Assert.True(await ExistsAsync(context, $"{PostgresUuidAggregate.FunctionName}(uuid, uuid)"));
        Assert.True(await ViewExistsAsync(context, "lowest_item"));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task Drop_WhenAViewUsesTheFunction_FailsAndKeepsTheAggregateToo()
    {
        await using var context = CreateContext(Migrated());
        await context.Database.ExecuteSqlRawAsync(
            $"CREATE VIEW smaller_of_two AS SELECT {PostgresUuidAggregate.FunctionName}(gen_random_uuid(), gen_random_uuid()) AS \"Id\"",
            TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => context.Database.ExecuteSqlRawAsync(PostgresUuidAggregate.DropSql, TestContext.Current.CancellationToken));

        // The aggregate is dropped first, so it only survives because the statement is atomic.
        Assert.Equal(PostgresErrorCodes.DependentObjectsStillExist, exception.SqlState);
        Assert.True(await ExistsAsync(context, "min(uuid)"));
        Assert.True(await ExistsAsync(context, $"{PostgresUuidAggregate.FunctionName}(uuid, uuid)"));
        Assert.True(await ViewExistsAsync(context, "smaller_of_two"));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task RestoredJsonBackup_CanBeGroupedByPresentationKey()
    {
        var source = Migrated();
        var target = Migrated();
        var expected = await SeedGroupsAsync(source);

        var manifest = await CreateBackupService(source, "source").CreateBackupAsync(new BackupOptionsDto());
        await CreateBackupService(target, "target").RestoreBackupAsync(manifest.Path);

        Assert.Equal(expected, await LowestIdPerKeyAsync(target));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task DatabaseRestoredFromPgDump_CanBeGroupedByPresentationKey()
    {
        Assert.SkipUnless(PostgresTestBackend.Instance.CanRunServerTools, "pg_dump is only available on a container that the tests start.");
        var source = Migrated();
        var expected = await SeedGroupsAsync(source);
        var target = PostgresTestBackend.Instance.CreateEmpty();
        _disposables.Add(target);

        PostgresTestBackend.Instance.CopyThroughDump(source, target);

        Assert.Equal(expected, await LowestIdPerKeyAsync(target));
    }

    private static async Task CreateForeignAggregateAsync(JellyfinDbContext context, string aggregateName)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE FUNCTION foreign_larger(a uuid, b uuid) RETURNS uuid
                LANGUAGE sql IMMUTABLE STRICT
                AS $body$ SELECT CASE WHEN b > a THEN b ELSE a END $body$
            """,
            TestContext.Current.CancellationToken);

        var create = aggregateName switch
        {
            "min" => "CREATE AGGREGATE min(uuid) (SFUNC = foreign_larger, STYPE = uuid)",
            "max" => "CREATE AGGREGATE max(uuid) (SFUNC = foreign_larger, STYPE = uuid)",
            _ => throw new ArgumentOutOfRangeException(nameof(aggregateName), aggregateName, null)
        };
        await context.Database.ExecuteSqlRawAsync(create, TestContext.Current.CancellationToken);
    }

    private static async Task<bool> ExistsAsync(JellyfinDbContext context, string signature)
    {
        return await context.Database
            .SqlQuery<bool>($"""SELECT to_regprocedure({signature}) IS NOT NULL AS "Value" """)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<bool> ViewExistsAsync(JellyfinDbContext context, string name)
    {
        return await context.Database
            .SqlQuery<bool>($"""SELECT to_regclass({name}) IS NOT NULL AS "Value" """)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<Dictionary<string, Guid>> SeedGroupsAsync(TestDatabase database)
    {
        // Three items share a key, two share another, one is alone.
        var ids = new[]
        {
            ("a", "0b000000-0000-0000-0000-000000000003"),
            ("a", "0a000000-0000-0000-0000-000000000002"),
            ("a", "0c000000-0000-0000-0000-000000000001"),
            ("b", "f0000000-0000-0000-0000-000000000004"),
            ("b", "e0000000-0000-0000-0000-000000000005"),
            ("c", "10000000-0000-0000-0000-000000000006")
        };

        await using var context = CreateContext(database);
        context.BaseItems.AddRange(ids.Select(e => new BaseItemEntity { Id = Guid.Parse(e.Item2), Type = "Movie", PresentationUniqueKey = e.Item1 }));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return ids.GroupBy(e => e.Item1).ToDictionary(g => g.Key, g => g.Select(e => Guid.Parse(e.Item2)).Min());
    }

    private static async Task<Dictionary<string, Guid>> LowestIdPerKeyAsync(TestDatabase database)
    {
        await using var context = CreateContext(database);
        var rows = await context.BaseItems
            .Where(e => e.PresentationUniqueKey != null)
            .GroupBy(e => e.PresentationUniqueKey!)
            .Select(g => new { g.Key, Id = g.Min(e => e.Id) })
            .ToListAsync(TestContext.Current.CancellationToken);
        return rows.ToDictionary(e => e.Key, e => e.Id);
    }

    private static JellyfinDbContext CreateContext(TestDatabase database) => new(
        database.Options,
        NullLogger<JellyfinDbContext>.Instance,
        database.CreateProvider(),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    private TestDatabase Migrated()
    {
        var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Migrations, []);
        _disposables.Add(database);
        return database;
    }

    private BackupService CreateBackupService(TestDatabase database, string name)
    {
        var root = Path.Combine(_testRoot, name);
        var provider = database.CreateProvider();
        var factory = new TestDbContextFactory(() => CreateContext(database));
        provider.DbContextFactory = factory;

        var applicationHost = new Mock<IServerApplicationHost>();
        applicationHost.Setup(a => a.ApplicationVersion).Returns(new Version(10, 11, 0));

        var applicationPaths = new Mock<IServerApplicationPaths>();
        applicationPaths.Setup(a => a.BackupPath).Returns(Path.Combine(_testRoot, "Backup"));
        applicationPaths.Setup(a => a.ConfigurationDirectoryPath).Returns(Path.Combine(root, "Config"));
        applicationPaths.Setup(a => a.DataPath).Returns(Path.Combine(root, "Data"));
        applicationPaths.Setup(a => a.CachePath).Returns(Path.Combine(root, "Cache"));
        applicationPaths.Setup(a => a.ProgramDataPath).Returns(root);
        applicationPaths.Setup(a => a.RootFolderPath).Returns(Path.Combine(root, "Root"));
        applicationPaths.Setup(a => a.InternalMetadataPath).Returns(Path.Combine(root, "Metadata"));
        applicationPaths.Setup(a => a.DefaultInternalMetadataPath).Returns(Path.Combine(root, "MetadataDefault"));

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.IsScanRunning).Returns(false);

        Directory.CreateDirectory(Path.Combine(_testRoot, "Backup"));
        Directory.CreateDirectory(Path.Combine(root, "Config"));

        var routineCatalog = new Mock<IMigrationRoutineCatalog>();
        routineCatalog.Setup(c => c.RoutineIds).Returns(new HashSet<string>(StringComparer.Ordinal));

        return new BackupService(
            NullLogger<BackupService>.Instance,
            factory,
            applicationHost.Object,
            applicationPaths.Object,
            provider,
            new Mock<IHostApplicationLifetime>().Object,
            libraryManager.Object,
            routineCatalog.Object);
    }
}
