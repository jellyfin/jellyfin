using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Emby.Server.Implementations.TV;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Moq;
using Moq.Protected;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests;

public sealed class TVSeriesManagerPagingTests
{
    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 1)]
    [InlineData(false, true, 1)]
    [InlineData(true, true, 6)]
    public void NextUp_ResolvesPreferredVoicesOnlyForReturnedPage(bool count, bool rewatch, int start)
    {
        var library = new Mock<ILibraryManager>();
        var userData = new Mock<IUserDataManager>();
        var config = new Mock<IServerConfigurationManager>();
        config.SetupGet(x => x.Configuration).Returns(new ServerConfiguration { DisplaySpecialsWithinSeasons = false });
        var user = new User("test", "auth", "reset");
        var batch = new Dictionary<string, NextUpEpisodeBatchResult>();
        var dates = new Dictionary<Guid, UserItemData>();
        var nextEpisodes = new List<CountingEpisode>();
        var watchedEpisodes = new List<CountingEpisode>();
        var matchingVersions = new List<Episode>();
        for (var i = 0; i < 3; i++)
        {
            var watched = new CountingEpisode { Id = Guid.NewGuid() };
            var played = new Episode { Id = Guid.NewGuid(), PrimaryVersionId = watched.Id };
            watched.Versions = [watched.Item, played];
            watched.Sources = [new MediaSourceInfo { Id = played.Id.ToString("N", CultureInfo.InvariantCulture), Name = "Voice" }];
            dates[played.Id] = new UserItemData { Key = "played", LastPlayedDate = new DateTime(2026, 1, 3 - i, 0, 0, 0, DateTimeKind.Utc) };
            var next = new CountingEpisode { Id = Guid.NewGuid() };
            next.Versions = [next.Item];
            var matching = new Episode { Id = Guid.NewGuid(), PrimaryVersionId = next.Id };
            next.Sources = [new MediaSourceInfo { Id = matching.Id.ToString("N", CultureInfo.InvariantCulture), Name = "Voice" }];
            library.Setup(x => x.GetItemById<Episode>(matching.Id)).Returns(matching);
            batch[i.ToString(CultureInfo.InvariantCulture)] = new NextUpEpisodeBatchResult
            {
                LastWatched = watched.Item,
                NextUp = next.Item,
                LastWatchedForRewatching = watched.Item,
                NextPlayedForRewatching = next.Item
            };
            nextEpisodes.Add(next);
            watchedEpisodes.Add(watched);
            matchingVersions.Add(matching);
        }

        var keys = batch.Keys.ToArray();
        library.Setup(x => x.GetNextUpSeriesKeys(It.IsAny<InternalItemsQuery>(), It.IsAny<IReadOnlyCollection<BaseItem>>(), It.IsAny<DateTime>())).Returns(keys);
        library.Setup(x => x.GetNextUpEpisodesBatch(It.IsAny<InternalItemsQuery>(), It.IsAny<IReadOnlyList<string>>(), false, rewatch)).Returns(batch);
        userData.Setup(x => x.GetUserDataBatch(It.IsAny<IReadOnlyList<BaseItem>>(), user))
            .Returns((IReadOnlyList<BaseItem> items, User _) => items.ToDictionary(x => x.Id, x => dates.GetValueOrDefault(x.Id) ?? new UserItemData { Key = "unplayed" }));
        var manager = new TVSeriesManager(userData.Object, library.Object, config.Object);
        var query = new NextUpQuery
        {
            User = user,
            StartIndex = start,
            Limit = 1,
            EnableTotalRecordCount = count,
            EnableResumable = true,
            EnableRewatching = rewatch
        };
        var result = manager.GetNextUp(query, Array.Empty<BaseItem>(), new DtoOptions());
        var total = rewatch ? 6 : 3;
        Assert.Equal(count ? total : 0, result.TotalRecordCount);
        Assert.Equal(start, result.StartIndex);
        Assert.Equal(start < total ? 1 : 0, result.Items.Count);
        Assert.Equal(result.Items.Count, nextEpisodes.Sum(x => x.SourceReads));
        Assert.Equal(result.Items.Count, watchedEpisodes.Sum(x => x.SourceReads));
        if (start < total)
        {
            Assert.Same(matchingVersions[rewatch ? start / 2 : start], result.Items[0]);
        }
    }

    private sealed class CountingEpisode
    {
        private readonly Mock<Episode> _episode = new() { CallBase = true };

        public CountingEpisode()
        {
            _episode.Setup(x => x.GetMediaSources(false)).Returns(() =>
            {
                SourceReads++;
                return Sources;
            });
            _episode.Protected()
                .Setup<IEnumerable<(BaseItem Item, MediaSourceType MediaSourceType)>>("GetAllItemsForMediaSources")
                .Returns(() => Versions.Select(x => ((BaseItem)x, MediaSourceType.Default)));
        }

        public Episode Item => _episode.Object;

        public Guid Id
        {
            get => Item.Id;
            set => Item.Id = value;
        }

        public IReadOnlyList<Video> Versions { get; set; } = [];

        public IReadOnlyList<MediaSourceInfo> Sources { get; set; } = [];

        public int SourceReads { get; private set; }
    }
}
