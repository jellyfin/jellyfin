using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
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
using Microsoft.Extensions.Hosting;
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
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly string _testRoot;
    private readonly string _backupPath;
    private readonly string _configurationDirectoryPath;

    public BackupServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var ctx = CreateDbContext())
        {
            ctx.Database.EnsureCreated();
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

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        var alice = new User("alice", "provider", "reset");
        alice.Permissions.Add(new Permission(PermissionKind.IsAdministrator, true));
        alice.Permissions.Add(new Permission(PermissionKind.EnableAllFolders, false));
        var bob = new User("bob", "provider", "reset");
        bob.Permissions.Add(new Permission(PermissionKind.IsAdministrator, false));

        var movieId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var otherMovieId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var rock = new ItemValue
        {
            ItemValueId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Type = ItemValueType.Genre,
            Value = "Rock",
            CleanValue = "rock"
        };

        await using var ctx = CreateDbContext();
        ctx.Users.AddRange(alice, bob);
        ctx.BaseItems.Add(CreateMovieEntity(movieId, "First Movie"));
        ctx.BaseItems.Add(CreateMovieEntity(otherMovieId, "Second Movie"));
        ctx.ItemValues.Add(rock);
        await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        ctx.AccessSchedules.Add(new AccessSchedule(DynamicDayOfWeek.Weekday, 8, 17, alice.Id));
        ctx.Devices.Add(new Device(alice.Id, "App", "1.0", "Phone", "device-1"));
        ctx.ItemValuesMap.Add(new ItemValueMap { ItemId = movieId, ItemValueId = rock.ItemValueId, Item = null!, ItemValue = null! });
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

    private BackupService CreateBackupService()
    {
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateDbContext);

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
            .Returns((JellyfinDbContext context, IEnumerable<string> tableNames) => sqliteProvider.PurgeDatabase(context, tableNames));

        var applicationLifetime = new Mock<IHostApplicationLifetime>();

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.IsScanRunning).Returns(false);

        return new BackupService(
            NullLogger<BackupService>.Instance,
            factory.Object,
            applicationHost.Object,
            applicationPaths.Object,
            jellyfinDatabaseProvider.Object,
            applicationLifetime.Object,
            libraryManager.Object);
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

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }
}
