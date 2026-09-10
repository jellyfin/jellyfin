using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.FullSystemBackup;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.SystemBackupService;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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
        _connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
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

        await using var archive = await ZipFile.OpenReadAsync(manifest.Path, cancellationToken).ConfigureAwait(true);
        await using (var manifestStream = await archive.GetEntry("manifest.json")!.OpenAsync(cancellationToken))
        {
            using var manifestDocument = await JsonDocument.ParseAsync(manifestStream, cancellationToken: cancellationToken);
            var declaredTables = manifestDocument.RootElement.GetProperty("DatabaseTables").EnumerateArray().Select(e => e.GetString()).Order().ToArray();
            var archivedTables = archive.Entries.Where(e => e.FullName.StartsWith("Database/", StringComparison.Ordinal)).Select(e => Path.GetFileNameWithoutExtension(e.Name)).Order().ToArray();
            Assert.Equal(archivedTables, declaredTables);
        }

        var keyframeEntry = archive.GetEntry("Database/KeyframeData.json");
        Assert.NotNull(keyframeEntry);

        await using var entryStream = await keyframeEntry!.OpenAsync(cancellationToken).ConfigureAwait(true);
        using var document = await JsonDocument.ParseAsync(entryStream, cancellationToken: cancellationToken).ConfigureAwait(true);

        var rows = document.RootElement.EnumerateArray().ToList();

        // The corrupt row must be skipped, but the valid row must still make it into the backup.
        var singleRow = Assert.Single(rows);
        Assert.Equal(validItemId, singleRow.GetProperty("ItemId").GetGuid());
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("malformed")]
    [InlineData("duplicate")]
    [InlineData("constraint")]
    public async Task RestoreBackupAsync_InvalidDatabase_PreservesExistingDataAndHistory(string failure)
    {
        var archivePath = await CreateRestoreArchiveAsync();
        await using (var archive = await ZipFile.OpenAsync(archivePath, ZipArchiveMode.Update, TestContext.Current.CancellationToken))
        {
            var entry = archive.GetEntry("Database/BaseItems.json")!;
            JsonArray? items = null;
            if (failure is "duplicate" or "constraint")
            {
                await using var stream = await entry.OpenAsync(TestContext.Current.CancellationToken);
                items = (await JsonNode.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken))!.AsArray();
            }

            entry.Delete();
            if (failure != "missing")
            {
                await using var writer = new StreamWriter(await archive.CreateEntry("Database/BaseItems.json").OpenAsync(TestContext.Current.CancellationToken));
                if (failure == "malformed")
                {
                    await writer.WriteAsync("[{".AsMemory(), TestContext.Current.CancellationToken);
                }
                else
                {
                    if (failure == "duplicate")
                    {
                        items!.Add(items[0]!.DeepClone());
                    }
                    else
                    {
                        items![0]!["ParentId"] = Guid.NewGuid();
                    }

                    await writer.WriteAsync(items.ToJsonString().AsMemory(), TestContext.Current.CancellationToken);
                }
            }
        }

        var exception = await Record.ExceptionAsync(() => CreateBackupService().RestoreBackupAsync(archivePath));

        if (failure == "constraint")
        {
            Assert.True(exception is DbUpdateException or SqliteException, exception?.ToString());
        }
        else
        {
            Assert.IsType(failure == "malformed" ? typeof(JsonException) : typeof(InvalidOperationException), exception);
        }

        await AssertExistingDatabaseAsync();
        if (failure != "constraint")
        {
            Assert.Equal("existing config", await File.ReadAllTextAsync(Path.Combine(_configurationDirectoryPath, "system.xml"), TestContext.Current.CancellationToken));
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RestoreBackupAsync_ValidDatabase_ReplacesRowsAndHistoryAndKeepsForeignKeysEnabled(bool legacyManifest, bool olderTables)
    {
        var archivePath = await CreateRestoreArchiveAsync();

        if (legacyManifest)
        {
            await UseLegacyManifestAsync(archivePath, olderTables);
        }

        await CreateBackupService().RestoreBackupAsync(archivePath);

        using var context = CreateDbContext();
        Assert.Equal(new[] { "Archived Child", "Archived Movie" }, context.BaseItems.Where(e => e.Type != "PLACEHOLDER").OrderBy(e => e.Name).Select(e => e.Name).ToArray());
        var link = Assert.Single(context.LinkedChildren);
        Assert.Equal("Archived Movie", context.BaseItems.Single(e => e.Id.Equals(link.ParentId)).Name);
        Assert.Equal("Archived Child", context.BaseItems.Single(e => e.Id.Equals(link.ChildId)).Name);
        Assert.Equal("backup", Assert.Single(await context.GetService<IHistoryRepository>().GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).MigrationId);
        Assert.Equal("archived config", await File.ReadAllTextAsync(Path.Combine(_configurationDirectoryPath, "system.xml"), TestContext.Current.CancellationToken));
        await AssertForeignKeysEnabledAsync(context);
    }

    [Fact]
    public async Task RestoreBackupAsync_LegacyManifestMissingTable_RejectsBeforeReplacingData()
    {
        var archivePath = await CreateRestoreArchiveAsync();
        await UseLegacyManifestAsync(archivePath, false);
        await using (var archive = await ZipFile.OpenAsync(archivePath, ZipArchiveMode.Update, TestContext.Current.CancellationToken))
        {
            archive.GetEntry("Database/BaseItems.json")!.Delete();
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateBackupService().RestoreBackupAsync(archivePath));

        await AssertExistingDatabaseAsync();
        Assert.Equal("existing config", await File.ReadAllTextAsync(Path.Combine(_configurationDirectoryPath, "system.xml"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PurgeDatabase_QuotedTableName_RemovesRows()
    {
        await using var context = CreateDbContext();
        await context.Database.ExecuteSqlRawAsync(
            """"
            CREATE TABLE "Restore ""items""" (Id INTEGER);
            INSERT INTO "Restore ""items""" VALUES (1);
            """",
            TestContext.Current.CancellationToken);

        var provider = new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance);
        await provider.PurgeDatabase(context, ["Restore \"items\""]);

        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"Restore \"\"items\"\"\";";
        Assert.Equal(0L, await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    private static async Task UseLegacyManifestAsync(string archivePath, bool olderTables)
    {
        await using var archive = await ZipFile.OpenAsync(archivePath, ZipArchiveMode.Update, TestContext.Current.CancellationToken);
        var entry = archive.GetEntry("manifest.json")!;
        JsonObject manifest;
        await using (var stream = await entry.OpenAsync(TestContext.Current.CancellationToken))
        {
            manifest = (await JsonNode.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken))!.AsObject();
        }

        if (olderTables)
        {
            archive.GetEntry("Database/MediaSegments.json")!.Delete();
        }

        manifest["DatabaseTables"] = new JsonArray(archive.Entries
            .Where(e => e.FullName.StartsWith("Database/", StringComparison.Ordinal))
            .Select(e => (JsonNode)JsonValue.Create(e.Name == "HistoryRow.json" ? "HistoryRow" : "DbSet`1")!)
            .ToArray());
        entry.Delete();
        await using var output = await archive.CreateEntry("manifest.json").OpenAsync(TestContext.Current.CancellationToken);
        await JsonSerializer.SerializeAsync(output, manifest, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RestoreBackupAsync_PreservesGeneratedIdsAndPrivateForeignKeys()
    {
        var token = TestContext.Current.CancellationToken;
        var user = new User("restore-user", "test", "test");
        await using (var context = CreateDbContext())
        {
            var activity = new ActivityLog("archived", "restore-test", user.Id);
            var image = new ImageInfo("archived-image");
            context.AddRange(user, activity, image);
            context.Entry(activity).Property(row => row.Id).CurrentValue = 91;
            context.Entry(image).Property(row => row.Id).CurrentValue = 92;
            context.Entry(image).Property(row => row.UserId).CurrentValue = user.Id;
            await context.SaveChangesAsync(token);
            await context.GetService<IHistoryRepository>().CreateIfNotExistsAsync(token);
        }

        var service = CreateBackupService();
        var archive = await service.CreateBackupAsync(new BackupOptionsDto());
        await service.RestoreBackupAsync(archive.Path);

        await using var restored = CreateDbContext();
        Assert.Equal(91, (await restored.ActivityLogs.SingleAsync(token)).Id);
        var restoredImage = await restored.ImageInfos.SingleAsync(token);
        Assert.Equal(92, restoredImage.Id);
        Assert.Equal(user.Id, restoredImage.UserId);
        var next = new ActivityLog("generated", "restore-test", user.Id);
        restored.ActivityLogs.Add(next);
        await restored.SaveChangesAsync(token);
        Assert.True(next.Id > 91);
    }

    [Fact]
    public async Task RestoreBackupAsync_CompletionFails_RollsBackSavedRowsAndHistory()
    {
        var archivePath = await CreateRestoreArchiveAsync();
        var failure = new InvalidOperationException("completion failed");
        var sqlite = new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance);
        var provider = new Mock<IJellyfinDatabaseProvider>();
        provider.Setup(value => value.PurgeDatabase(It.IsAny<JellyfinDbContext>(), It.IsAny<System.Collections.Generic.IEnumerable<string>>()))
            .Returns<JellyfinDbContext, System.Collections.Generic.IEnumerable<string>>(sqlite.PurgeDatabase);
        provider.Setup(value => value.CompleteDatabaseRestoreAsync(It.IsAny<JellyfinDbContext>(), It.IsAny<CancellationToken>()))
            .Returns<JellyfinDbContext, CancellationToken>(async (context, token) =>
            {
                Assert.NotNull(context.Database.CurrentTransaction);
                Assert.Equal(new[] { "Archived Child", "Archived Movie" }, await context.BaseItems.Where(row => row.Type != "PLACEHOLDER").OrderBy(row => row.Name).Select(row => row.Name).ToArrayAsync(token));
                Assert.Equal("backup", Assert.Single(await context.GetService<IHistoryRepository>().GetAppliedMigrationsAsync(token)).MigrationId);
                throw failure;
            });

        var error = await Record.ExceptionAsync(() => CreateBackupService(provider.Object).RestoreBackupAsync(archivePath));
        Assert.Same(failure, error);
        await AssertExistingDatabaseAsync();
    }

    private async Task<string> CreateRestoreArchiveAsync()
    {
        using var context = CreateDbContext();
        var archived = CreateMovieEntity(Guid.NewGuid(), "Archived Movie");
        var archivedChild = CreateMovieEntity(Guid.NewGuid(), "Archived Child");
        context.BaseItems.AddRange(archived, archivedChild);
        context.LinkedChildren.Add(new LinkedChildEntity { ParentId = archived.Id, ChildId = archivedChild.Id, ChildType = LinkedChildType.Manual });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var history = context.GetService<IHistoryRepository>();
        await history.CreateIfNotExistsAsync(TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow("backup", "10.0.0")), TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_configurationDirectoryPath, "system.xml"), "archived config", TestContext.Current.CancellationToken);
        var archive = await CreateBackupService().CreateBackupAsync(new BackupOptionsDto());
        context.ChangeTracker.Clear();
        await context.LinkedChildren.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.BaseItems.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        var existing = CreateMovieEntity(Guid.NewGuid(), "Existing Movie");
        var existingChild = CreateMovieEntity(Guid.NewGuid(), "Existing Child");
        context.BaseItems.AddRange(existing, existingChild);
        context.LinkedChildren.Add(new LinkedChildEntity { ParentId = existing.Id, ChildId = existingChild.Id, ChildType = LinkedChildType.Manual });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlRawAsync(history.GetDeleteScript("backup"), TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow("existing", "10.0.0")), TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(_configurationDirectoryPath, "system.xml"), "existing config", TestContext.Current.CancellationToken);
        return archive.Path;
    }

    private async Task AssertExistingDatabaseAsync()
    {
        using var context = CreateDbContext();
        Assert.Equal(new[] { "Existing Child", "Existing Movie" }, context.BaseItems.OrderBy(e => e.Name).Select(e => e.Name).ToArray());
        Assert.Single(context.LinkedChildren);
        Assert.Equal("existing", Assert.Single(await context.GetService<IHistoryRepository>().GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).MigrationId);
        await AssertForeignKeysEnabledAsync(context);
    }

    private static async Task AssertForeignKeysEnabledAsync(JellyfinDbContext context)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "PRAGMA foreign_keys;";
        Assert.Equal(1L, await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
        command.CommandText = "PRAGMA defer_foreign_keys;";
        Assert.Equal(0L, await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
        context.BaseItems.Add(new BaseItemEntity { Id = Guid.NewGuid(), Type = "Movie", ParentId = Guid.NewGuid() });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    private BackupService CreateBackupService(IJellyfinDatabaseProvider? databaseProvider = null)
    {
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateDbContext);

        var applicationHost = new Mock<IServerApplicationHost>();
        applicationHost.Setup(a => a.ApplicationVersion).Returns(new Version(10, 11, 0));

        var applicationPaths = new Mock<IServerApplicationPaths>();
        applicationPaths.Setup(a => a.BackupPath).Returns(_backupPath);
        applicationPaths.Setup(a => a.CachePath).Returns(Path.Combine(_testRoot, "Cache"));
        applicationPaths.Setup(a => a.ProgramDataPath).Returns(_testRoot);
        applicationPaths.Setup(a => a.ConfigurationDirectoryPath).Returns(_configurationDirectoryPath);
        applicationPaths.Setup(a => a.DataPath).Returns(Path.Combine(_testRoot, "Data"));
        applicationPaths.Setup(a => a.RootFolderPath).Returns(Path.Combine(_testRoot, "Root"));
        applicationPaths.Setup(a => a.InternalMetadataPath).Returns(Path.Combine(_testRoot, "Metadata"));
        applicationPaths.Setup(a => a.DefaultInternalMetadataPath).Returns(Path.Combine(_testRoot, "MetadataDefault"));

        var applicationLifetime = new Mock<IHostApplicationLifetime>();

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.IsScanRunning).Returns(false);

        return new BackupService(
            NullLogger<BackupService>.Instance,
            factory.Object,
            applicationHost.Object,
            applicationPaths.Object,
            databaseProvider ?? new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
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
