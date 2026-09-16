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

    [Fact]
    public void Ready_ClientResumedWithLowPing_AppliesTheDefaultPingFloorInMilliseconds()
    {
        var harness = new GroupHarness();
        var group = harness.Group;

        // Both members report a ping well under the default, so the floor is what decides the delay.
        group.UpdatePing(harness.First, 10);
        group.UpdatePing(harness.Second, 10);

        group.PositionTicks = TimeSpan.FromMinutes(5).Ticks;
        group.LastActivity = DateTime.UtcNow;
        group.SetBuffering(harness.First, true);
        group.SetBuffering(harness.Second, false);

        var state = new WaitingGroupState(NullLoggerFactory.Instance) { ResumePlaying = true };

        var before = DateTime.UtcNow;
        state.HandleRequest(
            new ReadyGroupRequest(DateTime.UtcNow, group.PositionTicks, true, harness.PlaylistItemId),
            group,
            GroupStateType.Waiting,
            harness.First,
            CancellationToken.None);

        // DefaultPing is expressed in milliseconds, so the floor must be converted before being
        // compared against a tick count. Without the conversion the floor is 500 ticks (0.05 ms)
        // and never applies.
        var scheduledDelay = group.LastActivity - before;
        Assert.True(
            scheduledDelay >= TimeSpan.FromMilliseconds(group.DefaultPing),
            $"expected a resume delay of at least {group.DefaultPing} ms, got {scheduledDelay.TotalMilliseconds} ms");
    }

    [Theory]
    [InlineData(4_000_000_000L)]
    [InlineData(1_000_000_000_000_000L)]
    [InlineData(long.MaxValue)]
    [InlineData(-1L)]
    public void UpdatePing_ClientReportsAnUnusablePing_IsClampedAndCannotStallTheGroup(long reportedPing)
    {
        var harness = new GroupHarness();
        var group = harness.Group;

        group.UpdatePing(harness.First, reportedPing);

        Assert.InRange(group.GetHighestPing(), 0, group.MaxPing);

        // The reported ping is scaled into the group's resume point, so an unclamped value either
        // pushes playback months out or overflows the arithmetic outright.
        var state = new PlayingGroupState(NullLoggerFactory.Instance);
        var before = DateTime.UtcNow;
        state.HandleRequest(
            new UnpauseGroupRequest(),
            group,
            GroupStateType.Paused,
            harness.First,
            CancellationToken.None);

        Assert.InRange(group.LastActivity - before, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task SessionJoined_JoinerNeverReportsReady_GroupResumesWithoutIt()
    {
        var harness = new GroupHarness(groupWaitTimeout: 200);
        var group = harness.Group;

        group.PositionTicks = TimeSpan.FromMinutes(5).Ticks;
        group.LastActivity = DateTime.UtcNow;
        group.SetState(new PlayingGroupState(NullLoggerFactory.Instance));

        // A session joins while the group is playing: the group pauses and waits for it.
        var joiner = harness.NewSession("joiner");
        group.SessionJoin(joiner, new JoinGroupRequest(group.GroupId), CancellationToken.None);

        Assert.Equal(GroupStateType.Waiting, group.GetInfo().State);

        // The joiner's player aborts and never reports ready. Without a bounded wait the whole
        // group stays paused forever.
        await harness.WaitForState(GroupStateType.Playing);

        // Late buffer reports from the session that missed the deadline must not drag the group
        // back into waiting.
        group.HandleRequest(
            joiner,
            new BufferGroupRequest(DateTime.UtcNow, 0, false, harness.PlaylistItemId),
            CancellationToken.None);

        Assert.Equal(GroupStateType.Playing, group.GetInfo().State);
    }

    [Fact]
    public async Task SessionJoined_GroupWasPaused_TimeoutLeavesTheGroupPaused()
    {
        var harness = new GroupHarness(groupWaitTimeout: 200);
        var group = harness.Group;

        group.PositionTicks = TimeSpan.FromMinutes(5).Ticks;

        // The group has been sitting paused for a while before anyone joins.
        group.LastActivity = DateTime.UtcNow.AddMinutes(-2);
        group.SetState(new PausedGroupState(NullLoggerFactory.Instance));

        var joiner = harness.NewSession("joiner");
        group.SessionJoin(joiner, new JoinGroupRequest(group.GroupId), CancellationToken.None);

        Assert.Equal(GroupStateType.Waiting, group.GetInfo().State);

        // A group that was paused must not start playing because a member failed to report ready.
        await harness.WaitForState(GroupStateType.Paused);

        // Giving up on the joiner must not move the playback position of an already paused group.
        Assert.Equal(TimeSpan.FromMinutes(5).Ticks, group.PositionTicks);

        // Every member has to be told the group is no longer waiting.
        var recipients = harness.StateUpdates
            .Where(update => update.Update.State == GroupStateType.Paused)
            .Select(update => update.SessionId)
            .ToList();
        Assert.Contains(harness.First.Id, recipients);
        Assert.Contains(harness.Second.Id, recipients);
        Assert.Contains(joiner.Id, recipients);
    }

    [Fact]
    public async Task Ready_ReportedBeforeTheDeadline_GroupDoesNotGiveUpOnAnyone()
    {
        var harness = new GroupHarness(groupWaitTimeout: 200);
        var group = harness.Group;

        group.PositionTicks = TimeSpan.FromMinutes(5).Ticks;
        group.LastActivity = DateTime.UtcNow;
        group.SetState(new PlayingGroupState(NullLoggerFactory.Instance));

        var joiner = harness.NewSession("joiner");
        group.SessionJoin(joiner, new JoinGroupRequest(group.GroupId), CancellationToken.None);
        Assert.Equal(GroupStateType.Waiting, group.GetInfo().State);

        group.HandleRequest(
            joiner,
            new ReadyGroupRequest(DateTime.UtcNow, group.PositionTicks, true, harness.PlaylistItemId),
            CancellationToken.None);

        // Everyone reported ready, so no deadline is left to trip and force a spurious unpause.
        Assert.Equal(GroupStateType.Playing, group.GetInfo().State);
        Assert.Null(group.GroupWaitDeadline);

        var until = DateTime.UtcNow.AddMilliseconds(3 * 200);
        while (DateTime.UtcNow < until)
        {
            harness.PumpGroupWaitTimeout();
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        Assert.Equal(GroupStateType.Playing, group.GetInfo().State);
    }

    [Fact]
    public async Task SetPlaylistItem_AfterATimeout_GroupWaitsForEveryoneAgain()
    {
        var harness = new GroupHarness(groupWaitTimeout: 200);
        var group = harness.Group;

        group.LastActivity = DateTime.UtcNow;
        group.SetState(new PlayingGroupState(NullLoggerFactory.Instance));

        var joiner = harness.NewSession("joiner");
        group.SessionJoin(joiner, new JoinGroupRequest(group.GroupId), CancellationToken.None);
        await harness.WaitForState(GroupStateType.Playing);

        // Giving up on a session lasts only until the group changes what it is playing.
        group.HandleRequest(
            harness.First,
            new SetPlaylistItemGroupRequest(harness.PlaylistItemId),
            CancellationToken.None);

        Assert.Equal(GroupStateType.Waiting, group.GetInfo().State);
        Assert.NotNull(group.GroupWaitDeadline);
    }

    [Fact]
    public void Play_AMemberCannotSeeTheQueue_TheSessionThatAskedIsTold()
    {
        var harness = new GroupHarness();
        var group = harness.Group;

        // A member whose libraries do not cover the item. Nothing stops them joining: the group
        // was idle, so there was no queue to check them against when they did.
        var restricted = harness.NewSessionForAnotherUser("restricted");
        group.SessionJoin(restricted, new JoinGroupRequest(group.GroupId), CancellationToken.None);
        harness.UsersWithoutAccess.Add(restricted.UserId);

        group.SetState(new IdleGroupState(NullLoggerFactory.Instance));
        harness.GroupUpdates.Clear();

        group.HandleRequest(
            harness.First,
            new PlayGroupRequest(new[] { Guid.NewGuid() }, 0, 0),
            CancellationToken.None);

        // The queue is refused for the whole group, so the member that pressed play has to be
        // told why instead of waiting for a playback that is never going to start.
        Assert.Contains(
            harness.GroupUpdates,
            update => update.SessionId == harness.First.Id && update.Type == GroupUpdateType.LibraryAccessDenied);

        // Only that member: the refusal is about their request, not about the group.
        Assert.DoesNotContain(harness.GroupUpdates, update => update.SessionId == restricted.Id);
        Assert.Equal(GroupStateType.Idle, group.GetInfo().State);
    }

    [Fact]
    public void Play_MalformedQueue_IsNotReportedAsALibraryAccessProblem()
    {
        var harness = new GroupHarness();
        var group = harness.Group;

        group.SetState(new IdleGroupState(NullLoggerFactory.Instance));
        harness.GroupUpdates.Clear();

        // Everybody can see the item, the request itself is nonsense.
        group.HandleRequest(
            harness.First,
            new PlayGroupRequest(new[] { Guid.NewGuid() }, 5, 0),
            CancellationToken.None);

        Assert.DoesNotContain(harness.GroupUpdates, update => update.Type == GroupUpdateType.LibraryAccessDenied);
    }

    [Fact]
    public void Queue_AMemberCannotSeeTheItems_TheSessionThatAskedIsTold()
    {
        var harness = new GroupHarness();
        var group = harness.Group;

        var restricted = harness.NewSessionForAnotherUser("restricted");
        group.SessionJoin(restricted, new JoinGroupRequest(group.GroupId), CancellationToken.None);
        harness.UsersWithoutAccess.Add(restricted.UserId);

        harness.GroupUpdates.Clear();

        group.HandleRequest(
            harness.First,
            new QueueGroupRequest(new[] { Guid.NewGuid() }, GroupQueueMode.Queue),
            CancellationToken.None);

        Assert.Contains(
            harness.GroupUpdates,
            update => update.SessionId == harness.First.Id && update.Type == GroupUpdateType.LibraryAccessDenied);
    }

    private sealed class GroupHarness
    {
        private readonly Dictionary<Guid, User> _users = new();
        private readonly ISessionManager _sessionManager;
        private readonly Guid _userId;

        public GroupHarness(long? groupWaitTimeout = null)
        {
            var userManager = new Mock<IUserManager>();
            var sessionManager = new Mock<ISessionManager>();
            var libraryManager = new Mock<ILibraryManager>();

            var user = new User("tester", "auth-provider", "pwdreset-provider");
            _users[user.Id] = user;
            userManager.Setup(m => m.GetUserById(It.IsAny<Guid>())).Returns<Guid>(id => _users[id]);

            var item = new Mock<BaseItem>();
            item.Setup(i => i.IsVisibleStandalone(It.IsAny<User>()))
                .Returns<User>(u => !UsersWithoutAccess.Contains(u.Id));
            item.Object.RunTimeTicks = TimeSpan.FromHours(2).Ticks;
            libraryManager.Setup(m => m.GetItemById(It.IsAny<Guid>())).Returns(item.Object);

            sessionManager
                .Setup(m => m.SendSyncPlayCommand(It.IsAny<string>(), It.IsAny<SendCommand>(), It.IsAny<CancellationToken>()))
                .Callback<string, SendCommand, CancellationToken>((_, command, _) => Commands.Add(command))
                .Returns(Task.CompletedTask);

            sessionManager
                .Setup(m => m.SendSyncPlayGroupUpdate(It.IsAny<string>(), It.IsAny<GroupUpdate<GroupStateUpdate>>(), It.IsAny<CancellationToken>()))
                .Callback((string sessionId, GroupUpdate<GroupStateUpdate> update, CancellationToken _) => StateUpdates.Add((sessionId, update.Data)))
                .Returns(Task.CompletedTask);

            sessionManager
                .Setup(m => m.SendSyncPlayGroupUpdate(It.IsAny<string>(), It.IsAny<GroupUpdate<string>>(), It.IsAny<CancellationToken>()))
                .Callback((string sessionId, GroupUpdate<string> update, CancellationToken _) => GroupUpdates.Add((sessionId, update.Type)))
                .Returns(Task.CompletedTask);

            Group = new SyncPlayGroup(
                NullLoggerFactory.Instance,
                userManager.Object,
                sessionManager.Object,
                libraryManager.Object)
            {
                GroupWaitTimeout = groupWaitTimeout ?? SyncPlayGroup.DefaultGroupWaitTimeout
            };

            _sessionManager = sessionManager.Object;
            _userId = user.Id;

            First = NewSession("first");
            Second = NewSession("second");

            Group.CreateGroup(First, new NewGroupRequest("group"), CancellationToken.None);
            Group.SessionJoin(Second, new JoinGroupRequest(Group.GroupId), CancellationToken.None);
            Group.SetPlayQueue(new List<Guid> { Guid.NewGuid() }, 0, 0);
            PlaylistItemId = Group.PlayQueue.GetPlayingItemPlaylistId();
        }

        public SyncPlayGroup Group { get; }

        public List<(string SessionId, GroupStateUpdate Update)> StateUpdates { get; } = new();

        public List<(string SessionId, GroupUpdateType Type)> GroupUpdates { get; } = new();

        // Users listed here cannot see any item, as far as the mocked library is concerned.
        public HashSet<Guid> UsersWithoutAccess { get; } = new();

        public SessionInfo First { get; }

        public SessionInfo Second { get; }

        public Guid PlaylistItemId { get; }

        public List<SendCommand> Commands { get; } = new List<SendCommand>();

        // Mirrors the sweep SyncPlayManager runs on a timer.
        public void PumpGroupWaitTimeout()
        {
            var group = Group;

            // Group lock required as Group is not thread-safe.
            lock (group)
            {
                group.HandleGroupWaitTimeout(CancellationToken.None);
            }
        }

        public async Task WaitForState(GroupStateType expected)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (Group.GetInfo().State != expected && DateTime.UtcNow < deadline)
            {
                PumpGroupWaitTimeout();
                await Task.Delay(20, TestContext.Current.CancellationToken);
            }

            Assert.Equal(expected, Group.GetInfo().State);
        }

        public SessionInfo NewSession(string id)
        {
            return new SessionInfo(_sessionManager, NullLogger.Instance)
            {
                Id = id,
                UserId = _userId,
                UserName = id
            };
        }

        // A session belonging to somebody other than the user every other session shares.
        public SessionInfo NewSessionForAnotherUser(string id)
        {
            var user = new User(id, "auth-provider", "pwdreset-provider");
            _users[user.Id] = user;

            return new SessionInfo(_sessionManager, NullLogger.Instance)
            {
                Id = id,
                UserId = user.Id,
                UserName = id
            };
        }
    }
}
