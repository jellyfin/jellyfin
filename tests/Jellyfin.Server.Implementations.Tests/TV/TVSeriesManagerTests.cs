using System;
using System.Collections.Generic;
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
using MediaBrowser.Model.Querying;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.TV;

[Collection("TVSeriesManager static state")]
public sealed class TVSeriesManagerTests
{
    [Fact]
    public void NextUp_RanksRecentPartialPlaybackAheadOfAnOlderCompletedEpisode()
    {
        var oldLastWatched = new Episode { Id = Guid.NewGuid() };
        var oldNext = new Episode { Id = Guid.NewGuid() };
        var recentNext = new Episode { Id = Guid.NewGuid() };
        var oldDate = new DateTime(2025, 1, 1);
        var results = new Dictionary<string, NextUpEpisodeBatchResult>
        {
            ["old"] = new() { LastWatched = oldLastWatched, NextUp = oldNext },
            ["recent-partial"] = new() { LastPlayedDate = new DateTime(2025, 1, 2), NextUp = recentNext }
        };

        var nextUp = GetNextUp(["old", "recent-partial"], results, new Dictionary<Guid, UserItemData>
        {
            [oldLastWatched.Id] = CreateUserData(oldLastWatched.Id, lastPlayedDate: oldDate),
            [recentNext.Id] = CreateUserData(recentNext.Id, playbackPositionTicks: 1)
        });

        Assert.Equal([recentNext.Id, oldNext.Id], nextUp.Items.Select(item => item.Id));
    }

    [Fact]
    public void NextUp_PagesSortedFirstPartialEpisodesWithoutCompletedPredecessors()
    {
        var oldest = new Episode { Id = Guid.NewGuid() };
        var middle = new Episode { Id = Guid.NewGuid() };
        var newest = new Episode { Id = Guid.NewGuid() };
        var results = new Dictionary<string, NextUpEpisodeBatchResult>
        {
            ["oldest"] = new() { LastPlayedDate = new DateTime(2025, 1, 1), NextUp = oldest },
            ["newest"] = new() { LastPlayedDate = new DateTime(2025, 1, 3), NextUp = newest },
            ["middle"] = new() { LastPlayedDate = new DateTime(2025, 1, 2), NextUp = middle }
        };

        var nextUp = GetNextUp(
            ["oldest", "newest", "middle"],
            results,
            new Dictionary<Guid, UserItemData>(),
            startIndex: 1,
            limit: 1);

        Assert.Equal([middle.Id], nextUp.Items.Select(item => item.Id));
    }

    [Fact]
    public void NextUp_KeepsResumableExclusionAndRewatchInterleaving()
    {
        var normal = new Episode { Id = Guid.NewGuid() };
        var resumable = new Episode { Id = Guid.NewGuid() };
        var rewatchAnchor = new Episode { Id = Guid.NewGuid() };
        var rewatch = new Episode { Id = Guid.NewGuid() };
        var results = new Dictionary<string, NextUpEpisodeBatchResult>
        {
            ["normal"] = new() { LastPlayedDate = new DateTime(2025, 1, 3), NextUp = normal },
            ["resumable"] = new() { LastPlayedDate = new DateTime(2025, 1, 2), NextUp = resumable },
            ["rewatch"] = new()
            {
                LastWatchedForRewatching = rewatchAnchor,
                NextPlayedForRewatching = rewatch
            }
        };

        var data = new Dictionary<Guid, UserItemData>
        {
            [rewatchAnchor.Id] = CreateUserData(rewatchAnchor.Id, lastPlayedDate: new DateTime(2025, 1, 1)),
            [resumable.Id] = CreateUserData(resumable.Id, playbackPositionTicks: 1)
        };

        var nextUp = GetNextUp(
            ["normal", "resumable", "rewatch"],
            results,
            data,
            enableRewatching: true,
            enableResumable: false);

        Assert.Equal([normal.Id, rewatch.Id], nextUp.Items.Select(item => item.Id));
    }

    private static QueryResult<BaseItem> GetNextUp(
        IReadOnlyList<string> seriesKeys,
        IReadOnlyDictionary<string, NextUpEpisodeBatchResult> results,
        IReadOnlyDictionary<Guid, UserItemData> userData,
        int? startIndex = null,
        int? limit = null,
        bool enableRewatching = false,
        bool enableResumable = true)
    {
        var user = new User("user", "auth", "reset");
        var userDataManager = new Mock<IUserDataManager>();
        userDataManager.Setup(manager => manager.GetUserDataBatch(It.IsAny<IReadOnlyList<BaseItem>>(), user))
            .Returns((IReadOnlyList<BaseItem> items, User _) => items
                .Where(item => userData.ContainsKey(item.Id))
                .ToDictionary(item => item.Id, item => userData[item.Id]));
        userDataManager.Setup(manager => manager.GetUserData(user, It.IsAny<BaseItem>()))
            .Returns((User _, BaseItem item) => userData.GetValueOrDefault(item.Id));

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(manager => manager.GetNextUpSeriesKeys(
            It.IsAny<InternalItemsQuery>(),
            It.IsAny<IReadOnlyCollection<BaseItem>>(),
            It.IsAny<DateTime>()))
            .Returns(seriesKeys);
        libraryManager.Setup(manager => manager.GetNextUpEpisodesBatch(
            It.IsAny<InternalItemsQuery>(),
            It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>()))
            .Returns(results);
        libraryManager.Setup(manager => manager.GetLinkedAlternateVersions(It.IsAny<Video>())).Returns([]);
        libraryManager.Setup(manager => manager.GetLocalAlternateVersionIds(It.IsAny<Video>())).Returns([]);

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.SetupGet(manager => manager.Configuration).Returns(new ServerConfiguration());

        var manager = new TVSeriesManager(userDataManager.Object, libraryManager.Object, configurationManager.Object);
        var previousLibraryManager = BaseItem.LibraryManager;
        BaseItem.LibraryManager = libraryManager.Object;
        try
        {
            return manager.GetNextUp(
                new NextUpQuery
                {
                    User = user,
                    StartIndex = startIndex,
                    Limit = limit,
                    EnableRewatching = enableRewatching,
                    EnableResumable = enableResumable
                },
                [],
                new DtoOptions());
        }
        finally
        {
            BaseItem.LibraryManager = previousLibraryManager;
        }
    }

    private static UserItemData CreateUserData(Guid itemId, DateTime? lastPlayedDate = null, long playbackPositionTicks = 0)
        => new()
        {
            Key = itemId.ToString("N"),
            LastPlayedDate = lastPlayedDate,
            PlaybackPositionTicks = playbackPositionTicks
        };
}
