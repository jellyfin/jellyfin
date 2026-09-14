using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay.PlaybackRequests;
using MediaBrowser.Controller.SyncPlay.Requests;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using SyncPlayGroup = Emby.Server.Implementations.SyncPlay.Group;
using SyncPlayManager = Emby.Server.Implementations.SyncPlay.SyncPlayManager;

namespace Jellyfin.Server.Implementations.Tests.SyncPlay;

public class SyncPlayManagerTests
{
    [Fact]
    public void LeaveGroup_AfterJoiningTheSameGroupTwice_ClearsTheActiveSessionCounter()
    {
        var harness = new ManagerHarness();

        var info = harness.Manager.NewGroup(harness.Session, new NewGroupRequest("group"), CancellationToken.None);
        Assert.True(harness.Manager.IsUserActive(harness.User.Id));

        // A client that re-sends Join for the group it is already in must not be counted twice.
        harness.Manager.JoinGroup(harness.Session, new JoinGroupRequest(info.GroupId), CancellationToken.None);
        harness.Manager.LeaveGroup(harness.Session, new LeaveGroupRequest(), CancellationToken.None);

        Assert.False(harness.Manager.IsUserActive(harness.User.Id));
    }

    [Fact]
    public void LeaveGroup_AfterASingleJoin_ClearsTheActiveSessionCounter()
    {
        var harness = new ManagerHarness();

        harness.Manager.NewGroup(harness.Session, new NewGroupRequest("group"), CancellationToken.None);
        harness.Manager.LeaveGroup(harness.Session, new LeaveGroupRequest(), CancellationToken.None);

        Assert.False(harness.Manager.IsUserActive(harness.User.Id));
    }

    [Fact]
    public void IsUserActive_WithTwoSessionsOfTheSameUser_TracksBothSeparately()
    {
        var harness = new ManagerHarness();
        var second = harness.CreateSession("session-2");

        var info = harness.Manager.NewGroup(harness.Session, new NewGroupRequest("group"), CancellationToken.None);
        harness.Manager.JoinGroup(second, new JoinGroupRequest(info.GroupId), CancellationToken.None);

        harness.Manager.LeaveGroup(harness.Session, new LeaveGroupRequest(), CancellationToken.None);
        Assert.True(harness.Manager.IsUserActive(harness.User.Id));

        harness.Manager.LeaveGroup(second, new LeaveGroupRequest(), CancellationToken.None);
        Assert.False(harness.Manager.IsUserActive(harness.User.Id));
    }

    [Fact]
    public async Task HandleRequest_GroupWaitsForAMemberThatNeverReportsReady_RecoversOnItsOwn()
    {
        var harness = new ManagerHarness(groupWaitTimeout: 200);
        var second = harness.CreateSession("session-2");

        var info = harness.Manager.NewGroup(harness.Session, new NewGroupRequest("group"), CancellationToken.None);
        harness.Manager.JoinGroup(second, new JoinGroupRequest(info.GroupId), CancellationToken.None);

        // Starting playback puts the group behind the ready barrier.
        harness.Manager.HandleRequest(
            harness.Session,
            new PlayGroupRequest(new[] { Guid.NewGuid() }, 0, 0),
            CancellationToken.None);
        Assert.Equal(GroupStateType.Waiting, harness.Manager.GetGroup(harness.Session, info.GroupId).State);

        // Neither session ever reports ready, so the group has to come out of the wait by itself.
        Assert.Equal(
            GroupStateType.Playing,
            await harness.WaitForState(harness.Session, info.GroupId, GroupStateType.Playing));
    }

    private sealed class ManagerHarness
    {
        private readonly Mock<ISessionManager> _sessionManager = new();

        public ManagerHarness(long? groupWaitTimeout = null)
        {
            var userManager = new Mock<IUserManager>();
            var libraryManager = new Mock<ILibraryManager>();

            User = new User("tester", "auth-provider", "pwdreset-provider");
            userManager.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(User);

            var item = new Mock<BaseItem>();
            item.Setup(i => i.IsVisibleStandalone(It.IsAny<User>())).Returns(true);
            item.Object.RunTimeTicks = TimeSpan.FromHours(2).Ticks;
            libraryManager.Setup(m => m.GetItemById(It.IsAny<Guid>())).Returns(item.Object);

            _sessionManager
                .Setup(m => m.SendSyncPlayCommand(It.IsAny<string>(), It.IsAny<SendCommand>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _sessionManager
                .Setup(m => m.SendSyncPlayGroupUpdate(It.IsAny<string>(), It.IsAny<GroupUpdate<GroupStateUpdate>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Manager = new SyncPlayManager(
                NullLoggerFactory.Instance,
                userManager.Object,
                _sessionManager.Object,
                libraryManager.Object)
            {
                GroupWaitTimeout = groupWaitTimeout ?? SyncPlayGroup.DefaultGroupWaitTimeout
            };

            Session = CreateSession("session-1");
        }

        public SyncPlayManager Manager { get; }

        public User User { get; }

        public SessionInfo Session { get; }

        public SessionInfo CreateSession(string id)
        {
            return new SessionInfo(_sessionManager.Object, NullLogger.Instance)
            {
                Id = id,
                UserId = User.Id,
                UserName = User.Username
            };
        }

        public async Task<GroupStateType> WaitForState(SessionInfo session, Guid groupId, GroupStateType expected)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            GroupStateType state;
            while ((state = Manager.GetGroup(session, groupId).State) != expected && DateTime.UtcNow < deadline)
            {
                await Task.Delay(20, TestContext.Current.CancellationToken);
            }

            return state;
        }
    }
}
