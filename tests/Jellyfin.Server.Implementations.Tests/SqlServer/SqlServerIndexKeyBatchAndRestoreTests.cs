using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.FullSystemBackup;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.SystemBackupService;
using MediaBrowser.Model.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SqlServer;

/// <summary>
/// Holds SQL Server to what an item whose key is over the 1700 bytes of an index costs: a batch saved as a whole saves
/// none of its items, a batch saved skipping failures saves all the others, and a restore that meets such a row rolls back
/// as a whole, leaving the database as it was.
/// </summary>
public sealed class SqlServerIndexKeyBatchAndRestoreTests : DatabaseTestFixture
{
    private const int BatchSize = 10;
    private const int LongOne = 4;

    private readonly string _root = Path.Combine(Path.GetTempPath(), "jellyfin-sqlserver-key-" + Guid.NewGuid().ToString("N"));
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private readonly IRecordingsManager? _previousRecordingsManager;

    public SqlServerIndexKeyBatchAndRestoreTests()
    {
        // BaseItem resolves these through process-wide statics; restored in Dispose.
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;
        _previousRecordingsManager = Video.RecordingsManager;
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>())).Returns([]);
        BaseItem.LibraryManager = libraryManager.Object;
        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configurationManager.Object;
        Video.RecordingsManager = Mock.Of<IRecordingsManager>();
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public void SaveItems_OneItemWithAPathOverTheKeyOfItsIndex_SavesNoneAndThrowsTheRefusal()
    {
        var items = CreateMovies();

        var refusal = Assert.Throws<DbUpdateException>(() => CreateService().SaveItems(items, CancellationToken.None));

        Assert.Contains(1946, Assert.IsType<SqlException>(refusal.InnerException).Errors.Cast<SqlError>().Select(e => e.Number));
        Assert.Empty(StoredIds(items));
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public void SaveItemsSkippingFailures_OneItemWithAPathOverTheKeyOfItsIndex_SavesTheOthersAndLogsIt()
    {
        var items = CreateMovies();
        var logger = new CollectingLogger<ItemPersistenceService>();

        var notSaved = CreateService(logger).SaveItemsSkippingFailures(items, CancellationToken.None);

        Assert.Equal([items[LongOne].Id], notSaved);
        Assert.Equal(items.Where((_, i) => i != LongOne).Select(i => i.Id).Order(), StoredIds(items).Order());
        var error = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Error);
        Assert.Contains(items[LongOne].Path, error.Message, StringComparison.Ordinal);
    }

    [ProviderFact(TestDbProvider.SqlServer)]
    public async Task RestoreBackup_ItemWithAPathOverTheKeyOfItsIndex_RollsBackAsAWhole()
    {
        var kept = Guid.NewGuid();
        AddItem(kept, "/movies/kept.mkv");
        var archive = await CreateBackupService().CreateBackupAsync(new BackupOptionsDto());
        var untouched = Path.Combine(_root, "untouched.zip");
        File.Copy(archive.Path, untouched);
        SetPathInArchive(archive.Path, kept, "/" + new string('p', 850));

        // A row that the restore would remove, so that a restore that went through is told apart from one that did not.
        var added = Guid.NewGuid();
        AddItem(added, "/movies/added.mkv");
        var before = Describe();

        // The restore purges every table before it inserts the rows of the backup, in one transaction, so the row added
        // after the backup was deleted by the time the long path was refused: it is there again only if all of it was
        // rolled back.
        var refusal = await Assert.ThrowsAnyAsync<Exception>(() => CreateBackupService().RestoreBackupAsync(archive.Path));

        Assert.True(Has1946(refusal), refusal.ToString());
        Assert.Equal(before, Describe());
        Assert.Contains(added.ToString(), before, StringComparison.Ordinal);

        // The same restore from the untouched archive goes through, and removes that row, so the state that was compared
        // is one a restore changes.
        await CreateBackupService().RestoreBackupAsync(untouched);
        var after = Describe();
        Assert.NotEqual(before, after);
        Assert.DoesNotContain(added.ToString(), after, StringComparison.Ordinal);
        Assert.Contains(kept.ToString(), after, StringComparison.Ordinal);
    }

    protected override void Dispose(bool disposing)
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        Video.RecordingsManager = _previousRecordingsManager!;
        if (disposing && Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }

        base.Dispose(disposing);
    }

    private static bool Has1946(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException)
            {
                return sqlException.Errors.Cast<SqlError>().Any(error => error.Number == 1946);
            }
        }

        return false;
    }

    private static List<BaseItem> CreateMovies()
    {
        var items = Enumerable.Range(0, BatchSize)
            .Select(i => (BaseItem)new Movie { Id = Guid.NewGuid(), Name = "Movie " + i, Path = "/movies/m" + i + ".mkv" })
            .ToList();
        items[LongOne].Path = "/movies/" + new string('m', 900);
        return items;
    }

    private static void SetPathInArchive(string archive, Guid id, string path)
    {
        using var zip = ZipFile.Open(archive, ZipArchiveMode.Update);
        var entry = zip.GetEntry("Database/BaseItems.json") ?? zip.Entries.Single(e => e.FullName.EndsWith("BaseItems.json", StringComparison.Ordinal) && e.FullName.Contains("Database", StringComparison.Ordinal));
        JsonArray rows;
        using (var stream = entry.Open())
        {
            rows = JsonNode.Parse(stream)!.AsArray();
        }

        var row = rows.Single(r => string.Equals(r!["Id"]!.GetValue<string>(), id.ToString(), StringComparison.OrdinalIgnoreCase))!;
        row["Path"] = path;
        var name = entry.FullName;
        entry.Delete();
        using var writer = new StreamWriter(zip.CreateEntry(name).Open());
        writer.Write(rows.ToJsonString());
    }

    private void AddItem(Guid id, string path)
    {
        using var context = CreateDbContext();
        context.BaseItems.Add(new BaseItemEntity { Id = id, Type = "Test", Path = path });
        context.SaveChanges();
    }

    private string Describe()
    {
        using var context = CreateDbContext();
        // Formatted after reading: an id in the SQL is 16 bytes, not text.
        var items = context.BaseItems.OrderBy(e => e.Id).Select(e => new { e.Id, e.Path }).AsEnumerable().Select(e => e.Id + "=" + e.Path).ToList();
        return string.Join(" | ", items) + " | logs " + context.ActivityLogs.Count() + " | users " + context.Users.Count();
    }

    private List<Guid> StoredIds(IEnumerable<BaseItem> items)
    {
        var ids = items.Select(i => i.Id).ToList();
        using var context = CreateDbContext();
        return context.BaseItems.Where(e => ids.Contains(e.Id)).Select(e => e.Id).ToList();
    }

    private ItemPersistenceService CreateService(ILogger<ItemPersistenceService>? logger = null)
    {
        // Paths round-trip through the host's virtual path mapping on the way in and out.
        var appHost = new Mock<IServerApplicationHost>();
        appHost.Setup(h => h.ReverseVirtualPath(It.IsAny<string>())).Returns((string p) => p);
        appHost.Setup(h => h.ExpandVirtualPath(It.IsAny<string>())).Returns((string p) => p);
        return new(CreateDbContextFactory(), DatabaseProvider, appHost.Object, logger ?? NullLogger<ItemPersistenceService>.Instance);
    }

    private BackupService CreateBackupService()
    {
        var applicationHost = new Mock<IServerApplicationHost>();
        applicationHost.Setup(a => a.ApplicationVersion).Returns(new Version(10, 11, 0));

        var applicationPaths = new Mock<IServerApplicationPaths>();
        string Directory(string name)
        {
            var path = Path.Combine(_root, name);
            System.IO.Directory.CreateDirectory(path);
            return path;
        }

        applicationPaths.Setup(a => a.BackupPath).Returns(Directory("Backup"));
        applicationPaths.Setup(a => a.ConfigurationDirectoryPath).Returns(Directory("Config"));
        applicationPaths.Setup(a => a.DataPath).Returns(Directory("Data"));
        applicationPaths.Setup(a => a.CachePath).Returns(Directory("Cache"));
        applicationPaths.Setup(a => a.ProgramDataPath).Returns(Directory("ProgramData"));
        applicationPaths.Setup(a => a.RootFolderPath).Returns(Directory("Root"));
        applicationPaths.Setup(a => a.InternalMetadataPath).Returns(Directory("Metadata"));
        applicationPaths.Setup(a => a.DefaultInternalMetadataPath).Returns(Directory("MetadataDefault"));

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.IsScanRunning).Returns(false);

        var routineCatalog = new Mock<IMigrationRoutineCatalog>();
        routineCatalog.Setup(c => c.RoutineIds).Returns(new HashSet<string>(StringComparer.Ordinal));

        return new BackupService(
            NullLogger<BackupService>.Instance,
            CreateDbContextFactory(),
            applicationHost.Object,
            applicationPaths.Object,
            DatabaseProvider,
            new Mock<IHostApplicationLifetime>().Object,
            libraryManager.Object,
            routineCatalog.Object);
    }
}
