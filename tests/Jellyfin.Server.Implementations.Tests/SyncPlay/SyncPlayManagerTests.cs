using System;
using System.Threading;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay.Requests;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
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

    private sealed class ManagerHarness
    {
        private readonly Mock<ISessionManager> _sessionManager = new();

        public ManagerHarness()
        {
            var userManager = new Mock<IUserManager>();
            var libraryManager = new Mock<ILibraryManager>();

            User = new User("tester", "auth-provider", "pwdreset-provider");
            userManager.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(User);

            Manager = new SyncPlayManager(
                NullLoggerFactory.Instance,
                userManager.Object,
                _sessionManager.Object,
                libraryManager.Object);

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
    }
}
