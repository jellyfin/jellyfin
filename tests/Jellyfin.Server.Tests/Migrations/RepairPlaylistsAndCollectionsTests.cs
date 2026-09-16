using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Migrations.Routines;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// Covers the repair of playlist and collection rows. Sparing a row from the library-media cleanups
/// is not enough: every item query drops items that carry an OwnerId without an ExtraType, so a
/// playlist left with a stale owner stays invisible even though it is still in the database.
/// </summary>
public sealed class RepairPlaylistsAndCollectionsTests : IDisposable
{
    private const string PlaylistType = "MediaBrowser.Controller.Playlists.Playlist";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly IApplicationPaths _applicationPaths;
    private readonly string _dataPath;

    public RepairPlaylistsAndCollectionsTests()
    {
        _applicationPaths = new Mock<IApplicationPaths>().Object;
        _dataPath = Path.Combine(Path.GetTempPath(), "jf-repair-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_dataPath, "playlists"));

        // The connection owns the in-memory database, so it stays open for the whole test.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task PerformAsync_PlaylistWithStaleOwner_ClearsIt()
    {
        var playlistId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var ownerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        SeedPlaylist(playlistId, "Focus", ownerId: ownerId, isFolder: true);

        await PerformAsync();

        using var context = CreateDbContext();
        var playlist = context.BaseItems.First(b => b.Id.Equals(playlistId));
        Assert.Null(playlist.OwnerId);
        Assert.Null(playlist.ExtraType);
    }

    [Fact]
    public async Task PerformAsync_PlaylistThatIsNotAFolder_BecomesOne()
    {
        var playlistId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        SeedPlaylist(playlistId, "Workout", ownerId: null, isFolder: false);

        await PerformAsync();

        using var context = CreateDbContext();
        Assert.True(context.BaseItems.First(b => b.Id.Equals(playlistId)).IsFolder);
    }

    [Fact]
    public async Task PerformAsync_PlaylistWithDanglingTopParent_IsRehomed()
    {
        var folderId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var playlistId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        SeedContainerFolder(folderId);

        // TopParentId has no foreign key, so a value left over from a deleted row survives and hides
        // the playlist from every query scoped to a top level folder.
        SeedPlaylist(playlistId, "Chillout", ownerId: null, isFolder: true, topParentId: Guid.NewGuid());

        await PerformAsync();

        using var context = CreateDbContext();
        var playlist = context.BaseItems.First(b => b.Id.Equals(playlistId));
        Assert.Equal(folderId, playlist.TopParentId);
        Assert.Equal(folderId, playlist.ParentId);
    }

    [Fact]
    public async Task PerformAsync_HealthyPlaylist_IsLeftAlone()
    {
        var folderId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var playlistId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        SeedContainerFolder(folderId);

        SeedPlaylist(playlistId, "Classical", ownerId: null, isFolder: true, topParentId: folderId, parentId: folderId);

        await PerformAsync();

        using var context = CreateDbContext();
        var playlist = context.BaseItems.First(b => b.Id.Equals(playlistId));
        Assert.Null(playlist.OwnerId);
        Assert.True(playlist.IsFolder);
        Assert.Equal(folderId, playlist.ParentId);
        Assert.Equal(folderId, playlist.TopParentId);
    }

    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_dataPath))
        {
            Directory.Delete(_dataPath, true);
        }
    }

    private JellyfinDbContext CreateDbContext() => new(
        _dbOptions,
        NullLogger<JellyfinDbContext>.Instance,
        new SqliteDatabaseProvider(_applicationPaths, NullLogger<SqliteDatabaseProvider>.Instance),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    private void SeedContainerFolder(Guid id)
    {
        using var context = CreateDbContext();
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = "MediaBrowser.Controller.Entities.Folder",
            IsFolder = true,
            Path = Path.Combine(_dataPath, "playlists")
        });
        context.SaveChanges();
    }

    private void SeedPlaylist(
        Guid id,
        string name,
        Guid? ownerId,
        bool isFolder,
        Guid? topParentId = null,
        Guid? parentId = null)
    {
        var path = Path.Combine(_dataPath, "playlists", name);
        Directory.CreateDirectory(path);

        using var context = CreateDbContext();
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = PlaylistType,
            Name = name,
            Path = path,
            IsFolder = isFolder,
            OwnerId = ownerId,
            ParentId = parentId,
            TopParentId = topParentId
        });
        context.SaveChanges();
    }

    private async Task PerformAsync()
    {
        var appHost = new Mock<IServerApplicationHost>();
        appHost.Setup(h => h.ExpandVirtualPath(It.IsAny<string>())).Returns<string>(p => p);

        var appPaths = new Mock<IServerApplicationPaths>();
        appPaths.SetupGet(p => p.DataPath).Returns(_dataPath);

        var libraryManager = new Mock<ILibraryManager>();

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateDbContext);

        var migration = new RepairPlaylistsAndCollections(
            NullLoggerFactory.Instance,
            factory.Object,
            libraryManager.Object,
            appHost.Object,
            appPaths.Object);

        await migration.PerformAsync(CancellationToken.None);
    }
}
