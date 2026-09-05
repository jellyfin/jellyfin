using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Tests.Item;
using Jellyfin.Server.Implementations.Trickplay;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using IConfigurationManager = MediaBrowser.Common.Configuration.IConfigurationManager;

namespace Jellyfin.Server.Implementations.Tests.Trickplay;

public class TrickplayManagerTests : SqliteDbTestFixture
{
    [Fact]
    public async Task GetTrickplayManifest_RemoteSourceWithPersistedTrickplay_IncludesPersistedInfo()
    {
        var sourceId = Guid.NewGuid();
        var expected = CreateTrickplayInfo(sourceId);
        await SaveTrickplayInfo(expected);
        var item = CreateVideo(sourceId, isRemote: true);

        var manifest = await CreateManager().GetTrickplayManifest(item);

        var sourceManifest = Assert.Single(manifest);
        Assert.Equal(sourceId.ToString("N"), sourceManifest.Key);
        var actual = Assert.Single(sourceManifest.Value).Value;
        Assert.Equal(expected.ItemId, actual.ItemId);
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);
        Assert.Equal(expected.Interval, actual.Interval);
        Assert.Equal(expected.TileWidth, actual.TileWidth);
        Assert.Equal(expected.TileHeight, actual.TileHeight);
        Assert.Equal(expected.ThumbnailCount, actual.ThumbnailCount);
        Assert.Equal(expected.Bandwidth, actual.Bandwidth);
    }

    [Fact]
    public async Task GetTrickplayManifest_RemoteSourceWithoutTrickplay_OmitsSource()
    {
        var item = CreateVideo(Guid.NewGuid(), isRemote: true);

        var manifest = await CreateManager().GetTrickplayManifest(item);

        Assert.Empty(manifest);
    }

    [Fact]
    public async Task GetTrickplayManifest_LocalSourceWithPersistedTrickplay_IncludesPersistedInfo()
    {
        var sourceId = Guid.NewGuid();
        var expected = CreateTrickplayInfo(sourceId);
        await SaveTrickplayInfo(expected);
        var item = CreateVideo(sourceId, isRemote: false);

        var manifest = await CreateManager().GetTrickplayManifest(item);

        var actual = Assert.Single(Assert.Single(manifest).Value).Value;
        Assert.Equal(expected.ItemId, actual.ItemId);
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);
        Assert.Equal(expected.Interval, actual.Interval);
        Assert.Equal(expected.TileWidth, actual.TileWidth);
        Assert.Equal(expected.TileHeight, actual.TileHeight);
        Assert.Equal(expected.ThumbnailCount, actual.ThumbnailCount);
        Assert.Equal(expected.Bandwidth, actual.Bandwidth);
    }

    private static TrickplayInfo CreateTrickplayInfo(Guid sourceId)
    {
        return new TrickplayInfo
        {
            ItemId = sourceId,
            Width = 320,
            Height = 180,
            Interval = 10_000,
            TileWidth = 10,
            TileHeight = 10,
            ThumbnailCount = 321,
            Bandwidth = 42_000
        };
    }

    private static Video CreateVideo(Guid sourceId, bool isRemote)
    {
        return new TestVideo(
            new MediaSourceInfo
            {
                Id = sourceId.ToString("N"),
                IsRemote = isRemote
            });
    }

    private async Task SaveTrickplayInfo(TrickplayInfo trickplayInfo)
    {
        await using var context = CreateDbContext();
        context.TrickplayInfos.Add(trickplayInfo);
        await context.SaveChangesAsync();
    }

    private TrickplayManager CreateManager()
    {
        var mediaEncoder = Mock.Of<IMediaEncoder>();
        var pathManager = Mock.Of<IPathManager>();
        var dbContextFactory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        dbContextFactory
            .Setup(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDbContext);
        var encodingHelper = new EncodingHelper(
            ApplicationPaths,
            mediaEncoder,
            Mock.Of<ISubtitleEncoder>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<IConfigurationManager>(),
            pathManager);

        return new TrickplayManager(
            NullLogger<TrickplayManager>.Instance,
            mediaEncoder,
            Mock.Of<IFileSystem>(),
            encodingHelper,
            Mock.Of<IServerConfigurationManager>(),
            Mock.Of<IImageEncoder>(),
            dbContextFactory.Object,
            ApplicationPaths,
            pathManager);
    }

    private sealed class TestVideo(MediaSourceInfo mediaSource) : Video
    {
        private readonly IReadOnlyList<MediaSourceInfo> _mediaSources = [mediaSource];

        public override IReadOnlyList<MediaSourceInfo> GetMediaSources(bool enablePathSubstitution)
        {
            return _mediaSources;
        }
    }
}
