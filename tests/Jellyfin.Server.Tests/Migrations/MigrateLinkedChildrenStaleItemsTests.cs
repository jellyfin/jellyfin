using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Migrations.Routines;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// Covers which items the stale file cleanup is allowed to delete. Resolving no library locations at
/// all puts every file-backed item outside every location, so acting on that emptied whole libraries.
/// </summary>
public sealed class MigrateLinkedChildrenStaleItemsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly IApplicationPaths _applicationPaths;
    private readonly string _root;

    public MigrateLinkedChildrenStaleItemsTests()
    {
        _applicationPaths = new Mock<IApplicationPaths>().Object;
        _root = Path.Combine(Path.GetTempPath(), "jf-stale-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "media"));
        Directory.CreateDirectory(Path.Combine(_root, "root", "default"));

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    private string LibraryDefinitionRoot => Path.Combine(_root, "root", "default");

    [Fact]
    public void Perform_NoLibraryLocationsResolved_KeepsEverything()
    {
        var itemId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        SeedItem(itemId, SeedMediaFile("kept.flac"));

        var deleted = Perform(libraryLocations: []);

        Assert.Empty(deleted);
    }

    [Fact]
    public void Perform_ItemOutsideEveryLocation_IsRemoved()
    {
        var itemId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        // An empty location counts as inaccessible, which disables the removal on purpose, so the
        // library has to hold something for this case to be reachable at all.
        SeedMediaFile("anchor.flac");
        SeedItem(itemId, Path.Combine(_root, "removed-media", "gone.flac"));

        var deleted = Perform(libraryLocations: [Path.Combine(_root, "media")]);

        Assert.Equal([itemId], deleted);
    }

    [Fact]
    public void Perform_ItemInsideLocationWithMissingFile_IsRemoved()
    {
        var itemId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        SeedMediaFile("anchor.flac");
        SeedItem(itemId, Path.Combine(_root, "media", "deleted.flac"));

        var deleted = Perform(libraryLocations: [Path.Combine(_root, "media")]);

        Assert.Equal([itemId], deleted);
    }

    [Fact]
    public void Perform_ItemInsideLocationWithFilePresent_IsKept()
    {
        var itemId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        SeedItem(itemId, SeedMediaFile("present.flac"));

        var deleted = Perform(libraryLocations: [Path.Combine(_root, "media")]);

        Assert.Empty(deleted);
    }

    [Fact]
    public void Perform_LibraryDefinitionMissing_KeepsItsItems()
    {
        var libraryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var itemId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // The definition folder of "Movies" is gone, so it contributes no locations and everything it
        // held reads as unrooted, while "Media" still resolves and makes the run look actionable.
        SeedLibrary(libraryId, "Movies", createDefinitionFolder: false);
        SeedMediaFile("anchor.flac");
        SeedItem(itemId, Path.Combine(_root, "movies", "kept.mkv"), libraryId);

        var deleted = Perform(libraryLocations: [Path.Combine(_root, "media")]);

        Assert.Empty(deleted);
    }

    [Fact]
    public void Perform_PathRemovedFromSurvivingLibrary_IsRemoved()
    {
        var libraryId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var itemId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        // The library is still there; only one of its media paths went away, which is what this
        // branch exists to clean up.
        SeedLibrary(libraryId, "Media", createDefinitionFolder: true);
        SeedMediaFile("anchor.flac");
        SeedItem(itemId, Path.Combine(_root, "removed-media", "gone.flac"), libraryId);

        var deleted = Perform(libraryLocations: [Path.Combine(_root, "media")]);

        Assert.Equal([itemId], deleted);
    }

    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    private JellyfinDbContext CreateDbContext() => new(
        _dbOptions,
        NullLogger<JellyfinDbContext>.Instance,
        new SqliteDatabaseProvider(_applicationPaths, NullLogger<SqliteDatabaseProvider>.Instance),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    private void SeedLibrary(Guid id, string name, bool createDefinitionFolder)
    {
        var path = Path.Combine(LibraryDefinitionRoot, name);
        if (createDefinitionFolder)
        {
            Directory.CreateDirectory(path);
        }

        using var context = CreateDbContext();
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = "MediaBrowser.Controller.Entities.CollectionFolder",
            Name = name,
            Path = path,
            IsFolder = true
        });
        context.SaveChanges();
    }

    private string SeedMediaFile(string name)
    {
        var path = Path.Combine(_root, "media", name);
        File.WriteAllBytes(path, [0]);
        return path;
    }

    private void SeedItem(Guid id, string path, Guid? libraryId = null)
    {
        using var context = CreateDbContext();
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = typeof(Audio).FullName!,
            Name = Path.GetFileNameWithoutExtension(path),
            Path = path,
            IsFolder = false,
            IsVirtualItem = false
        });

        context.SaveChanges();

        if (libraryId.HasValue)
        {
            context.Database.ExecuteSql(
                $"INSERT INTO AncestorIds (ItemId, ParentItemId) VALUES ({id}, {libraryId.Value})");
        }
    }

    private IReadOnlyList<Guid> Perform(string[] libraryLocations, string libraryName = "Media")
    {
        var deleted = new List<Guid>();

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(l => l.GetVirtualFolders())
            .Returns(libraryLocations.Length == 0
                ? []
                : [new VirtualFolderInfo { Name = libraryName, Locations = libraryLocations }]);
        libraryManager
            .Setup(l => l.GetItemById(It.IsAny<Guid>()))
            .Returns<Guid>(id => new Audio { Id = id });
        libraryManager
            .Setup(l => l.DeleteItem(It.IsAny<BaseItem>(), It.IsAny<DeleteOptions>()))
            .Callback<BaseItem, DeleteOptions>((item, _) => deleted.Add(item.Id));

        var appHost = new Mock<IServerApplicationHost>();
        appHost.Setup(h => h.ExpandVirtualPath(It.IsAny<string>())).Returns<string>(p => p);

        var appPaths = new Mock<IServerApplicationPaths>();
        appPaths.SetupGet(p => p.DataPath).Returns(Path.Combine(_root, "data"));
        appPaths.SetupGet(p => p.InternalMetadataPath).Returns(Path.Combine(_root, "metadata"));
        appPaths.SetupGet(p => p.DefaultUserViewsPath).Returns(LibraryDefinitionRoot);

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateDbContext);

        new MigrateLinkedChildren(
            NullLoggerFactory.Instance,
            factory.Object,
            libraryManager.Object,
            appHost.Object,
            appPaths.Object)
            .Perform();

        return deleted;
    }
}
