using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.EntryPoints;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.EntryPoints;

public class UserDataChangeNotifierTests
{
    // How long a test waits for the notifier's timer callback to run. Generous: the assertions are
    // about a batch being sent at all, not about how promptly.
    private static readonly TimeSpan _flushTimeout = TimeSpan.FromSeconds(15);

    private readonly Mock<IUserDataManager> _userDataManager = new();
    private readonly Mock<ISessionManager> _sessionManager = new();
    private readonly Mock<IUserManager> _userManager = new();

    private int _flushCount;

    public UserDataChangeNotifierTests()
    {
        _sessionManager
            .Setup(e => e.SendMessageToUserSessions(
                It.IsAny<System.Collections.Generic.List<Guid>>(),
                SessionMessageType.UserDataChanged,
                It.IsAny<Func<UserDataChangeInfo>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => Interlocked.Increment(ref _flushCount))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task OnUserDataSaved_ChangesNeverPause_StillSendsOnTheWindow()
    {
        // A scan changes user data continuously. The window must run from the first change of a batch,
        // or the batch never closes and holds every item it named alive for the length of the scan.
        var notifier = CreateNotifier();
        await notifier.StartAsync(TestContext.Current.CancellationToken);

        var userId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < _flushTimeout && Volatile.Read(ref _flushCount) == 0)
        {
            // Well below the window, and well below the size cap over the whole loop.
            RaiseUserDataSaved(userId);
            await Task.Delay(25, TestContext.Current.CancellationToken);
        }

        Assert.True(Volatile.Read(ref _flushCount) > 0, "The batch was never sent while changes kept arriving.");

        await notifier.StopAsync(TestContext.Current.CancellationToken);
        notifier.Dispose();
    }

    [Fact]
    public async Task OnUserDataSaved_Episode_AlsoSendsItsSeries()
    {
        // Series cards show an unplayed count too, but an episode's parent is its season,
        // so going up one level never reaches the series.
        var series = new Series { Id = Guid.NewGuid() };
        var season = new Season { Id = Guid.NewGuid(), ParentId = series.Id, SeriesId = series.Id };
        var episode = new Episode { Id = Guid.NewGuid(), ParentId = season.Id, SeasonId = season.Id, SeriesId = series.Id };

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(e => e.GetItemById(season.Id)).Returns(season);
        libraryManager.Setup(e => e.GetItemById(series.Id)).Returns(series);

        var user = new User("user", "provider", "resetProvider");
        _userManager.Setup(e => e.GetUserById(It.IsAny<Guid>())).Returns(user);
        _userDataManager
            .Setup(e => e.GetUserDataDto(It.IsAny<BaseItem>(), user))
            .Returns(() => new UserItemDataDto { Key = string.Empty });

        UserDataChangeInfo? sent = null;
        var flushed = new TaskCompletionSource();
        _sessionManager
            .Setup(e => e.SendMessageToUserSessions(
                It.IsAny<List<Guid>>(),
                SessionMessageType.UserDataChanged,
                It.IsAny<Func<UserDataChangeInfo>>(),
                It.IsAny<CancellationToken>()))
            .Callback<List<Guid>, SessionMessageType, Func<UserDataChangeInfo>, CancellationToken>((_, _, getData, _) =>
            {
                sent = getData();
                flushed.TrySetResult();
            })
            .Returns(Task.CompletedTask);

        var previousLibraryManager = BaseItem.LibraryManager;
        BaseItem.LibraryManager = libraryManager.Object;
        try
        {
            var notifier = CreateNotifier();
            await notifier.StartAsync(TestContext.Current.CancellationToken);

            _userDataManager.Raise(
                e => e.UserDataSaved += null,
                _userDataManager.Object,
                new UserDataSaveEventArgs
                {
                    UserId = Guid.NewGuid(),
                    SaveReason = UserDataSaveReason.TogglePlayed,
                    Item = episode
                });

            await flushed.Task.WaitAsync(_flushTimeout, TestContext.Current.CancellationToken);

            await notifier.StopAsync(TestContext.Current.CancellationToken);
            notifier.Dispose();
        }
        finally
        {
            BaseItem.LibraryManager = previousLibraryManager;
        }

        Assert.NotNull(sent);
        Assert.Equal(
            new HashSet<Guid> { episode.Id, season.Id, series.Id },
            sent.UserDataList.Select(e => e.ItemId).ToHashSet());
    }

    private UserDataChangeNotifier CreateNotifier()
        => new(_userDataManager.Object, _sessionManager.Object, _userManager.Object);

    // A folder needs none of BaseItem's static services, and PlaybackProgress is the one reason the
    // notifier ignores outright.
    private void RaiseUserDataSaved(Guid userId)
        => _userDataManager.Raise(
            e => e.UserDataSaved += null,
            _userDataManager.Object,
            new UserDataSaveEventArgs
            {
                UserId = userId,
                SaveReason = UserDataSaveReason.UpdateUserRating,
                Item = new Folder { Id = Guid.NewGuid() }
            });
}
