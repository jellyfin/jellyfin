using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.FullSystemBackup;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.SystemBackupService;
using Microsoft.Data.Sqlite;
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
public sealed class BackupServiceTests : IDisposable
{
    private const string RoutineInArchiveId = "20250101120000_RoutineInArchive";
    private const string RoutineOnlyInTargetId = "20250101130000_RoutineOnlyInTarget";
    private const string ForeignSchemaMigrationId = "20250101140000_SchemaMigrationOfAnotherEngine";
    private const string RemovedRoutineId = "20250101150000_RoutineThisServerNoLongerHas";

    private static readonly Guid MovieId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherMovieId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid RockId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid NulRockId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly string _testRoot;
    private readonly string _backupPath;
    private readonly string _configurationDirectoryPath;
    private readonly RecordingLogger<BackupService> _logger = new();
    private readonly List<string> _identityScopeEvents = [];
    private readonly List<IEntityType> _identityScopeTypes = [];
    private readonly List<string> _unsavedWhenScopeClosed = [];
    private int _purgeCalls;
    private bool _failCompletion;

    public BackupServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection, sqliteOptions => sqliteOptions.MigrationsAssembly(typeof(SqliteDatabaseProvider).Assembly))
            .Options;

        using (var ctx = CreateDbContext())
        {
            ctx.Database.Migrate();
        }

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

    public void Dispose()
    {
        _connection.Dispose();

        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, true);
        }
    }

    [Fact]
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
                $"INSERT INTO KeyframeData (ItemId, TotalDuration, KeyframeTicks) VALUES ({corruptItemId.ToString()}, {5000L}, {"[1,2,3"})",
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

        Assert.IsType<SqliteException>(exception.InnerException);
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

    [Fact]
    public async Task RestoreBackupAsync_WithHistoryFromAnotherSchema_LeavesNoPendingMigrations()
    {
        var (_, pending) = await RestoreWithRewrittenHistoryAsync().ConfigureAwait(true);

        Assert.Empty(pending);
    }

    [Fact]
    public async Task RestoreBackupAsync_KeepsTheSchemaMigrationRowsOfTheTarget()
    {
        var (history, _) = await RestoreWithRewrittenHistoryAsync().ConfigureAwait(true);

        await using var ctx = CreateDbContext();
        var schemaMigrationIds = ctx.GetService<IMigrationsAssembly>().Migrations.Keys;
        Assert.NotEmpty(schemaMigrationIds);
        Assert.All(schemaMigrationIds, id => Assert.Contains(id, history));
    }

    [Fact]
    public async Task RestoreBackupAsync_ReplacesCodeRoutinesWithThoseOfTheArchive()
    {
        var (history, _) = await RestoreWithRewrittenHistoryAsync().ConfigureAwait(true);

        Assert.Contains(RoutineInArchiveId, history);
        Assert.DoesNotContain(RoutineOnlyInTargetId, history);
    }

    [Fact]
    public async Task RestoreBackupAsync_DoesNotTakeOverSchemaMigrationsOfAnotherEngine()
    {
        var (history, _) = await RestoreWithRewrittenHistoryAsync().ConfigureAwait(true);

        Assert.DoesNotContain(ForeignSchemaMigrationId, history);
    }

    [Fact]
    public async Task RestoreBackupAsync_DoesNotTakeOverRoutinesThisServerNoLongerHas()
    {
        var (history, _) = await RestoreWithRewrittenHistoryAsync().ConfigureAwait(true);

        Assert.DoesNotContain(RemovedRoutineId, history);
    }

    [Fact]
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

    [Fact]
    public async Task RestoreBackupAsync_IntoADatabaseThatWasOnlySeeded_CreatesTheSchemaAndRestoresTheRows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken).ConfigureAwait(true);
        var expected = await SnapshotAsync(cancellationToken).ConfigureAwait(true);
        var manifest = await CreateBackupService().CreateBackupAsync(new BackupOptionsDto()).ConfigureAwait(true);

        // What a first start leaves behind: an empty database with the history of the code routines, but no tables.
        using var seededConnection = new SqliteConnection("Data Source=:memory:");
        await seededConnection.OpenAsync(cancellationToken).ConfigureAwait(true);
        var seededOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(seededConnection, sqliteOptions => sqliteOptions.MigrationsAssembly(typeof(SqliteDatabaseProvider).Assembly))
            .Options;
        await using (var seeded = CreateDbContext(seededOptions))
        {
            await seeded.GetService<IHistoryRepository>().CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(true);
            await seeded.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ({RoutineOnlyInTargetId}, {"10.11.0"})",
                cancellationToken).ConfigureAwait(true);
        }

        await CreateBackupService(seededOptions).RestoreBackupAsync(manifest.Path).ConfigureAwait(true);

        await using var restored = CreateDbContext(seededOptions);
        Assert.Empty(await restored.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(true));
        Assert.Equal(
            expected.Where(e => e.StartsWith("User ", StringComparison.Ordinal)),
            (await restored.Users.AsNoTracking().OrderBy(e => e.Username).ToListAsync(cancellationToken).ConfigureAwait(true)).Select(e => $"User {e.Id} {e.Username}"));
        Assert.Equal(2, await restored.BaseItems.CountAsync(e => e.Type == "Movie", cancellationToken).ConfigureAwait(true));
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
        await ctx.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ({migrationId}, {"10.11.0"})",
            cancellationToken).ConfigureAwait(false);
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

    private BackupService CreateBackupService(DbContextOptions<JellyfinDbContext>? dbOptions = null)
    {
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(() => CreateDbContext(dbOptions));
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => CreateDbContext(dbOptions));

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

        var sqliteProvider = new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance);
        var jellyfinDatabaseProvider = new Mock<IJellyfinDatabaseProvider>();
        jellyfinDatabaseProvider.Setup(p => p.RunScheduledOptimisation(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        jellyfinDatabaseProvider
            .Setup(p => p.PurgeDatabase(It.IsAny<JellyfinDbContext>(), It.IsAny<IEnumerable<string>>()))
            .Returns((JellyfinDbContext context, IEnumerable<string> tableNames) =>
            {
                _purgeCalls++;
                return sqliteProvider.PurgeDatabase(context, tableNames);
            });

        jellyfinDatabaseProvider
            .Setup(p => p.BeginExplicitIdentityInsertAsync(It.IsAny<JellyfinDbContext>(), It.IsAny<IEntityType>(), It.IsAny<CancellationToken>()))
            .Returns((JellyfinDbContext context, IEntityType entityType, CancellationToken _) =>
            {
                _identityScopeEvents.Add($"open {entityType.ClrType.Name}");
                _identityScopeTypes.Add(entityType);
                return ValueTask.FromResult<IAsyncDisposable>(new IdentityScope(() =>
                {
                    _identityScopeEvents.Add($"close {entityType.ClrType.Name}");
                    if (context.ChangeTracker.Entries().Any(e => e.State != EntityState.Unchanged))
                    {
                        _unsavedWhenScopeClosed.Add(entityType.ClrType.Name);
                    }
                }));
            });
        jellyfinDatabaseProvider
            .Setup(p => p.CompleteDatabaseRestoreAsync(It.IsAny<JellyfinDbContext>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                _identityScopeEvents.Add("complete");
                return _failCompletion ? Task.FromException(new InvalidOperationException("Completion failed")) : Task.CompletedTask;
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

    private JellyfinDbContext CreateDbContext(DbContextOptions<JellyfinDbContext>? dbOptions = null)
    {
        return new JellyfinDbContext(
            dbOptions ?? _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }

    private sealed class IdentityScope(Action onDispose) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            onDispose();
            return ValueTask.CompletedTask;
        }
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
