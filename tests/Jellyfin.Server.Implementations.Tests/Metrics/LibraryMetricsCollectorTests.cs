using System;
using System.Collections.Generic;
using Jellyfin.Data.Queries;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.Implementations.Metrics;
using Jellyfin.Server.Implementations.Tests.Item;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Metrics;

[Collection(MetricsTestCollection.Name)]
public sealed class LibraryMetricsCollectorTests : SqliteDbTestFixture
{
    [Fact]
    public void RefreshGauges_ReportsCountsAndZerosRemovedLabels()
    {
        var itemId = Guid.NewGuid();
        var libraryId = Guid.NewGuid();
        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = itemId,
                Type = "MediaBrowser.Controller.Entities.Movies.Movie",
                Name = "Metrics test movie",
                TopParentId = libraryId
            });
            context.MediaStreamInfos.Add(new MediaStreamInfo
            {
                ItemId = itemId,
                Item = null!,
                StreamIndex = 0,
                StreamType = MediaStreamTypeEntity.Video,
                Codec = "H264"
            });
            context.SaveChanges();
        }

        var folders = new List<VirtualFolderInfo>
        {
            new VirtualFolderInfo
            {
                Name = "Movies",
                ItemId = libraryId.ToString("N"),
                CollectionType = CollectionTypeOptions.movies
            },
            new VirtualFolderInfo { CollectionType = null }
        };
        var itemCounts = new ItemCounts { MovieCount = 7, SongCount = 3 };
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(x => x.GetVirtualFolders()).Returns(() => folders);
        var itemCountService = new Mock<IItemCountService>();
        itemCountService.Setup(x => x.GetItemCounts(It.IsAny<InternalItemsQuery>())).Returns(() => itemCounts);
        var collector = new LibraryMetricsCollector(
            libraryManager.Object,
            itemCountService.Object,
            CreateDbContextFactory());

        collector.RefreshGauges();

        Assert.Equal(1d, JellyfinMetrics.Libraries.WithLabels("movies").Value);
        Assert.Equal(1d, JellyfinMetrics.Libraries.WithLabels("unknown").Value);
        Assert.Equal(7d, JellyfinMetrics.LibraryItems.WithLabels("movie").Value);
        Assert.Equal(3d, JellyfinMetrics.LibraryItems.WithLabels("song").Value);
        Assert.Equal(1d, JellyfinMetrics.MediaStreams
            .WithLabels("Movies", libraryId.ToString("N"), "video", "h264")
            .Value);

        folders = [new VirtualFolderInfo { CollectionType = CollectionTypeOptions.music }];
        itemCounts = new ItemCounts { AlbumCount = 2 };
        collector.RefreshGauges();

        Assert.Equal(0d, JellyfinMetrics.Libraries.WithLabels("movies").Value);
        Assert.Equal(0d, JellyfinMetrics.Libraries.WithLabels("unknown").Value);
        Assert.Equal(1d, JellyfinMetrics.Libraries.WithLabels("music").Value);
        Assert.Equal(0d, JellyfinMetrics.LibraryItems.WithLabels("movie").Value);
        Assert.Equal(2d, JellyfinMetrics.LibraryItems.WithLabels("album").Value);
    }
}
