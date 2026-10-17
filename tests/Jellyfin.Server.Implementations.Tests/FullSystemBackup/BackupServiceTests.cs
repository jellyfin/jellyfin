using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.FullSystemBackup;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.SystemBackupService;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.FullSystemBackup;

/// <summary>
/// Tests for <see cref="BackupService"/>, in particular that a single row of corrupt
/// <see cref="KeyframeData"/> (e.g. malformed <c>KeyframeTicks</c> JSON) does not abort
/// an otherwise healthy backup. See https://github.com/jellyfin/jellyfin/issues/17216.
/// </summary>
public sealed class BackupServiceTests : DatabaseTestFixture
{
    // The database configuration of the server that restores, and the one inside the archive.
    private const string ThisServerDatabaseConfiguration = """
        <?xml version="1.0" encoding="utf-8"?>
        <DatabaseConfigurationOptions>
          <DatabaseType>PLUGIN_PROVIDER</DatabaseType>
          <CustomProviderOptions>
            <PluginName>ExampleDatabase</PluginName>
            <PluginAssembly>Example.Database</PluginAssembly>
            <ConnectionString>Host=database;Database=jellyfin;Username=jellyfin;Password=target</ConnectionString>
            <Options />
          </CustomProviderOptions>
          <LockingBehavior>NoLock</LockingBehavior>
        </DatabaseConfigurationOptions>
        """;

    private const string ArchivedDatabaseConfiguration = """
        <?xml version="1.0" encoding="utf-8"?>
        <DatabaseConfigurationOptions>
          <DatabaseType>Jellyfin-SQLite</DatabaseType>
          <LockingBehavior>Pessimistic</LockingBehavior>
        </DatabaseConfigurationOptions>
        """;

    private const string RoutineInArchiveId = "20250101120000_RoutineInArchive";
    private const string RoutineOnlyInTargetId = "20250101130000_RoutineOnlyInTarget";
    private const string ForeignSchemaMigrationId = "20250101140000_SchemaMigrationOfAnotherEngine";
    private const string RemovedRoutineId = "20250101150000_RoutineThisServerNoLongerHas";

    private static readonly Guid MovieId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherMovieId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid RockId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid NulRockId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private readonly string _testRoot;
    private readonly string _backupPath;
    private readonly string _configurationDirectoryPath;
    private readonly RecordingLogger<BackupService> _logger = new();
    private readonly List<string> _identityScopeEvents = [];
    private readonly List<IEntityType> _identityScopeTypes = [];
    private readonly List<string> _unsavedWhenScopeClosed = [];
    private int _purgeCalls;
    private bool _failCompletion;
    private bool _supportsTransactionalMigrations = true;

    public BackupServiceTests()
    {
        // Use the test assembly's own output directory instead of Path.GetTempPath(). On GitHub-hosted
        // windows-latest runners, the system temp directory lives on the constrained C: drive, which can have
        // less than the 5GiB BackupService requires free, causing spurious failures. AppContext.BaseDirectory
        // is under the repo checkout (the much larger D: drive on Windows runners) on all platforms.
        _testRoot = Path.Combine(AppContext.BaseDirectory, "jellyfin-backup-service-tests-" + Guid.NewGuid().ToString("N"));
        _backupPath = Path.Combine(_testRoot, "Backup");
        _configurationDirectoryPath = Path.Combine(_testRoot, "Config");
        Directory.CreateDirectory(_backupPath);
        Directory.CreateDirectory(_configurationDirectoryPath);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, true);
        }

        base.Dispose(disposing);
    }

    // SQLite keeps KeyframeTicks as JSON text, which a damaged row can hold; PostgreSQL stores an array of bigint.
    [ProviderFact(TestDbProvider.Sqlite)]
    public async Task CreateBackupAsync_WithCorruptKeyframeDataRow_SkipsRowAndCompletesBackup()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var validItemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var corruptItemId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        await using (var ctx = CreateDbContext())
        {
            // A healthy item + keyframe row, written the normal way.
            ctx.BaseItems.Add(CreateMovieEntity(validItemId, "Good Movie"));
            ctx.BaseItems.Add(CreateMovieEntity(corruptItemId, "Corrupt Movie"));
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);

            ctx.KeyframeData.Add(new KeyframeData
            {
                ItemId = validItemId,
                TotalDuration = 60_000,
                KeyframeTicks = [0, 1000, 2000]
            });
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);

            // Simulate a corrupted database row: truncated JSON array for KeyframeTicks,
            // written directly via SQL to bypass EF's normal (well-formed) write path.
            await ctx.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO \"KeyframeData\" (\"ItemId\", \"TotalDuration\", \"KeyframeTicks\") VALUES ({corruptItemId}, {5000L}, {"[1,2,3"})",
                cancellationToken).ConfigureAwait(true);
        }

        var backupService = CreateBackupService();

        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);

        Assert.True(File.Exists(manifest.Path));

        using var archive = await ZipFile.OpenReadAsync(manifest.Path, cancellationToken).ConfigureAwait(true);
        var keyframeEntry = archive.GetEntry("Database/KeyframeData.json");
        Assert.NotNull(keyframeEntry);

        await using var entryStream = await keyframeEntry!.OpenAsync(cancellationToken).ConfigureAwait(true);
        using var document = await JsonDocument.ParseAsync(entryStream, cancellationToken: cancellationToken).ConfigureAwait(true);

        var rows = document.RootElement.EnumerateArray().ToList();

        // The corrupt row must be skipped, but the valid row must still make it into the backup.
        var singleRow = Assert.Single(rows);
        Assert.Equal(validItemId, singleRow.GetProperty("ItemId").GetGuid());
    }

    [Fact]
    public async Task RestoreBackupAsync_WithHomeSections_RestoresThem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedHomeSectionsAsync(cancellationToken).ConfigureAwait(true);
        var expected = await DisplayPreferencesRowsAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);

        // The sections change after the backup, so only the restore can bring them back.
        await using (var ctx = CreateDbContext())
        {
            var sections = await ctx.Set<HomeSection>().OrderBy(e => e.Order).ToListAsync(cancellationToken).ConfigureAwait(true);
            sections[0].Type = HomeSectionType.NextUp;
            ctx.Set<HomeSection>().Remove(sections[1]);
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
        }

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        Assert.Equal(expected, await DisplayPreferencesRowsAsync(cancellationToken).ConfigureAwait(true));
    }

    [Fact]
    public async Task RestoreBackupAsync_AfterDataWasChanged_RestoresTheBackedUpRows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var expected = await SnapshotAsync(cancellationToken).ConfigureAwait(true);

        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);

        await using (var ctx = CreateDbContext())
        {
            ctx.Permissions.RemoveRange(ctx.Permissions);
            ctx.AccessSchedules.RemoveRange(ctx.AccessSchedules);
            ctx.Devices.RemoveRange(ctx.Devices);
            ctx.ItemValuesMap.RemoveRange(ctx.ItemValuesMap);
            ctx.ItemValues.RemoveRange(ctx.ItemValues);
            ctx.BaseItems.RemoveRange(ctx.BaseItems);
            ctx.Users.RemoveRange(ctx.Users);
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);

            ctx.BaseItems.Add(CreateMovieEntity(Guid.NewGuid(), "Added After Backup"));
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
        }

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        Assert.Equal(expected, await SnapshotAsync(cancellationToken).ConfigureAwait(true));
    }

    [Fact]
    public async Task RestoreBackupAsync_AfterRowsWereAdded_RemovesThem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var expected = await SnapshotAsync(cancellationToken).ConfigureAwait(true);

        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);

        await using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateMovieEntity(Guid.NewGuid(), "Added After Backup"));
            ctx.Users.Add(new User("added", "provider", "reset"));
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
        }

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        Assert.Equal(expected, await SnapshotAsync(cancellationToken).ConfigureAwait(true));
    }

    [Theory]
    [InlineData("KeyframeData", "[{\"ItemId\":\"11111111-1111-1111-11")]
    [InlineData("KeyframeData", "[{\"ItemId\":\"not-a-guid\",\"TotalDuration\":1,\"KeyframeTicks\":[]}]")]
    [InlineData("Users", "{\"not\":\"an array\"}")]
    public async Task RestoreBackupAsync_WithCorruptTableEntry_LeavesDatabaseUntouched(string table, string content)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await RewriteEntryAsync(manifest.Path, $"Database/{table}.json", _ => content, cancellationToken).ConfigureAwait(true);

        await using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateMovieEntity(Guid.NewGuid(), "Added After Backup"));
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
        }

        var before = await SnapshotAsync(cancellationToken).ConfigureAwait(true);

        await Assert.ThrowsAnyAsync<Exception>(() => backupService.RestoreBackupAsync(manifest.Path)).ConfigureAwait(true);

        Assert.Equal(0, _purgeCalls);
        Assert.Equal(before, await SnapshotAsync(cancellationToken).ConfigureAwait(true));
    }

    [Fact]
    public async Task RestoreBackupAsync_WithDamagedGeneratedId_LeavesDatabaseUntouched()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await RewriteEntryAsync(
            manifest.Path,
            "Database/Permissions.json",
            json =>
            {
                var permissions = JsonNode.Parse(json)!.AsArray();
                permissions[0]!["Id"] = "not-a-number";
                return permissions.ToJsonString();
            },
            cancellationToken).ConfigureAwait(true);

        var before = await SnapshotAsync(cancellationToken).ConfigureAwait(true);

        await Assert.ThrowsAnyAsync<Exception>(() => backupService.RestoreBackupAsync(manifest.Path)).ConfigureAwait(true);

        Assert.Equal(0, _purgeCalls);
        Assert.Equal(before, await SnapshotAsync(cancellationToken).ConfigureAwait(true));
    }

    [Fact]
    public async Task RestoreBackupAsync_WhenInsertingRowsFails_RollsBackThePurge()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await RewriteEntryAsync(
            manifest.Path,
            "Database/Users.json",
            json =>
            {
                // A second user with the same name but a new id, which the unique index on the name rejects when it is inserted.
                var users = JsonNode.Parse(json)!.AsArray();
                var duplicate = JsonNode.Parse(users[0]!.ToJsonString())!;
                duplicate["Id"] = Guid.NewGuid();
                users.Add(duplicate);
                return users.ToJsonString();
            },
            cancellationToken).ConfigureAwait(true);

        await using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateMovieEntity(Guid.NewGuid(), "Added After Backup"));
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
        }

        var before = await SnapshotAsync(cancellationToken).ConfigureAwait(true);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => backupService.RestoreBackupAsync(manifest.Path)).ConfigureAwait(true);

        EngineTraits.For(DbProvider).AssertUniqueViolation(exception.InnerException);

        Assert.Equal(1, _purgeCalls);
        Assert.Equal(before, await SnapshotAsync(cancellationToken).ConfigureAwait(true));
    }

    [Fact]
    public async Task RestoreBackupAsync_KeepsTheGeneratedIdsOfTheArchive()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        await using (var ctx = CreateDbContext())
        {
            // Removing a row in the middle leaves a gap that is only kept if the ids are restored rather than generated again.
            var second = await ctx.Permissions.OrderBy(e => e.Id).Skip(1).FirstAsync(cancellationToken).ConfigureAwait(true);
            ctx.Permissions.Remove(second);
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
        }

        var expected = await GeneratedIdsAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        Assert.Equal(expected, await GeneratedIdsAsync(cancellationToken).ConfigureAwait(true));
    }

    [Fact]
    public async Task RestoreBackupAsync_GivesNewIdentityRowsTheIdAfterTheHighestRestoredOne()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        await using var ctx = CreateDbContext();
        var highestRestored = await ctx.Permissions.MaxAsync(e => e.Id, cancellationToken).ConfigureAwait(true);
        var bob = await ctx.Users.Include(e => e.Permissions).SingleAsync(e => e.Username == "bob", cancellationToken).ConfigureAwait(true);
        var added = new Permission(PermissionKind.EnableAllFolders, true);
        bob.Permissions.Add(added);
        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);

        Assert.Equal(highestRestored + 1, added.Id);
    }

    [Fact]
    public async Task RestoreBackupAsync_OpensAndClosesAnIdentityScopePerTableBeforeFinishing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        Assert.Contains(typeof(User), _identityScopeTypes.Select(e => e.ClrType));
        Assert.Contains(typeof(Permission), _identityScopeTypes.Select(e => e.ClrType));
        Assert.Equal(_identityScopeTypes.Count, _identityScopeTypes.Distinct().Count());
        var expected = _identityScopeTypes.SelectMany(e => new[] { $"open {e.ClrType.Name}", $"close {e.ClrType.Name}" }).Append("complete");
        Assert.Equal(expected, _identityScopeEvents);
    }

    [Fact]
    public async Task RestoreBackupAsync_SavesEachTableBeforeClosingItsIdentityScope()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        Assert.NotEmpty(_identityScopeEvents);
        Assert.Empty(_unsavedWhenScopeClosed);
    }

    [Fact]
    public async Task RestoreBackupAsync_RestoresTablesBeforeTheTablesThatReferenceThem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        var references = _identityScopeTypes
            .SelectMany(dependent => dependent.GetForeignKeys()
                .Select(key => key.PrincipalEntityType)
                .Where(principal => principal != dependent && _identityScopeTypes.Contains(principal))
                .Select(principal => (Dependent: dependent, Principal: principal)))
            .ToList();
        Assert.NotEmpty(references);
        Assert.All(references, e => Assert.True(
            _identityScopeTypes.IndexOf(e.Principal) < _identityScopeTypes.IndexOf(e.Dependent),
            $"{e.Principal.ClrType.Name} is restored after {e.Dependent.ClrType.Name}, which refers to it"));
    }

    [Fact]
    public async Task RestoreBackupAsync_WhenFinishingFails_RollsBackThePurge()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateMovieEntity(Guid.NewGuid(), "Added After Backup"));
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
        }

        var before = await SnapshotAsync(cancellationToken).ConfigureAwait(true);
        _failCompletion = true;

        await Assert.ThrowsAnyAsync<Exception>(() => backupService.RestoreBackupAsync(manifest.Path)).ConfigureAwait(true);

        Assert.Equal(before, await SnapshotAsync(cancellationToken).ConfigureAwait(true));
    }

    [SchemaMigrationsFact]
    public async Task RestoreBackupAsync_WithHistoryFromAnotherSchema_LeavesNoPendingMigrations()
    {
        var (_, pending) = await RestoreWithRewrittenHistoryAsync().ConfigureAwait(true);

        Assert.Empty(pending);
    }

    [SchemaMigrationsFact]
    public async Task RestoreBackupAsync_KeepsTheSchemaMigrationRowsOfTheTarget()
    {
        var (history, _) = await RestoreWithRewrittenHistoryAsync().ConfigureAwait(true);

        await using var ctx = CreateDbContext();
        var schemaMigrationIds = ctx.GetService<IMigrationsAssembly>().Migrations.Keys;
        Assert.NotEmpty(schemaMigrationIds);
        Assert.All(schemaMigrationIds, id => Assert.Contains(id, history));
    }

    [SchemaMigrationsFact]
    public async Task RestoreBackupAsync_ReplacesCodeRoutinesWithThoseOfTheArchive()
    {
        var (history, _) = await RestoreWithRewrittenHistoryAsync().ConfigureAwait(true);

        Assert.Contains(RoutineInArchiveId, history);
        Assert.DoesNotContain(RoutineOnlyInTargetId, history);
    }

    [SchemaMigrationsFact]
    public async Task RestoreBackupAsync_DoesNotTakeOverSchemaMigrationsOfAnotherEngine()
    {
        var (history, _) = await RestoreWithRewrittenHistoryAsync().ConfigureAwait(true);

        Assert.DoesNotContain(ForeignSchemaMigrationId, history);
    }

    [SchemaMigrationsFact]
    public async Task RestoreBackupAsync_DoesNotTakeOverRoutinesThisServerNoLongerHas()
    {
        var (history, _) = await RestoreWithRewrittenHistoryAsync().ConfigureAwait(true);

        Assert.DoesNotContain(RemovedRoutineId, history);
    }

    [SchemaMigrationsFact]
    public async Task RestoreBackupAsync_WithArchiveOfTheSameSchema_RestoresTheHistoryAsItWas()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InsertHistoryRowAsync(RoutineInArchiveId, cancellationToken).ConfigureAwait(true);
        var before = await ReadHistoryAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await InsertHistoryRowAsync(RoutineOnlyInTargetId, cancellationToken).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        Assert.Equal(before, await ReadHistoryAsync(cancellationToken).ConfigureAwait(true));
    }

    [Fact]
    public async Task RestoreBackupAsync_WithValuesThatDifferOnlyByNul_RestoresOneRow()
    {
        await RestoreWithRockAndNulRockAsync(nulRowFirst: false, mapItemIds: []).ConfigureAwait(true);

        await using var ctx = CreateDbContext();
        var value = await ctx.ItemValues.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("Rock", value.Value);
        Assert.Equal(RockId, value.ItemValueId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RestoreBackupAsync_WithValuesThatDifferOnlyByNul_KeepsTheRowWithoutNul(bool nulRowFirst)
    {
        await RestoreWithRockAndNulRockAsync(nulRowFirst, mapItemIds: [OtherMovieId]).ConfigureAwait(true);

        await using var ctx = CreateDbContext();
        var value = await ctx.ItemValues.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("Rock", value.Value);
        Assert.Equal(RockId, value.ItemValueId);
        var maps = await ctx.ItemValuesMap.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(2, maps.Count);
        Assert.All(maps, e => Assert.Equal(RockId, e.ItemValueId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RestoreBackupAsync_WhenAllRowsThatCollideHadNul_KeepsTheFirstRowOfTheArchive(bool secondRowFirst)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await RewriteJsonArrayAsync(
            manifest.Path,
            "ItemValues",
            values =>
            {
                values[0]!["Value"] = "Ro\0ck";
                var duplicate = JsonNode.Parse(values[0]!.ToJsonString())!;
                duplicate["ItemValueId"] = NulRockId;
                duplicate["Value"] = "Rock\0";
                values.Insert(secondRowFirst ? 0 : values.Count, duplicate);
            },
            cancellationToken).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        await using var ctx = CreateDbContext();
        var value = await ctx.ItemValues.AsNoTracking().SingleAsync(cancellationToken).ConfigureAwait(true);
        Assert.Equal("Rock", value.Value);
        Assert.Equal(secondRowFirst ? NulRockId : RockId, value.ItemValueId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RestoreBackupAsync_WithDisplayPreferencesThatDifferOnlyByNul_KeepsTheRowWithoutNul(bool staleRowFirst)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        int currentId;
        await using (var seedContext = CreateDbContext())
        {
            var alice = await seedContext.Users.SingleAsync(e => e.Username == "alice", cancellationToken).ConfigureAwait(true);
            var preferences = new DisplayPreferences(alice.Id, MovieId, "web");
            seedContext.DisplayPreferences.Add(preferences);
            await seedContext.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
            currentId = preferences.Id;
        }

        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await RewriteJsonArrayAsync(
            manifest.Path,
            "DisplayPreferences",
            rows =>
            {
                var stale = JsonNode.Parse(rows[0]!.ToJsonString())!;
                stale["Id"] = currentId + 100;
                stale["Client"] = "web\0";
                rows.Insert(staleRowFirst ? 0 : rows.Count, stale);
            },
            cancellationToken).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        await using var ctx = CreateDbContext();
        var restored = await ctx.DisplayPreferences.AsNoTracking().SingleAsync(cancellationToken).ConfigureAwait(true);
        Assert.Equal("web", restored.Client);
        Assert.Equal(currentId, restored.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RestoreBackupAsync_WithUsersThatDifferOnlyByNul_PointsTheActivityLogAtTheKeptUser(bool staleRowFirst)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var staleUserId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        Guid bobId;
        await using (var seedContext = CreateDbContext())
        {
            bobId = (await seedContext.Users.SingleAsync(e => e.Username == "bob", cancellationToken).ConfigureAwait(true)).Id;
            seedContext.ActivityLogs.Add(new ActivityLog("Signed in", "SessionStarted", staleUserId));
            await seedContext.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
        }

        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await RewriteJsonArrayAsync(
            manifest.Path,
            "Users",
            rows =>
            {
                var stale = JsonNode.Parse(rows.Single(e => e!["Username"]!.GetValue<string>() == "bob")!.ToJsonString())!;
                stale["Id"] = staleUserId;
                stale["Username"] = "bob\0";
                stale["NormalizedUsername"] = "BOB\0";
                rows.Insert(staleRowFirst ? 0 : rows.Count, stale);
            },
            cancellationToken).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        await using var ctx = CreateDbContext();
        Assert.Equal(1, await ctx.Users.CountAsync(e => e.Username == "bob", cancellationToken).ConfigureAwait(true));
        var log = await ctx.ActivityLogs.AsNoTracking().SingleAsync(cancellationToken).ConfigureAwait(true);
        Assert.Equal(bobId, log.UserId);
    }

    [Fact]
    public async Task RestoreBackupAsync_WithValuesThatDifferOnlyByNul_PointsReferencesToTheDroppedRowAtTheKeptRow()
    {
        await RestoreWithRockAndNulRockAsync(nulRowFirst: false, mapItemIds: [OtherMovieId]).ConfigureAwait(true);

        await using var ctx = CreateDbContext();
        var maps = await ctx.ItemValuesMap.AsNoTracking().OrderBy(e => e.ItemId).ToListAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal([MovieId, OtherMovieId], maps.Select(e => e.ItemId));
        Assert.All(maps, e => Assert.Equal(RockId, e.ItemValueId));
    }

    [Fact]
    public async Task RestoreBackupAsync_WhenReferencesBecomeDuplicates_KeepsOneOfThem()
    {
        await RestoreWithRockAndNulRockAsync(nulRowFirst: false, mapItemIds: [MovieId]).ConfigureAwait(true);

        await using var ctx = CreateDbContext();
        var map = await ctx.ItemValuesMap.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(MovieId, map.ItemId);
        Assert.Equal(RockId, map.ItemValueId);
    }

    [Fact]
    public async Task RestoreBackupAsync_WithNulInAKeyColumnOnly_RemovesIt()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await RewriteJsonArrayAsync(manifest.Path, "ItemValues", values => values[0]!["Value"] = "Ro\0ck", cancellationToken).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        await using var ctx = CreateDbContext();
        var value = await ctx.ItemValues.AsNoTracking().SingleAsync(cancellationToken).ConfigureAwait(true);
        Assert.Equal("Rock", value.Value);
        Assert.Equal(RockId, value.ItemValueId);
    }

    [Fact]
    public async Task RestoreBackupAsync_WithNulInAColumnThatIsNotAKey_RemovesIt()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await RewriteJsonArrayAsync(
            manifest.Path,
            "BaseItems",
            items => items.Single(e => MovieId.Equals(e!["Id"]!.GetValue<Guid>()))!["Name"] = "First\0Movie",
            cancellationToken).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        await using var ctx = CreateDbContext();
        var item = await ctx.BaseItems.AsNoTracking().SingleAsync(e => e.Id.Equals(MovieId), cancellationToken).ConfigureAwait(true);
        Assert.Equal("FirstMovie", item.Name);
    }

    [Fact]
    public void Model_HasNoTableThatRefersToItselfAndHasAUniqueStringColumn()
    {
        using var ctx = CreateDbContext();
        var selfReferencing = ctx.Model.GetEntityTypes()
            .Where(e => e.GetForeignKeys().Any(key => key.PrincipalEntityType == e))
            .ToList();
        var offenders = selfReferencing
            .Where(e => e.GetKeys().Select(key => key.Properties)
                .Concat(e.GetIndexes().Where(index => index.IsUnique).Select(index => index.Properties))
                .Any(properties => properties.Any(property => property.ClrType == typeof(string))))
            .Select(e => e.ClrType.Name)
            .ToList();

        Assert.NotEmpty(selfReferencing);
        Assert.True(
            offenders.Count == 0,
            $"Restored-row cleaning would lose the references of {string.Join(", ", offenders)} to rows of the same table, as it drops or replaces rows whose unique string columns collide once NUL characters are removed.");
    }

    [Fact]
    public void Model_BoundedKeyColumns_AreTheColumnsTheRestoreHasARuleFor()
    {
        using var ctx = CreateDbContext();
        var bounded = ctx.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(e => e.ClrType == typeof(string) && e.GetMaxLength().HasValue && (e.IsKey() || e.GetContainingIndexes().Any()))
            .Select(e => (Entity: e.DeclaringType.ClrType, e.Name))
            .ToList();
        var ruled = RestoredRowCleaner.GetRuledColumns();

        var withoutRule = bounded.Where(e => !ruled.Contains(e)).Select(e => $"{e.Entity.Name}.{e.Name}").ToList();
        var withoutBound = ruled.Where(e => !bounded.Contains(e)).Select(e => $"{e.Entity.Name}.{e.Property}").ToList();

        Assert.NotEmpty(bounded);
        Assert.True(
            withoutRule.Count == 0,
            $"A value in {string.Join(", ", withoutRule)} that is longer than the column is neither shortened, dropped nor refused by the restore. Add a rule for the column to RestoredRowCleaner.");
        Assert.True(
            withoutBound.Count == 0,
            $"RestoredRowCleaner has a rule for {string.Join(", ", withoutBound)}, which is not a bounded key or index column.");
    }

    [Fact]
    public async Task RestoreBackupAsync_WhenRowsAreDropped_LogsTheCountAndTheColumnsButNotTheValues()
    {
        await RestoreWithRockAndNulRockAsync(nulRowFirst: false, mapItemIds: []).ConfigureAwait(true);

        var information = Assert.Single(_logger.Entries, e => e.Level == LogLevel.Information && e.Message.StartsWith("Dropped", StringComparison.Ordinal));
        Assert.Contains("ItemValue", information.Message, StringComparison.Ordinal);
        Assert.Contains("1", information.Message, StringComparison.Ordinal);
        var debug = Assert.Single(_logger.Entries, e => e.Level == LogLevel.Debug && e.Message.StartsWith("Dropped", StringComparison.Ordinal));
        Assert.Contains("ItemValue", debug.Message, StringComparison.Ordinal);
        Assert.Contains("Value", debug.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(_logger.Entries, e => e.Message.Contains("Rock", StringComparison.Ordinal));
    }

    [SchemaMigrationsFact]
    public async Task RestoreBackupAsync_IntoADatabaseThatWasOnlySeeded_CreatesTheSchemaAndRestoresTheRows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var expected = await SnapshotAsync(cancellationToken).ConfigureAwait(true);
        var manifest = await CreateBackupService().CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);

        // What a first start leaves behind: an empty database with the history of the code routines, but no tables.
        using var seededDatabase = TestDatabaseEnvironment.Backend.CreateEmpty();
        var seededOptions = seededDatabase.Options;
        await using (var seeded = CreateDbContext(seededOptions))
        {
            var history = seeded.GetService<IHistoryRepository>();
            await history.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(true);
            await seeded.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(RoutineOnlyInTargetId, "10.11.0")), cancellationToken).ConfigureAwait(true);
        }

        await CreateBackupService(seededOptions).RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        await using var restored = CreateDbContext(seededOptions);
        Assert.Empty(await restored.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(true));
        Assert.Equal(
            expected.Where(e => e.StartsWith("User ", StringComparison.Ordinal)),
            (await restored.Users.AsNoTracking().OrderBy(e => e.Username).ToListAsync(cancellationToken).ConfigureAwait(true)).Select(e => $"User {e.Id} {e.Username}"));
        Assert.Equal(2, await restored.BaseItems.CountAsync(e => e.Type == "Movie", cancellationToken).ConfigureAwait(true));
    }

    [SchemaMigrationsFact]
    public async Task RestoreBackupAsync_WhenMigrationsCannotBeUndoneAndSomeArePending_RefusesAndLeavesTheDatabaseAlone()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var manifest = await CreateBackupWithFilesAsync().ConfigureAwait(true);
        _supportsTransactionalMigrations = false;
        var filesBefore = ReadFilesOfTheServer();

        using var unmigratedDatabase = TestDatabaseEnvironment.Backend.CreateEmpty();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateBackupService(unmigratedDatabase.Options).RestoreBackupAsync(manifest.Path)).ConfigureAwait(true);

        Assert.Contains("--mode MigrateSystem", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Nothing was restored", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Start the server once", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, _purgeCalls);
        Assert.Equal(filesBefore, ReadFilesOfTheServer());
        await using var untouched = CreateDbContext(unmigratedDatabase.Options);
        Assert.NotEmpty(await untouched.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(true));
        Assert.Empty(await untouched.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(true));
    }

    // The marker does not depend on the schema of the database, so this runs on every engine.
    [Fact]
    public async Task RestoreBackupAsync_WhileAMigrationIsMarkedAsUnfinished_RefusesAndLeavesFilesAndDatabaseAlone()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var manifest = await CreateBackupWithFilesAsync().ConfigureAwait(true);
        _supportsTransactionalMigrations = false;
        var markerPath = Path.Combine(_testRoot, "Data", BackupService.MigrationInProgressMarkerFileName);
        await File.WriteAllTextAsync(markerPath, "{}", cancellationToken).ConfigureAwait(true);
        var expected = await SnapshotAsync(cancellationToken).ConfigureAwait(true);
        var filesBefore = ReadFilesOfTheServer();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateBackupService().RestoreBackupAsync(manifest.Path)).ConfigureAwait(true);

        Assert.Contains(markerPath, exception.Message, StringComparison.Ordinal);
        Assert.Contains("mysqldump", exception.Message, StringComparison.Ordinal);
        Assert.Contains("--mode MigrateSystem", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Nothing was restored", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Start the server once", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, _purgeCalls);
        Assert.Equal(filesBefore, ReadFilesOfTheServer());
        Assert.Equal(expected, await SnapshotAsync(cancellationToken).ConfigureAwait(true));
    }

    [Fact]
    public async Task RestoreBackupAsync_WithNoMarker_RestoresFilesOfTheArchive()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var manifest = await CreateBackupWithFilesAsync().ConfigureAwait(true);

        await CreateBackupService().RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        Assert.Equal(
            "box in the archive",
            await File.ReadAllTextAsync(Path.Combine(_testRoot, "Data", "collections", "box.txt"), cancellationToken).ConfigureAwait(true));
        Assert.Equal(
            "<archived />",
            await File.ReadAllTextAsync(Path.Combine(_configurationDirectoryPath, "system.xml"), cancellationToken).ConfigureAwait(true));
    }

    [Theory]
    [InlineData("Config/database.xml")]
    [InlineData("Config/DATABASE.XML")]
    [InlineData("Config/users/../database.xml")]
    public async Task RestoreBackupAsync_WithDatabaseConfigurationInTheArchive_KeepsThatOfThisServer(string entryName)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await WriteConfigurationFileAsync("system.xml", "<system />", cancellationToken).ConfigureAwait(true);
        await WriteConfigurationFileAsync("database.xml", ThisServerDatabaseConfiguration, cancellationToken).ConfigureAwait(true);
        var manifest = await CreateBackupService().CreateBackupAsync(new BackupOptionsDto { Database = false }).ConfigureAwait(true);
        await AddEntryAsync(manifest.Path, entryName, ArchivedDatabaseConfiguration, cancellationToken).ConfigureAwait(true);
        var databaseConfigurationPath = Path.Combine(_configurationDirectoryPath, "database.xml");
        var bytesBefore = await File.ReadAllBytesAsync(databaseConfigurationPath, cancellationToken).ConfigureAwait(true);
        var filesBefore = ListConfigurationFiles();

        await CreateBackupService().RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        // A copy under another casing must not appear beside the file (a file system that tells the casings apart would keep both).
        Assert.Equal(filesBefore, ListConfigurationFiles());
        Assert.Equal(bytesBefore, await File.ReadAllBytesAsync(databaseConfigurationPath, cancellationToken).ConfigureAwait(true));
    }

    [Fact]
    public async Task RestoreBackupAsync_WithDatabaseConfigurationInTheArchive_StillRestoresTheOtherConfiguration()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var userFile = Path.Combine("users", "0f4e5a50d3c64f6c8d8a3f2c4b1a9e77", "x.xml");

        // A user directory may hold a file of the same name; only the one in the configuration directory itself is kept.
        var userDatabaseFile = Path.Combine("users", "0f4e5a50d3c64f6c8d8a3f2c4b1a9e77", "database.xml");
        string[] files = ["system.xml", "network.xml", "encoding.xml", userFile, userDatabaseFile];
        foreach (var file in files)
        {
            await WriteConfigurationFileAsync(file, "<archived />", cancellationToken).ConfigureAwait(true);
        }

        await WriteConfigurationFileAsync("database.xml", ThisServerDatabaseConfiguration, cancellationToken).ConfigureAwait(true);
        var manifest = await CreateBackupService().CreateBackupAsync(new BackupOptionsDto { Database = false }).ConfigureAwait(true);
        await AddEntryAsync(manifest.Path, "Config/database.xml", ArchivedDatabaseConfiguration, cancellationToken).ConfigureAwait(true);
        foreach (var file in files)
        {
            await WriteConfigurationFileAsync(file, "<changed after the backup />", cancellationToken).ConfigureAwait(true);
        }

        await CreateBackupService().RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        foreach (var file in files)
        {
            Assert.Equal(
                "<archived />",
                await File.ReadAllTextAsync(Path.Combine(_configurationDirectoryPath, file), cancellationToken).ConfigureAwait(true));
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_WithAnArchiveFromBeforeThisChange_DoesNotApplyItsDatabaseConfiguration()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await WriteConfigurationFileAsync("system.xml", "<archived />", cancellationToken).ConfigureAwait(true);
        await WriteConfigurationFileAsync("database.xml", ThisServerDatabaseConfiguration, cancellationToken).ConfigureAwait(true);
        var manifest = await CreateBackupService().CreateBackupAsync(new BackupOptionsDto { Database = false }).ConfigureAwait(true);

        // What a server made before this change wrote: the same engine version and the database configuration among the files.
        const string ManifestOfAnOlderServer = """{"ServerVersion":"10.11.0","BackupEngineVersion":"0.2.0","DateCreated":"2026-01-01T00:00:00+00:00","DatabaseTables":[],"Options":{"Metadata":false,"Trickplay":false,"Subtitles":false,"Database":false}}""";
        await AddEntryAsync(manifest.Path, "manifest.json", ManifestOfAnOlderServer, cancellationToken).ConfigureAwait(true);
        await AddEntryAsync(manifest.Path, "Config/database.xml", ArchivedDatabaseConfiguration, cancellationToken).ConfigureAwait(true);
        await WriteConfigurationFileAsync("system.xml", "<changed after the backup />", cancellationToken).ConfigureAwait(true);
        var databaseConfigurationPath = Path.Combine(_configurationDirectoryPath, "database.xml");
        var bytesBefore = await File.ReadAllBytesAsync(databaseConfigurationPath, cancellationToken).ConfigureAwait(true);

        await CreateBackupService().RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        Assert.Equal(bytesBefore, await File.ReadAllBytesAsync(databaseConfigurationPath, cancellationToken).ConfigureAwait(true));
        Assert.Equal(
            "<archived />",
            await File.ReadAllTextAsync(Path.Combine(_configurationDirectoryPath, "system.xml"), cancellationToken).ConfigureAwait(true));
    }

    [Fact]
    public async Task RestoreBackupAsync_WhenThisServerHasNoDatabaseConfiguration_DoesNotCreateOne()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await WriteConfigurationFileAsync("system.xml", "<system />", cancellationToken).ConfigureAwait(true);
        var manifest = await CreateBackupService().CreateBackupAsync(new BackupOptionsDto { Database = false }).ConfigureAwait(true);
        await AddEntryAsync(manifest.Path, "Config/database.xml", ArchivedDatabaseConfiguration, cancellationToken).ConfigureAwait(true);
        var databaseConfigurationPath = Path.Combine(_configurationDirectoryPath, "database.xml");
        Assert.False(File.Exists(databaseConfigurationPath));

        await CreateBackupService().RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        Assert.False(File.Exists(databaseConfigurationPath));
    }

    [Fact]
    public async Task RestoreBackupAsync_WhenMigrationsCannotBeUndoneAndNoneArePending_RestoresTheRows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var expected = await SnapshotAsync(cancellationToken).ConfigureAwait(true);
        _supportsTransactionalMigrations = false;
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateMovieEntity(Guid.NewGuid(), "Added After Backup"));
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
        }

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        Assert.Equal(expected, await SnapshotAsync(cancellationToken).ConfigureAwait(true));
    }

    [Fact]
    public async Task RestoreBackupAsync_WithDeviceIdOverTheBound_RestoresItShortened()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var deviceId = new string('d', KeyNormalizer.DeviceIdMaxLength + 100);
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await RewriteJsonArrayAsync(manifest.Path, "Devices", devices => devices[0]!["DeviceId"] = deviceId, cancellationToken).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        await using var ctx = CreateDbContext();
        var device = await ctx.Devices.AsNoTracking().SingleAsync(cancellationToken).ConfigureAwait(true);
        Assert.Equal(KeyNormalizer.NormalizeDeviceId(deviceId), device.DeviceId);
    }

    [Fact]
    public async Task RestoreBackupAsync_WithUserDataKeyOverTheBound_RestoresItShortened()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var key = new string('k', KeyNormalizer.UserDataKeyMaxLength + 44);
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        await using (var ctx = CreateDbContext())
        {
            var userId = await ctx.Users.Select(e => e.Id).FirstAsync(cancellationToken).ConfigureAwait(true);
            ctx.UserData.Add(new UserData { CustomDataKey = "placeholder", ItemId = MovieId, UserId = userId, Item = null, User = null });
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
        }

        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await RewriteJsonArrayAsync(manifest.Path, "UserData", rows => rows[0]!["CustomDataKey"] = key, cancellationToken).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        await using var restored = CreateDbContext();
        var userData = await restored.UserData.AsNoTracking().SingleAsync(cancellationToken).ConfigureAwait(true);
        Assert.Equal(KeyNormalizer.NormalizeUserDataKey(key), userData.CustomDataKey);
    }

    // The bound counts the characters of a column on MySQL and MariaDB and the UTF-16 units of a string everywhere
    // else. A value of four-byte characters that is cut in the middle of a pair would be rejected by a utf8mb4 column.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task RestoreBackupAsync_WithKeysOfFourByteCharactersOverTheBound_RestoresThemShortenedWholeCharacters(int padding)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        const string Clapper = "\U0001F3AC";
        var deviceId = new string('d', padding) + string.Concat(Enumerable.Repeat(Clapper, KeyNormalizer.DeviceIdMaxLength));
        var dataKey = new string('k', padding) + string.Concat(Enumerable.Repeat(Clapper, KeyNormalizer.UserDataKeyMaxLength));
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        await using (var ctx = CreateDbContext())
        {
            var userId = await ctx.Users.Select(e => e.Id).FirstAsync(cancellationToken).ConfigureAwait(true);
            ctx.UserData.Add(new UserData { CustomDataKey = "placeholder", ItemId = MovieId, UserId = userId, Item = null, User = null });
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
        }

        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await RewriteJsonArrayAsync(manifest.Path, "Devices", devices => devices[0]!["DeviceId"] = deviceId, cancellationToken).ConfigureAwait(true);
        await RewriteJsonArrayAsync(manifest.Path, "UserData", rows => rows[0]!["CustomDataKey"] = dataKey, cancellationToken).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        await using var restored = CreateDbContext();
        var restoredDeviceId = (await restored.Devices.AsNoTracking().SingleAsync(cancellationToken).ConfigureAwait(true)).DeviceId;
        var restoredDataKey = (await restored.UserData.AsNoTracking().SingleAsync(cancellationToken).ConfigureAwait(true)).CustomDataKey;
        Assert.Equal(KeyNormalizer.NormalizeDeviceId(deviceId), restoredDeviceId);
        Assert.Equal(KeyNormalizer.NormalizeUserDataKey(dataKey), restoredDataKey);
        Assert.InRange(restoredDeviceId.Length, 1, KeyNormalizer.DeviceIdMaxLength);
        Assert.InRange(restoredDataKey.Length, 1, KeyNormalizer.UserDataKeyMaxLength);
        Assert.Contains(Clapper, restoredDeviceId, StringComparison.Ordinal);
        Assert.Contains(Clapper, restoredDataKey, StringComparison.Ordinal);

        // A lone surrogate cannot be encoded, which is also why a utf8mb4 column would reject it.
        var strict = new UTF8Encoding(false, true);
        Assert.NotEmpty(strict.GetBytes(restoredDeviceId));
        Assert.NotEmpty(strict.GetBytes(restoredDataKey));
    }

    [Fact]
    public async Task RestoreBackupAsync_WithRoleOverTheBound_RestoresItCutAndTrimmed()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var personId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var role = new string('r', KeyNormalizer.PersonRoleMaxLength - 1) + " tail";
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        await using (var ctx = CreateDbContext())
        {
            ctx.Peoples.Add(new People { Id = personId, Name = "Person", NameLower = MatchNormalizer.NameLower("Person") });
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
            ctx.PeopleBaseItemMap.Add(new PeopleBaseItemMap { ItemId = MovieId, PeopleId = personId, Role = "role", Item = null!, People = null! });
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
        }

        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await RewriteJsonArrayAsync(manifest.Path, "PeopleBaseItemMap", rows => rows[0]!["Role"] = role, cancellationToken).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        await using var restored = CreateDbContext();
        var map = await restored.PeopleBaseItemMap.AsNoTracking().SingleAsync(cancellationToken).ConfigureAwait(true);
        Assert.Equal(new string('r', KeyNormalizer.PersonRoleMaxLength - 1), map.Role);
    }

    [Fact]
    public async Task RestoreBackupAsync_WithValuesThatAreEqualAfterTheCut_RestoresOneValueForBoth()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var prefix = new string('v', KeyNormalizer.ItemValueMaxLength);
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await RewriteJsonArrayAsync(
            manifest.Path,
            "ItemValues",
            values =>
            {
                var duplicate = JsonNode.Parse(values[0]!.ToJsonString())!;
                duplicate["ItemValueId"] = NulRockId;
                duplicate["Value"] = prefix + "second";
                values[0]!["Value"] = prefix + "first";
                values.Add(duplicate);
            },
            cancellationToken).ConfigureAwait(true);
        await RewriteJsonArrayAsync(
            manifest.Path,
            "ItemValuesMap",
            maps =>
            {
                var map = JsonNode.Parse(maps[0]!.ToJsonString())!;
                map["ItemValueId"] = NulRockId;
                map["ItemId"] = OtherMovieId;
                maps.Add(map);
            },
            cancellationToken).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        await using var ctx = CreateDbContext();
        var value = await ctx.ItemValues.AsNoTracking().SingleAsync(cancellationToken).ConfigureAwait(true);
        Assert.Equal(prefix, value.Value);
        Assert.Equal(prefix, value.CleanValue);
        var maps = await ctx.ItemValuesMap.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(true);
        Assert.Equal(2, maps.Count);
        Assert.All(maps, e => Assert.Equal(value.ItemValueId, e.ItemValueId));
    }

    [Fact]
    public async Task RestoreBackupAsync_BackupFromBeforeTheNormalizedColumns_RestoresThemFilledIn()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedNormalizedSourcesAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await RewriteJsonArrayAsync(manifest.Path, "BaseItems", rows => RemoveProperty(rows, "CleanOriginalTitle"), cancellationToken).ConfigureAwait(true);
        await RewriteJsonArrayAsync(manifest.Path, "Peoples", rows => RemoveProperty(rows, "NameLower"), cancellationToken).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        await AssertNormalizedColumnsAsync(cancellationToken).ConfigureAwait(true);
    }

    [Fact]
    public async Task RestoreBackupAsync_BackupWithEmptyOrStaleNormalizedColumns_RestoresThemRecomputed()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedNormalizedSourcesAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await RewriteJsonArrayAsync(
            manifest.Path,
            "BaseItems",
            rows =>
            {
                foreach (var row in rows)
                {
                    row!["CleanOriginalTitle"] = Guid.Parse(row["Id"]!.GetValue<string>()).Equals(MovieId) ? string.Empty : "stale";
                }
            },
            cancellationToken).ConfigureAwait(true);
        await RewriteJsonArrayAsync(
            manifest.Path,
            "Peoples",
            rows =>
            {
                foreach (var row in rows)
                {
                    row!["NameLower"] = "stale";
                }
            },
            cancellationToken).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        await AssertNormalizedColumnsAsync(cancellationToken).ConfigureAwait(true);
    }

    private static void RemoveProperty(JsonArray rows, string name)
    {
        foreach (var row in rows)
        {
            Assert.True(row!.AsObject().Remove(name));
        }
    }

    private async Task SeedNormalizedSourcesAsync(CancellationToken cancellationToken)
    {
        await SeedAsync(cancellationToken).ConfigureAwait(false);
        await using var ctx = CreateDbContext();
        await ctx.BaseItems.Where(e => e.Id.Equals(MovieId))
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.OriginalTitle, "Émile Zola").SetProperty(e => e.CleanOriginalTitle, "emile zola"), cancellationToken)
            .ConfigureAwait(false);
        ctx.Peoples.Add(new People { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Émile ZOLA", NameLower = "émile zola" });
        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task AssertNormalizedColumnsAsync(CancellationToken cancellationToken)
    {
        await using var restored = CreateDbContext();
        var items = await restored.BaseItems.AsNoTracking().ToDictionaryAsync(e => e.Id, e => e.CleanOriginalTitle, cancellationToken).ConfigureAwait(false);
        Assert.Equal("emile zola", items[MovieId]);
        Assert.Null(items[OtherMovieId]);
        var person = await restored.Peoples.AsNoTracking().SingleAsync(cancellationToken).ConfigureAwait(false);
        Assert.Equal("émile zola", person.NameLower);
    }

    [Fact]
    public async Task RestoreBackupAsync_WithProviderIdOverTheBound_DropsItAndKeepsTheOthers()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        await using (var ctx = CreateDbContext())
        {
            ctx.BaseItemProviders.Add(new BaseItemProvider { ItemId = MovieId, ProviderId = "Imdb", ProviderValue = "tt1", Item = null! });
            ctx.BaseItemProviders.Add(new BaseItemProvider { ItemId = MovieId, ProviderId = "Tmdb", ProviderValue = "2", Item = null! });
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
        }

        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await RewriteJsonArrayAsync(
            manifest.Path,
            "BaseItemProviders",
            rows => rows.Single(e => e!["ProviderId"]!.GetValue<string>() == "Tmdb")!["ProviderId"] = new string('p', KeyNormalizer.ProviderIdMaxLength + 1),
            cancellationToken).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        await using var restored = CreateDbContext();
        Assert.Equal("Imdb", (await restored.BaseItemProviders.AsNoTracking().SingleAsync(cancellationToken).ConfigureAwait(true)).ProviderId);
        Assert.Single(_logger.Entries, e => e.Level == LogLevel.Warning && e.Message.StartsWith("Dropped 1 provider ids", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("BaseItems", "Type", 256)]
    [InlineData("BaseItems", "MediaType", 64)]
    [InlineData("Devices", "AccessToken", 128)]
    [InlineData("CustomItemDisplayPreferences", "Key", 256)]
    [InlineData("CustomItemDisplayPreferences", "Client", 32)]
    [InlineData("DisplayPreferences", "Client", 32)]
    [InlineData("Users", "Username", 255)]
    [InlineData("Users", "NormalizedUsername", 255)]
    public async Task RestoreBackupAsync_WithValueOverTheBoundThatIsNotShortened_FailsBeforeThePurgeNamingTheColumn(string table, string column, int maxLength)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        await using (var ctx = CreateDbContext())
        {
            var user = await ctx.Users.FirstAsync(cancellationToken).ConfigureAwait(true);
            ctx.CustomItemDisplayPreferences.Add(new CustomItemDisplayPreferences(user.Id, MovieId, "client", "key", "value"));
            ctx.DisplayPreferences.Add(new DisplayPreferences(user.Id, MovieId, "client"));
            await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(true);
        }

        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);
        await RewriteJsonArrayAsync(manifest.Path, table, rows => rows[0]![column] = new string('x', maxLength + 1), cancellationToken).ConfigureAwait(true);
        var before = await SnapshotAsync(cancellationToken).ConfigureAwait(true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => backupService.RestoreBackupAsync(manifest.Path)).ConfigureAwait(true);

        Assert.Contains(table, exception.Message, StringComparison.Ordinal);
        Assert.Contains(column, exception.Message, StringComparison.Ordinal);
        Assert.Contains(maxLength.ToString(System.Globalization.CultureInfo.InvariantCulture), exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, _purgeCalls);
        Assert.Equal(before, await SnapshotAsync(cancellationToken).ConfigureAwait(true));
    }

    private async Task RestoreWithRockAndNulRockAsync(bool nulRowFirst, Guid[] mapItemIds)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);

        // An archive from SQLite, which stores NUL characters, holding a value twice: with and without the character.
        await RewriteJsonArrayAsync(
            manifest.Path,
            "ItemValues",
            values =>
            {
                var duplicate = JsonNode.Parse(values[0]!.ToJsonString())!;
                duplicate["ItemValueId"] = NulRockId;
                duplicate["Value"] = "Rock\0";
                values.Insert(nulRowFirst ? 0 : values.Count, duplicate);
            },
            cancellationToken).ConfigureAwait(true);

        await RewriteJsonArrayAsync(
            manifest.Path,
            "ItemValuesMap",
            maps =>
            {
                foreach (var itemId in mapItemIds)
                {
                    var map = JsonNode.Parse(maps[0]!.ToJsonString())!;
                    map["ItemValueId"] = NulRockId;
                    map["ItemId"] = itemId;
                    maps.Add(map);
                }
            },
            cancellationToken).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);
    }

    private static Task RewriteJsonArrayAsync(string archivePath, string table, Action<JsonArray> rewrite, CancellationToken cancellationToken)
    {
        return RewriteEntryAsync(
            archivePath,
            $"Database/{table}.json",
            json =>
            {
                var rows = JsonNode.Parse(json)!.AsArray();
                rewrite(rows);
                return rows.ToJsonString();
            },
            cancellationToken);
    }

    private async Task<(IReadOnlyList<string> History, IReadOnlyList<string> Pending)> RestoreWithRewrittenHistoryAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await InsertHistoryRowAsync(RoutineInArchiveId, cancellationToken).ConfigureAwait(true);
        var backupService = CreateBackupService();
        var manifest = await backupService.CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);

        // An archive taken by a server that knew fewer schema migrations than this one, plus a schema migration that
        // only another database engine knows and a routine that this server has since removed.
        await RewriteEntryAsync(
            manifest.Path,
            "Database/HistoryRow.json",
            json =>
            {
                var rows = JsonNode.Parse(json)!.AsArray();
                var schemaRows = rows.Where(e => e!["MigrationId"]!.GetValue<string>() != RoutineInArchiveId).ToList();
                foreach (var row in schemaRows.OrderBy(e => e!["MigrationId"]!.GetValue<string>(), StringComparer.Ordinal).TakeLast(2))
                {
                    rows.Remove(row);
                }

                rows.Add(new JsonObject { ["MigrationId"] = ForeignSchemaMigrationId, ["ProductVersion"] = "9.0.0" });
                rows.Add(new JsonObject { ["MigrationId"] = RemovedRoutineId, ["ProductVersion"] = "9.0.0" });
                return rows.ToJsonString();
            },
            cancellationToken).ConfigureAwait(true);

        await InsertHistoryRowAsync(RoutineOnlyInTargetId, cancellationToken).ConfigureAwait(true);

        await backupService.RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        await using var ctx = CreateDbContext();
        var history = (await ctx.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(true)).ToList();
        var pending = (await ctx.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(true)).ToList();
        return (history, pending);
    }

    private async Task<List<(string MigrationId, string ProductVersion)>> ReadHistoryAsync(CancellationToken cancellationToken)
    {
        await using var ctx = CreateDbContext();
        var rows = await ctx.GetService<IHistoryRepository>().GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(row => (row.MigrationId, row.ProductVersion)).OrderBy(row => row.MigrationId, StringComparer.Ordinal).ToList();
    }

    private async Task InsertHistoryRowAsync(string migrationId, CancellationToken cancellationToken)
    {
        await using var ctx = CreateDbContext();
        await ctx.Database.ExecuteSqlRawAsync(ctx.GetService<IHistoryRepository>().GetInsertScript(new HistoryRow(migrationId, "10.11.0")), cancellationToken).ConfigureAwait(false);
    }

    private static async Task RewriteEntryAsync(string archivePath, string entryName, Func<string, string> transform, CancellationToken cancellationToken)
    {
        using var archive = await ZipFile.OpenAsync(archivePath, ZipArchiveMode.Update, cancellationToken).ConfigureAwait(false);
        var entry = archive.GetEntry(entryName)!;
        string content;
        var readStream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using (readStream.ConfigureAwait(false))
        using (var reader = new StreamReader(readStream))
        {
            content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        entry.Delete();
        var replacement = archive.CreateEntry(entryName);
        var writeStream = await replacement.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using (writeStream.ConfigureAwait(false))
        using (var writer = new StreamWriter(writeStream))
        {
            await writer.WriteAsync(transform(content)).ConfigureAwait(false);
        }
    }

    private async Task SeedHomeSectionsAsync(CancellationToken cancellationToken)
    {
        await using var ctx = CreateDbContext();
        var user = new User("alice", "provider", "reset");
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // The removed row leaves a gap, so the sections only point at their preferences if the ids are restored.
        var removed = new DisplayPreferences(user.Id, Guid.Empty, "removed");
        var preferences = new DisplayPreferences(user.Id, Guid.Empty, "web");
        preferences.HomeSections.Add(new HomeSection { Order = 0, Type = HomeSectionType.LatestMedia });
        preferences.HomeSections.Add(new HomeSection { Order = 1, Type = HomeSectionType.Resume });
        ctx.DisplayPreferences.AddRange(removed, preferences);
        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        ctx.DisplayPreferences.Remove(removed);
        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<string>> DisplayPreferencesRowsAsync(CancellationToken cancellationToken)
    {
        await using var ctx = CreateDbContext();
        var rows = new List<string>();
        foreach (var row in await ctx.DisplayPreferences.AsNoTracking().OrderBy(e => e.Id).ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add($"DisplayPreferences {row.Id} {row.UserId} {row.Client}");
        }

        foreach (var row in await ctx.Set<HomeSection>().AsNoTracking().OrderBy(e => e.Id).ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add($"HomeSection {row.Id} {row.DisplayPreferencesId} {row.Order} {row.Type}");
        }

        return rows;
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        var alice = new User("alice", "provider", "reset");
        alice.Permissions.Add(new Permission(PermissionKind.IsAdministrator, true));
        alice.Permissions.Add(new Permission(PermissionKind.EnableAllFolders, false));
        var bob = new User("bob", "provider", "reset");
        bob.Permissions.Add(new Permission(PermissionKind.IsAdministrator, false));

        var rock = new ItemValue
        {
            ItemValueId = RockId,
            Type = ItemValueType.Genre,
            Value = "Rock",
            CleanValue = "rock"
        };

        await using var ctx = CreateDbContext();
        ctx.Users.AddRange(alice, bob);
        ctx.BaseItems.Add(CreateMovieEntity(MovieId, "First Movie"));
        ctx.BaseItems.Add(CreateMovieEntity(OtherMovieId, "Second Movie"));
        ctx.ItemValues.Add(rock);
        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        ctx.AccessSchedules.Add(new AccessSchedule(DynamicDayOfWeek.Weekday, 8, 17, alice.Id));
        ctx.Devices.Add(new Device(alice.Id, "App", "1.0", "Phone", "device-1"));
        ctx.ItemValuesMap.Add(new ItemValueMap { ItemId = MovieId, ItemValueId = rock.ItemValueId, Item = null!, ItemValue = null! });
        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<string>> SnapshotAsync(CancellationToken cancellationToken)
    {
        await using var ctx = CreateDbContext();
        var rows = new List<string>();

        foreach (var row in await ctx.Users.AsNoTracking().OrderBy(e => e.Username).ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add($"User {row.Id} {row.Username}");
        }

        foreach (var row in await ctx.Permissions.AsNoTracking().OrderBy(e => e.UserId).ThenBy(e => e.Kind).ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add($"Permission {row.UserId} {row.Kind} {row.Value}");
        }

        foreach (var row in await ctx.AccessSchedules.AsNoTracking().OrderBy(e => e.UserId).ThenBy(e => e.DayOfWeek).ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add($"AccessSchedule {row.UserId} {row.DayOfWeek} {row.StartHour} {row.EndHour}");
        }

        foreach (var row in await ctx.Devices.AsNoTracking().OrderBy(e => e.DeviceId).ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add($"Device {row.UserId} {row.AccessToken} {row.DeviceId}");
        }

        foreach (var row in await ctx.BaseItems.AsNoTracking().OrderBy(e => e.Id).ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add($"BaseItem {row.Id} {row.Name}");
        }

        foreach (var row in await ctx.ItemValues.AsNoTracking().OrderBy(e => e.ItemValueId).ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add($"ItemValue {row.ItemValueId} {row.Type} {row.Value} {row.CleanValue}");
        }

        foreach (var row in await ctx.ItemValuesMap.AsNoTracking().OrderBy(e => e.ItemId).ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add($"ItemValueMap {row.ItemId} {row.ItemValueId}");
        }

        return rows;
    }

    private async Task<List<string>> GeneratedIdsAsync(CancellationToken cancellationToken)
    {
        await using var ctx = CreateDbContext();
        var ids = new List<string>();
        ids.AddRange((await ctx.Permissions.AsNoTracking().OrderBy(e => e.Id).Select(e => e.Id).ToListAsync(cancellationToken).ConfigureAwait(false)).Select(e => $"Permission {e}"));
        ids.AddRange((await ctx.AccessSchedules.AsNoTracking().OrderBy(e => e.Id).Select(e => e.Id).ToListAsync(cancellationToken).ConfigureAwait(false)).Select(e => $"AccessSchedule {e}"));
        ids.AddRange((await ctx.Devices.AsNoTracking().OrderBy(e => e.Id).Select(e => e.Id).ToListAsync(cancellationToken).ConfigureAwait(false)).Select(e => $"Device {e}"));
        return ids;
    }

    private async Task<BackupManifestDto> CreateBackupWithFilesAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var collections = Path.Combine(_testRoot, "Data", "collections");
        Directory.CreateDirectory(collections);
        await File.WriteAllTextAsync(Path.Combine(collections, "box.txt"), "box in the archive", cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(_configurationDirectoryPath, "system.xml"), "<archived />", cancellationToken).ConfigureAwait(false);
        var manifest = await CreateBackupService().CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(false);

        // What is on disk after the backup differs from what the archive holds, so a restore that wrote files would show.
        File.Delete(Path.Combine(collections, "box.txt"));
        await File.WriteAllTextAsync(Path.Combine(_configurationDirectoryPath, "system.xml"), "<changed after the backup />", cancellationToken).ConfigureAwait(false);
        return manifest;
    }

    private List<string> ReadFilesOfTheServer()
    {
        return new[] { _configurationDirectoryPath, Path.Combine(_testRoot, "Data") }
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            .Order(StringComparer.Ordinal)
            .Select(file => file + "=" + File.ReadAllText(file))
            .ToList();
    }

    private static async Task AddEntryAsync(string archivePath, string entryName, string content, CancellationToken cancellationToken)
    {
        using var archive = await ZipFile.OpenAsync(archivePath, ZipArchiveMode.Update, cancellationToken).ConfigureAwait(false);

        // An archive made by a server that already leaves the file out has no such entry; one made by an older server has it.
        foreach (var existing in archive.Entries.Where(e => string.Equals(e.FullName, entryName, StringComparison.Ordinal)).ToList())
        {
            existing.Delete();
        }

        var entry = archive.CreateEntry(entryName);
        var stream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        using (var writer = new StreamWriter(stream))
        {
            await writer.WriteAsync(content.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task WriteConfigurationFileAsync(string relativePath, string content, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_configurationDirectoryPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, content, cancellationToken).ConfigureAwait(false);
    }

    private List<string> ListConfigurationFiles()
    {
        return Directory.EnumerateFiles(_configurationDirectoryPath, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(_configurationDirectoryPath, file))
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private BackupService CreateBackupService(DbContextOptions<JellyfinDbContext>? dbOptions = null)
    {
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(() => dbOptions is null ? CreateDbContext() : CreateDbContext(dbOptions));
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => dbOptions is null ? CreateDbContext() : CreateDbContext(dbOptions));

        var applicationHost = new Mock<IServerApplicationHost>();
        applicationHost.Setup(a => a.ApplicationVersion).Returns(new Version(10, 11, 0));

        var applicationPaths = new Mock<IServerApplicationPaths>();
        applicationPaths.Setup(a => a.BackupPath).Returns(_backupPath);
        applicationPaths.Setup(a => a.ConfigurationDirectoryPath).Returns(_configurationDirectoryPath);
        applicationPaths.Setup(a => a.DataPath).Returns(Path.Combine(_testRoot, "Data"));
        applicationPaths.Setup(a => a.CachePath).Returns(Path.Combine(_testRoot, "Cache"));
        applicationPaths.Setup(a => a.ProgramDataPath).Returns(Path.Combine(_testRoot, "ProgramData"));
        applicationPaths.Setup(a => a.RootFolderPath).Returns(Path.Combine(_testRoot, "Root"));
        applicationPaths.Setup(a => a.InternalMetadataPath).Returns(Path.Combine(_testRoot, "Metadata"));
        applicationPaths.Setup(a => a.DefaultInternalMetadataPath).Returns(Path.Combine(_testRoot, "MetadataDefault"));

        var jellyfinDatabaseProvider = new Mock<IJellyfinDatabaseProvider>();
        jellyfinDatabaseProvider.Setup(p => p.RunScheduledOptimisation(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        jellyfinDatabaseProvider.Setup(p => p.SupportsTransactionalMigrations).Returns(() => _supportsTransactionalMigrations);
        jellyfinDatabaseProvider
            .Setup(p => p.PurgeDatabase(It.IsAny<JellyfinDbContext>(), It.IsAny<IEnumerable<string>>()))
            .Returns((JellyfinDbContext context, IEnumerable<string> tableNames) =>
            {
                _purgeCalls++;
                return DatabaseProvider.PurgeDatabase(context, tableNames);
            });

        jellyfinDatabaseProvider
            .Setup(p => p.BeginExplicitIdentityInsertAsync(It.IsAny<JellyfinDbContext>(), It.IsAny<IEntityType>(), It.IsAny<CancellationToken>()))
            .Returns(async (JellyfinDbContext context, IEntityType entityType, CancellationToken cancellationToken) =>
            {
                _identityScopeEvents.Add($"open {entityType.ClrType.Name}");
                _identityScopeTypes.Add(entityType);
                var scope = await DatabaseProvider.BeginExplicitIdentityInsertAsync(context, entityType, cancellationToken).ConfigureAwait(false);
                return (IAsyncDisposable)new IdentityScope(async () =>
                {
                    _identityScopeEvents.Add($"close {entityType.ClrType.Name}");
                    if (context.ChangeTracker.Entries().Any(e => e.State != EntityState.Unchanged))
                    {
                        _unsavedWhenScopeClosed.Add(entityType.ClrType.Name);
                    }

                    await scope.DisposeAsync().ConfigureAwait(false);
                });
            });
        jellyfinDatabaseProvider
            .Setup(p => p.CompleteDatabaseRestoreAsync(It.IsAny<JellyfinDbContext>(), It.IsAny<CancellationToken>()))
            .Returns((JellyfinDbContext context, CancellationToken cancellationToken) =>
            {
                _identityScopeEvents.Add("complete");
                return _failCompletion
                    ? Task.FromException(new InvalidOperationException("Completion failed"))
                    : DatabaseProvider.CompleteDatabaseRestoreAsync(context, cancellationToken);
            });

        var applicationLifetime = new Mock<IHostApplicationLifetime>();

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.IsScanRunning).Returns(false);

        var routineCatalog = new Mock<IMigrationRoutineCatalog>();
        routineCatalog
            .Setup(c => c.RoutineIds)
            .Returns(new HashSet<string>(StringComparer.Ordinal) { RoutineInArchiveId, RoutineOnlyInTargetId });

        return new BackupService(
            _logger,
            factory.Object,
            applicationHost.Object,
            applicationPaths.Object,
            jellyfinDatabaseProvider.Object,
            applicationLifetime.Object,
            libraryManager.Object,
            routineCatalog.Object);
    }

    private static BaseItemEntity CreateMovieEntity(Guid id, string name)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = "Movie",
            Name = name,
            PresentationUniqueKey = id.ToString("N"),
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        };
    }

    private JellyfinDbContext CreateDbContext(DbContextOptions<JellyfinDbContext> dbOptions)
    {
        return new JellyfinDbContext(
            dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            DatabaseProvider,
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }

    private sealed class IdentityScope(Func<ValueTask> onDispose) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => onDispose();
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }
}
