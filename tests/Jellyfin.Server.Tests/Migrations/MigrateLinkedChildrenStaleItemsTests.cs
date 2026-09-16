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

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateDbContext();
        context.Database.EnsureCreated();
    }

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

    private string SeedMediaFile(string name)
    {
        var path = Path.Combine(_root, "media", name);
        File.WriteAllBytes(path, [0]);
        return path;
    }

    private void SeedItem(Guid id, string path)
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
    }

    private IReadOnlyList<Guid> Perform(string[] libraryLocations)
    {
        var deleted = new List<Guid>();

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(l => l.GetVirtualFolders())
            .Returns(libraryLocations.Length == 0
                ? []
                : [new VirtualFolderInfo { Locations = libraryLocations }]);
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
