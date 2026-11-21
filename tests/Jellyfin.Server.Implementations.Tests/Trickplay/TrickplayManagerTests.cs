using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Tests.Item;
using Jellyfin.Server.Implementations.Trickplay;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaSegments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Trickplay;

public sealed class TrickplayManagerTests : SqliteDbTestFixture
{
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

    [Fact]
    public async Task GetTrickplayManifest_Video_ResolvesManifestWithoutLoadingStreamsOrSegments()
    {
        var movie = new Movie
        {
            Id = Guid.NewGuid(),
            Name = "Movie",
            Path = "/movies/Movie.mkv"
        };

        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = movie.Id,
                Name = movie.Name,
                Path = movie.Path,
                Type = typeof(Movie).FullName!
            });
            context.TrickplayInfos.Add(new TrickplayInfo
            {
                ItemId = movie.Id,
                Width = 320,
                Height = 180,
                TileWidth = 10,
                TileHeight = 10,
                ThumbnailCount = 120,
                Interval = 10000,
                Bandwidth = 15000
            });
            await context.SaveChangesAsync(CancellationToken.None);
        }

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(x => x.GetLinkedAlternateVersions(It.IsAny<Video>())).Returns([]);
        libraryManager.Setup(x => x.GetLocalAlternateVersionIds(It.IsAny<Video>())).Returns([]);

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        var mediaSegmentManager = new Mock<IMediaSegmentManager>();

        Video.RecordingsManager = new Mock<IRecordingsManager>().Object;
        BaseItem.LibraryManager = libraryManager.Object;
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        BaseItem.MediaSegmentManager = mediaSegmentManager.Object;

        var trickplayManager = new TrickplayManager(
            NullLogger<TrickplayManager>.Instance,
            null!,
            null!,
            null!,
            null!,
            null!,
            CreateDbContextFactory(),
            null!,
            null!);

        var manifest = await trickplayManager.GetTrickplayManifest(movie);

        var key = movie.Id.ToString("N", CultureInfo.InvariantCulture);
        Assert.True(manifest.TryGetValue(key, out var resolutions));
        Assert.True(resolutions.ContainsKey(320));
        mediaSourceManager.Verify(x => x.GetMediaStreams(It.IsAny<Guid>()), Times.Never);
        mediaSourceManager.Verify(x => x.GetMediaAttachments(It.IsAny<Guid>()), Times.Never);
        mediaSegmentManager.Verify(x => x.HasSegments(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetTrickplayManifest_RemoteShortcutVideo_SkipsRemoteSource()
    {
        var movie = new Movie
        {
            Id = Guid.NewGuid(),
            Name = "RemoteMovie",
            Path = "/movies/RemoteMovie.strm",
            IsShortcut = true,
            ShortcutPath = "https://example.com/stream.mkv"
        };

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(x => x.GetLinkedAlternateVersions(It.IsAny<Video>())).Returns([]);
        libraryManager.Setup(x => x.GetLocalAlternateVersionIds(It.IsAny<Video>())).Returns([]);

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager
            .Setup(x => x.GetPathProtocol(movie.ShortcutPath))
            .Returns(MediaBrowser.Model.MediaInfo.MediaProtocol.Http);

        Video.RecordingsManager = new Mock<IRecordingsManager>().Object;
        BaseItem.LibraryManager = libraryManager.Object;
        BaseItem.MediaSourceManager = mediaSourceManager.Object;

        var trickplayManager = new TrickplayManager(
            NullLogger<TrickplayManager>.Instance,
            null!,
            null!,
            null!,
            null!,
            null!,
            CreateDbContextFactory(),
            null!,
            null!);

        var manifest = await trickplayManager.GetTrickplayManifest(movie);

        Assert.Empty(manifest);
    }
}
