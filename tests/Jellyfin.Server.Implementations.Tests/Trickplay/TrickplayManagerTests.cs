using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Tests.Item;
using Jellyfin.Server.Implementations.Trickplay;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Drawing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Trickplay;

public sealed class TrickplayManagerTests : SqliteDbTestFixture
{
    private readonly string _trickplayDirectory = Path.Combine(Path.GetTempPath(), "jellyfin-trickplay-test-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task DeleteTrickplayDataAsync_DisposesDbContext()
    {
        var contexts = new List<JellyfinDbContext>();
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var context = CreateDbContext();
                contexts.Add(context);
                return context;
            });

        // Only the database context factory is used when deleting trickplay data.
        var trickplayManager = new TrickplayManager(
            NullLogger<TrickplayManager>.Instance,
            null!,
            null!,
            null!,
            null!,
            null!,
            factory.Object,
            null!,
            null!);

        await trickplayManager.DeleteTrickplayDataAsync(Guid.NewGuid(), CancellationToken.None);

        var context = Assert.Single(contexts);
        Assert.Throws<ObjectDisposedException>(() => context.Model);
    }

    [Theory]
    [InlineData(6875, 35, 3438)]
    [InlineData(9948, 50, 4974)]
    [InlineData(5883, 30, 2942)]
    public async Task DiscoverExistingTrickplayAsync_RowWithTileFileCount_RebuildsThumbnailCount(int runtimeSeconds, int tileCount, int expectedThumbnailCount)
    {
        var video = new Video { Id = Guid.NewGuid(), RunTimeTicks = TimeSpan.FromSeconds(runtimeSeconds).Ticks };
        CreateTiles(320, 10, 10, tileCount);

        // The tile import in 10.10 through 11.x stored the number of tile files as the thumbnail count.
        await SaveTrickplayInfoAsync(CreateTrickplayInfo(video.Id, tileCount));

        var trickplayManager = CreateTrickplayManager();
        await trickplayManager.DiscoverExistingTrickplayAsync(video, true, CancellationToken.None);

        var info = Assert.Single((await trickplayManager.GetTrickplayResolutions(video.Id)).Values);
        Assert.Equal(expectedThumbnailCount, info.ThumbnailCount);
        Assert.Equal(2000, info.Interval);
        Assert.Equal(180, info.Height);
    }

    [Fact]
    public async Task DiscoverExistingTrickplayAsync_RowThatFitsTiles_IsKept()
    {
        var video = new Video { Id = Guid.NewGuid(), RunTimeTicks = TimeSpan.FromSeconds(6875).Ticks };
        CreateTiles(320, 10, 10, 35);

        // 3450 fits 35 tiles of 10x10 but differs from what discovery would calculate for the runtime.
        var existing = CreateTrickplayInfo(video.Id, 3450);
        existing.Bandwidth = 1234;
        await SaveTrickplayInfoAsync(existing);

        var trickplayManager = CreateTrickplayManager();
        await trickplayManager.DiscoverExistingTrickplayAsync(video, true, CancellationToken.None);

        var info = Assert.Single((await trickplayManager.GetTrickplayResolutions(video.Id)).Values);
        Assert.Equal(3450, info.ThumbnailCount);
        Assert.Equal(1234, info.Bandwidth);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_trickplayDirectory))
        {
            Directory.Delete(_trickplayDirectory, true);
        }

        base.Dispose(disposing);
    }

    private static TrickplayInfo CreateTrickplayInfo(Guid itemId, int thumbnailCount) => new()
    {
        ItemId = itemId,
        Width = 320,
        Height = 180,
        TileWidth = 10,
        TileHeight = 10,
        ThumbnailCount = thumbnailCount,
        Interval = 2000,
        Bandwidth = 0
    };

    private void CreateTiles(int width, int tileWidth, int tileHeight, int tileCount)
    {
        var directory = Directory.CreateDirectory(Path.Combine(_trickplayDirectory, $"{width} - {tileWidth}x{tileHeight}"));
        for (var i = 0; i < tileCount; i++)
        {
            File.WriteAllBytes(Path.Combine(directory.FullName, $"{i}.jpg"), []);
        }
    }

    private async Task SaveTrickplayInfoAsync(TrickplayInfo info)
    {
        await using var context = CreateDbContext();
        context.TrickplayInfos.Add(info);
        await context.SaveChangesAsync();
    }

    private TrickplayManager CreateTrickplayManager()
    {
        var config = new Mock<IServerConfigurationManager>();
        config.Setup(c => c.Configuration).Returns(new ServerConfiguration());

        // Every tile is a full 10x10 grid of 320x180 thumbnails.
        var imageEncoder = new Mock<IImageEncoder>();
        imageEncoder.Setup(e => e.GetImageSize(It.IsAny<string>())).Returns(new ImageDimensions(3200, 1800));

        var pathManager = new Mock<IPathManager>();
        pathManager.Setup(p => p.GetTrickplayDirectory(It.IsAny<BaseItem>(), It.IsAny<bool>())).Returns(_trickplayDirectory);

        return new TrickplayManager(
            NullLogger<TrickplayManager>.Instance,
            null!,
            null!,
            null!,
            config.Object,
            imageEncoder.Object,
            CreateDbContextFactory(),
            null!,
            pathManager.Object);
    }
}
