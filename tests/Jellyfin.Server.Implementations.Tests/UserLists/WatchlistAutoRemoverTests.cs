using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.EntryPoints;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.UserLists;

public sealed class WatchlistAutoRemoverTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<IItemCountService> _itemCountService = new();
    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly Mock<IUserDataManager> _userDataManager = new();
    private readonly Mock<IUserListManager> _userListManager = new();
    private readonly Mock<IUserManager> _userManager = new();
    private readonly WatchlistAutoRemover _autoRemover;

    public WatchlistAutoRemoverTests()
    {
        var user = new User("watchlist-user", "authentication", "password-reset")
        {
            Id = _userId
        };
        _userManager.Setup(manager => manager.GetUserById(_userId)).Returns(user);
        _userListManager
            .Setup(manager => manager.RemoveItemAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        _autoRemover = new WatchlistAutoRemover(
            NullLogger<WatchlistAutoRemover>.Instance,
            _itemCountService.Object,
            _libraryManager.Object,
            _userDataManager.Object,
            _userListManager.Object,
            _userManager.Object);
    }

    [Fact]
    public async Task PlaybackFinished_PlayedMovie_RemovesOnlyFromAutoRemoveLists()
    {
        var movie = new Movie { Id = Guid.NewGuid() };
        var autoRemoveList = CreateList(true);
        var retainedList = CreateList(false);
        SetupLists(autoRemoveList, retainedList);

        await RaiseUserDataSavedAsync(movie, UserDataSaveReason.PlaybackFinished, true);

        _userListManager.Verify(
            manager => manager.RemoveItemAsync(autoRemoveList.Id, movie.Id),
            Times.Once);
        _userListManager.Verify(
            manager => manager.RemoveItemAsync(retainedList.Id, movie.Id),
            Times.Never);
        _userListManager.Verify(
            manager => manager.RemoveItemAsync(It.IsAny<Guid>(), movie.Id),
            Times.Once);
    }

    [Fact]
    public async Task PlaybackFinished_SingleEpisodeCompleted_DoesNotRemoveParentSeries()
    {
        var (episode, series) = SetupEpisodeAndSeriesCounts(1, 3);
        var autoRemoveList = CreateList(true);
        SetupLists(autoRemoveList);

        await RaiseUserDataSavedAsync(episode, UserDataSaveReason.PlaybackFinished, true);

        _userListManager.Verify(
            manager => manager.RemoveItemAsync(autoRemoveList.Id, episode.Id),
            Times.Once);
        _userListManager.Verify(
            manager => manager.RemoveItemAsync(It.IsAny<Guid>(), series.Id),
            Times.Never);
    }

    [Fact]
    public async Task PlaybackFinished_FinalEpisodeCompleted_RemovesParentSeries()
    {
        var (episode, series) = SetupEpisodeAndSeriesCounts(3, 3);
        var autoRemoveList = CreateList(true);
        SetupLists(autoRemoveList);

        await RaiseUserDataSavedAsync(episode, UserDataSaveReason.PlaybackFinished, true);

        _userListManager.Verify(
            manager => manager.RemoveItemAsync(autoRemoveList.Id, episode.Id),
            Times.Once);
        _userListManager.Verify(
            manager => manager.RemoveItemAsync(autoRemoveList.Id, series.Id),
            Times.Once);
    }

    [Fact]
    public async Task PlaybackFinished_FinalEpisodeCompleted_RetainsSeriesInNonAutoRemoveList()
    {
        var (episode, series) = SetupEpisodeAndSeriesCounts(3, 3);
        var autoRemoveList = CreateList(true);
        var retainedList = CreateList(false);
        SetupLists(autoRemoveList, retainedList);

        await RaiseUserDataSavedAsync(episode, UserDataSaveReason.PlaybackFinished, true);

        _userListManager.Verify(
            manager => manager.RemoveItemAsync(autoRemoveList.Id, series.Id),
            Times.Once);
        _userListManager.Verify(
            manager => manager.RemoveItemAsync(retainedList.Id, series.Id),
            Times.Never);
    }

    [Theory]
    [InlineData(UserDataSaveReason.PlaybackProgress, true)]
    [InlineData(UserDataSaveReason.PlaybackFinished, false)]
    public async Task UserDataSaved_NotPlayedToCompletion_RemovesNothing(
        UserDataSaveReason saveReason,
        bool played)
    {
        var movie = new Movie { Id = Guid.NewGuid() };
        SetupLists(CreateList(true));

        await RaiseUserDataSavedAsync(movie, saveReason, played);

        _userListManager.Verify(
            manager => manager.GetListsAsync(It.IsAny<Guid>()),
            Times.Never);
        _userListManager.Verify(
            manager => manager.RemoveItemAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
    }

    private UserList CreateList(bool autoRemoveWatched)
    {
        return new UserList
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Name = autoRemoveWatched ? "Auto remove" : "Retained",
            AutoRemoveWatched = autoRemoveWatched
        };
    }

    private (Episode Episode, Series Series) SetupEpisodeAndSeriesCounts(int playedCount, int totalCount)
    {
        var series = new Series { Id = Guid.NewGuid() };
        var episode = new Episode
        {
            Id = Guid.NewGuid(),
            SeriesId = series.Id
        };
        _libraryManager
            .Setup(manager => manager.GetItemById<Series>(series.Id))
            .Returns(series);
        _itemCountService
            .Setup(service => service.GetPlayedAndTotalCount(
                It.Is<InternalItemsQuery>(query => query.User != null && query.User.Id.Equals(_userId)),
                series.Id))
            .Returns((playedCount, totalCount));
        return (episode, series);
    }

    private void SetupLists(params UserList[] lists)
    {
        _userListManager
            .Setup(manager => manager.GetListsAsync(_userId))
            .ReturnsAsync((IReadOnlyList<UserList>)lists);
    }

    private async Task RaiseUserDataSavedAsync(
        BaseItem item,
        UserDataSaveReason saveReason,
        bool played)
    {
        await _autoRemover.StartAsync(CancellationToken.None);
        _userDataManager.Raise(
            manager => manager.UserDataSaved += null,
            new UserDataSaveEventArgs
            {
                Item = item,
                Keys = [],
                SaveReason = saveReason,
                UserData = new UserItemData
                {
                    Key = item.Id.ToString("N"),
                    Played = played
                },
                UserId = _userId
            });
        await _autoRemover.StopAsync(CancellationToken.None);
    }
}
