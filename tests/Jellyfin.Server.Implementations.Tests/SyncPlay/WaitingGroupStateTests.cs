using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay.GroupStates;
using MediaBrowser.Controller.SyncPlay.PlaybackRequests;
using MediaBrowser.Controller.SyncPlay.Requests;
using MediaBrowser.Model.SyncPlay;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using SyncPlayGroup = Emby.Server.Implementations.SyncPlay.Group;

namespace Jellyfin.Server.Implementations.Tests.SyncPlay;

public class WaitingGroupStateTests
{
    [Fact]
    public void Ready_PlayingSessionReportsPositionFromBeforeSeek_IsCorrected()
    {
        var harness = new GroupHarness();
        var group = harness.Group;

        group.PositionTicks = TimeSpan.FromMinutes(10).Ticks;
        group.LastActivity = DateTime.UtcNow;

        var state = new WaitingGroupState(NullLoggerFactory.Instance) { ResumePlaying = true };

        // One member seeks half an hour in.
        state.HandleRequest(
            new SeekGroupRequest(TimeSpan.FromMinutes(40).Ticks),
            group,
            GroupStateType.Playing,
            harness.Second,
            CancellationToken.None);

        harness.Commands.Clear();

        // The other member has not applied the seek yet and reports the old position, still playing.
        state.HandleRequest(
            new ReadyGroupRequest(DateTime.UtcNow, TimeSpan.FromMinutes(10).Ticks, true, harness.PlaylistItemId),
            group,
            GroupStateType.Waiting,
            harness.First,
            CancellationToken.None);

        // It must be seeked into position, not accepted as ready and handed a pause command
        // scheduled the length of the seek into the future.
        Assert.Contains(harness.Commands, c => c.Command == SendCommandType.Seek);
        Assert.DoesNotContain(harness.Commands, c => c.Command == SendCommandType.Pause);
        Assert.True(group.IsBuffering(), "session should still be considered buffering");
    }

    [Fact]
    public void Ready_PlayingSessionRecoveringFromALongStall_IsNotSeeked()
    {
        var harness = new GroupHarness();
        var group = harness.Group;

        group.PositionTicks = TimeSpan.FromMinutes(10).Ticks;
        group.LastActivity = DateTime.UtcNow;

        var state = new WaitingGroupState(NullLoggerFactory.Instance) { ResumePlaying = true };

        // The session reports it is buffering. No seek happens, so the group position stays put.
        state.HandleRequest(
            new BufferGroupRequest(DateTime.UtcNow, group.PositionTicks, true, harness.PlaylistItemId),
            group,
            GroupStateType.Playing,
            harness.First,
            CancellationToken.None);

        harness.Commands.Clear();

        // It recovers 45 seconds later, still behind, and must be waited for rather than seeked
        // forward past content it already buffered.
        var behind = group.PositionTicks - TimeSpan.FromSeconds(45).Ticks;
        state.HandleRequest(
            new ReadyGroupRequest(DateTime.UtcNow, behind, true, harness.PlaylistItemId),
            group,
            GroupStateType.Waiting,
            harness.First,
            CancellationToken.None);

        Assert.DoesNotContain(harness.Commands, c => c.Command == SendCommandType.Seek);
    }

    [Fact]
    public void Ready_PlayingSessionSlightlyBehindGroup_IsStillTreatedAsCatchingUp()
    {
        var harness = new GroupHarness();
        var group = harness.Group;

        // A session that is a couple of seconds behind is genuinely recovering, and the group
        // is expected to wait for it rather than seek it around.
        group.PositionTicks = TimeSpan.FromMinutes(30).Ticks;
        group.LastActivity = DateTime.UtcNow;
        group.SetBuffering(harness.First, true);
        group.SetBuffering(harness.Second, true);

        var state = new WaitingGroupState(NullLoggerFactory.Instance) { ResumePlaying = true };
        harness.Commands.Clear();

        var clientPosition = group.PositionTicks - TimeSpan.FromSeconds(2).Ticks;
        state.HandleRequest(
            new ReadyGroupRequest(DateTime.UtcNow, clientPosition, true, harness.PlaylistItemId),
            group,
            GroupStateType.Waiting,
            harness.First,
            CancellationToken.None);

        Assert.DoesNotContain(harness.Commands, c => c.Command == SendCommandType.Seek);
        Assert.Contains(harness.Commands, c => c.Command == SendCommandType.Pause);
    }

    [Fact]
    public void Ready_PausedSessionOutOfPosition_IsStillCorrected()
    {
        var harness = new GroupHarness();
        var group = harness.Group;

        group.PositionTicks = TimeSpan.FromMinutes(30).Ticks;
        group.LastActivity = DateTime.UtcNow;
        group.SetBuffering(harness.First, true);
        group.SetBuffering(harness.Second, true);

        var state = new WaitingGroupState(NullLoggerFactory.Instance) { ResumePlaying = true };
        harness.Commands.Clear();

        state.HandleRequest(
            new ReadyGroupRequest(DateTime.UtcNow, 0, false, harness.PlaylistItemId),
            group,
            GroupStateType.Waiting,
            harness.First,
            CancellationToken.None);

        Assert.Contains(harness.Commands, c => c.Command == SendCommandType.Seek);
    }

    private sealed class GroupHarness
    {
        public GroupHarness()
        {
            var userManager = new Mock<IUserManager>();
            var sessionManager = new Mock<ISessionManager>();
            var libraryManager = new Mock<ILibraryManager>();

            var user = new User("tester", "auth-provider", "pwdreset-provider");
            userManager.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns(user);

            var item = new Mock<BaseItem>();
            item.Setup(i => i.IsVisibleStandalone(It.IsAny<User>())).Returns(true);
            item.Object.RunTimeTicks = TimeSpan.FromHours(2).Ticks;
            libraryManager.Setup(m => m.GetItemById(It.IsAny<Guid>())).Returns(item.Object);

            sessionManager
                .Setup(m => m.SendSyncPlayCommand(It.IsAny<string>(), It.IsAny<SendCommand>(), It.IsAny<CancellationToken>()))
                .Callback<string, SendCommand, CancellationToken>((_, command, _) => Commands.Add(command))
                .Returns(Task.CompletedTask);

            Group = new SyncPlayGroup(
                NullLoggerFactory.Instance,
                userManager.Object,
                sessionManager.Object,
                libraryManager.Object);

            First = new SessionInfo(sessionManager.Object, NullLogger.Instance)
            {
                Id = "first",
                UserId = user.Id,
                UserName = "first"
            };
            Second = new SessionInfo(sessionManager.Object, NullLogger.Instance)
            {
                Id = "second",
                UserId = user.Id,
                UserName = "second"
            };

            Group.CreateGroup(First, new NewGroupRequest("group"), CancellationToken.None);
            Group.SessionJoin(Second, new JoinGroupRequest(Group.GroupId), CancellationToken.None);
            Group.SetPlayQueue(new List<Guid> { Guid.NewGuid() }, 0, 0);
            PlaylistItemId = Group.PlayQueue.GetPlayingItemPlaylistId();
        }

        public SyncPlayGroup Group { get; }

        public SessionInfo First { get; }

        public SessionInfo Second { get; }

        public Guid PlaylistItemId { get; }

        public List<SendCommand> Commands { get; } = new List<SendCommand>();
    }
}
