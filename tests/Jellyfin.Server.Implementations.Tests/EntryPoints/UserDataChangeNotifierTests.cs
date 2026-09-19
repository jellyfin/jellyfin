using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.EntryPoints;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
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
