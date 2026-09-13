using System;
using System.Collections.Generic;
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
                .Returns(Task.CompletedTask);

            sessionManager
                .Setup(m => m.SendSyncPlayGroupUpdate(It.IsAny<string>(), It.IsAny<GroupUpdate<GroupStateUpdate>>(), It.IsAny<CancellationToken>()))
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
    }
}
