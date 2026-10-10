using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Playlists;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Extensions;
using Jellyfin.Server.Migrations.Routines;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
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

    [Fact]
    public async Task PerformAsync_MissingPlaylist_RestoresOwnerAndShares()
    {
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var sharedId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var dir = Path.Combine(_dataPath, "playlists", "Private");
        Directory.CreateDirectory(dir);

        var trackId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var trackPath = Path.Combine(_dataPath, "track.mkv");
        using var context = CreateDbContext();
        context.BaseItems.Add(new BaseItemEntity { Id = trackId, Type = "MediaBrowser.Controller.Entities.Video", Path = trackPath });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(dir, "playlist.xml"),
            $"""
            <?xml version="1.0" encoding="utf-8" standalone="yes"?>
            <Item>
              <OwnerUserId>{ownerId:N}</OwnerUserId>
              <PlaylistItems>
                <PlaylistItem>
                  <Path>{trackPath}</Path>
                </PlaylistItem>
              </PlaylistItems>
              <Shares>
                <Share>
                  <UserId>{sharedId}</UserId>
                  <CanEdit>true</CanEdit>
                </Share>
              </Shares>
              <PlaylistMediaType>Video</PlaylistMediaType>
            </Item>
            """,
            TestContext.Current.CancellationToken);

        var restored = await RestoreAsync();

        var playlist = Assert.Single(restored);
        Assert.Equal(ownerId, playlist.OwnerUserId);
        Assert.False(playlist.OpenAccess);
        var share = Assert.Single(playlist.Shares);
        Assert.Equal(sharedId, share.UserId);
        Assert.True(share.CanEdit);
        Assert.Equal(MediaType.Video, playlist.MediaType);

        Assert.Equal(trackId, Assert.Single(playlist.LinkedChildren).ItemId);
    }

    [Fact]
    public async Task PerformAsync_MissingPlaylist_EntriesAreOnTheCreatedItem()
    {
        var dir = Path.Combine(_dataPath, "playlists", "Mix");
        Directory.CreateDirectory(dir);

        var trackId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var trackPath = Path.Combine(_dataPath, "song.flac");
        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(new BaseItemEntity { Id = trackId, Type = "MediaBrowser.Controller.Entities.Audio.Audio", Path = trackPath });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await File.WriteAllTextAsync(
            Path.Combine(dir, "playlist.xml"),
            $"""
            <?xml version="1.0" encoding="utf-8" standalone="yes"?>
            <Item>
              <PlaylistItems>
                <PlaylistItem>
                  <Path>{trackPath}</Path>
                </PlaylistItem>
                <PlaylistItem>
                  <Path>{trackPath}</Path>
                </PlaylistItem>
              </PlaylistItems>
            </Item>
            """,
            TestContext.Current.CancellationToken);

        LinkedChild[]? entriesAtCreate = null;
        bool? loadedAtCreate = null;
        var restored = await RestoreAsync(item =>
        {
            entriesAtCreate = ((Playlist)item).LinkedChildren;
            loadedAtCreate = ((Playlist)item).LinkedChildrenLoaded;
        });

        Assert.Single(restored);
        Assert.True(loadedAtCreate);
        Assert.NotNull(entriesAtCreate);
        Assert.Equal(new Guid?[] { trackId, trackId }, entriesAtCreate.Select(e => e.ItemId));
        Assert.All(entriesAtCreate, e => Assert.Equal(MediaBrowser.Controller.Entities.LinkedChildType.Manual, e.Type));

        // Persisting the entries is left to CreateItem, the migration writes no rows of its own.
        using var verify = CreateDbContext();
        Assert.Empty(verify.LinkedChildren);
    }

    [Fact]
    public async Task PerformAsync_MissingPlaylist_TakesNameFromLocalTitle()
    {
        var dir = Path.Combine(_dataPath, "playlists", "Rock_Metal_ 90s1");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(
            Path.Combine(dir, "playlist.xml"),
            """
            <?xml version="1.0" encoding="utf-8" standalone="yes"?>
            <Item>
              <LocalTitle>Rock/Metal: 90s</LocalTitle>
            </Item>
            """,
            TestContext.Current.CancellationToken);

        var restored = await RestoreAsync();

        Assert.Equal("Rock/Metal: 90s", Assert.Single(restored).Name);
    }

    [Fact]
    public async Task PerformAsync_MissingPlaylistWithoutLocalTitle_TakesFolderName()
    {
        Directory.CreateDirectory(Path.Combine(_dataPath, "playlists", "Untitled"));

        var restored = await RestoreAsync();

        Assert.Equal("Untitled", Assert.Single(restored).Name);
    }

    [Fact]
    public async Task PerformAsync_MissingPlaylistWithoutMetadata_IsOpenAccess()
    {
        Directory.CreateDirectory(Path.Combine(_dataPath, "playlists", "Orphan"));

        var restored = await RestoreAsync();

        var playlist = Assert.Single(restored);
        Assert.True(playlist.OwnerUserId.IsEmpty());
        Assert.True(playlist.OpenAccess);
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

    private async Task<List<Playlist>> RestoreAsync(Action<BaseItem>? onCreate = null)
    {
        var playlistsFolder = new PlaylistsFolder { Id = Guid.NewGuid() };
        var restored = new List<Playlist>();

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(m => m.GetNewItemId(It.IsAny<string>(), It.IsAny<Type>())).Returns(Guid.NewGuid);
        libraryManager.Setup(m => m.GetItemById(It.IsAny<Guid>())).Returns(playlistsFolder);
        libraryManager.Setup(m => m.CreateItem(It.IsAny<BaseItem>(), It.IsAny<BaseItem>()))
            .Callback<BaseItem, BaseItem>((item, _) =>
            {
                restored.Add((Playlist)item);
                onCreate?.Invoke(item);
                using var context = CreateDbContext();
                context.BaseItems.Add(new BaseItemEntity { Id = item.Id, Type = PlaylistType, Path = item.Path, IsFolder = true });
                context.SaveChanges();
            });

        await PerformAsync(libraryManager);
        return restored;
    }

    private async Task PerformAsync(Mock<ILibraryManager>? libraryManager = null)
    {
        var appHost = new Mock<IServerApplicationHost>();
        appHost.Setup(h => h.ExpandVirtualPath(It.IsAny<string>())).Returns<string>(p => p);

        var appPaths = new Mock<IServerApplicationPaths>();
        appPaths.SetupGet(p => p.DataPath).Returns(_dataPath);

        libraryManager ??= new Mock<ILibraryManager>();

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
