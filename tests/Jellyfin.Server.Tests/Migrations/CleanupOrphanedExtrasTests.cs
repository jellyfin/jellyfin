using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// Covers which items the orphaned extra cleanup is allowed to delete. A playlist or a collection is
/// a folder that only exists in the database, so deleting one over a stale OwnerId loses user data
/// that no library scan can rebuild.
/// </summary>
public sealed class CleanupOrphanedExtrasTests : IDisposable
{
    private static readonly Guid _placeholderOwner = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly IApplicationPaths _applicationPaths;

    public CleanupOrphanedExtrasTests()
    {
        _applicationPaths = new Mock<IApplicationPaths>().Object;

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
    public async Task PerformAsync_OrphanedExtra_IsDeleted()
    {
        var extraId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Seed(extraId, typeof(Video).FullName!, isFolder: false);

        var deleted = await PerformAsync();

        Assert.Equal([extraId], deleted);
    }

    [Fact]
    public async Task PerformAsync_PlaylistWithStaleOwner_IsKept()
    {
        var playlistId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Seed(playlistId, "MediaBrowser.Controller.Playlists.Playlist", isFolder: true);

        var deleted = await PerformAsync();

        Assert.Empty(deleted);

        using var context = CreateDbContext();
        Assert.True(context.BaseItems.Any(b => b.Id.Equals(playlistId)));
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private JellyfinDbContext CreateDbContext() => new(
        _dbOptions,
        NullLogger<JellyfinDbContext>.Instance,
        new SqliteDatabaseProvider(_applicationPaths, NullLogger<SqliteDatabaseProvider>.Instance),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    private void Seed(Guid id, string type, bool isFolder)
    {
        using var context = CreateDbContext();
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = type,
            IsFolder = isFolder,
            OwnerId = _placeholderOwner
        });
        context.SaveChanges();
    }

    private async Task<IReadOnlyList<Guid>> PerformAsync()
    {
        var deleted = new List<Guid>();
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(l => l.DeleteItemsUnsafeFast(It.IsAny<IReadOnlyCollection<BaseItem>>(), It.IsAny<bool>()))
            .Callback<IReadOnlyCollection<BaseItem>, bool>((items, _) => deleted.AddRange(items.Select(i => i.Id)));

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateDbContext);

        var migration = new CleanupOrphanedExtras(
            new StartupLogger<CleanupOrphanedExtras>(NullLogger<CleanupOrphanedExtras>.Instance),
            factory.Object,
            libraryManager.Object);

        await migration.PerformAsync(CancellationToken.None);

        return deleted;
    }
}
