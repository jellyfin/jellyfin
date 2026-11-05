using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Emby.Server.Implementations.SyncPlay;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay.Requests;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SyncPlay;

public class GroupTests
{
    public GroupTests()
    {
        var mockLogger = new Mock<ILogger<Emby.Server.Implementations.SyncPlay.Group>>();
        MockLoggerFactory = new Mock<ILoggerFactory>();
        MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);

        MockUserManager = new Mock<IUserManager>();
        MockSessionManager = new Mock<ISessionManager>();
        MockLibraryManager = new Mock<ILibraryManager>();
        MockItem = new Mock<BaseItem>();
        MockItem.Setup(i => i.IsVisibleStandalone(It.IsAny<User>())).Returns(true);
    }

    private Mock<ILoggerFactory> MockLoggerFactory { get; }

    private Mock<IUserManager> MockUserManager { get; }

    private Mock<ISessionManager> MockSessionManager { get; }

    private Mock<ILibraryManager> MockLibraryManager { get; }

    private Mock<BaseItem> MockItem { get; }

    [Fact]
    public void HasAccessToPlayQueue_ReturnsTrue_WhenItemsAreVisible()
    {
        MockLibraryManager.Setup(m => m.GetItemById(It.IsAny<Guid>())).Returns(MockItem.Object);

        var group = new Emby.Server.Implementations.SyncPlay.Group(MockLoggerFactory.Object, MockUserManager.Object, MockSessionManager.Object, MockLibraryManager.Object);
        var itemId = Guid.NewGuid();
        var playlist = new List<Guid> { itemId };
        group.PlayQueue.Reset();
        group.PlayQueue.SetPlaylist(playlist);

        Assert.Single(group.PlayQueue.GetPlaylist());
        Assert.Equal(itemId, group.PlayQueue.GetPlaylist()[0].ItemId);

        var user = new User("test-user", "auth-provider", "pwdreset-provider");
        var result = group.HasAccessToPlayQueue(user);

        Assert.True(result);
    }

    [Fact]
    public void HasAccessToPlayQueue_ReturnsFalse_WhenLibraryReturnsNullForItem()
    {
        MockLibraryManager.Setup(m => m.GetItemById(It.IsAny<Guid>())).Returns((BaseItem?)null);

        Assert.Null(MockLibraryManager.Object.GetItemById(Guid.NewGuid()));

        var group = new Emby.Server.Implementations.SyncPlay.Group(MockLoggerFactory.Object, MockUserManager.Object, MockSessionManager.Object, MockLibraryManager.Object);
        var itemId = Guid.NewGuid();
        var playlist = new List<Guid> { itemId };
        group.PlayQueue.Reset();
        group.PlayQueue.SetPlaylist(playlist);

        Assert.Single(group.PlayQueue.GetPlaylist());
        Assert.Equal(itemId, group.PlayQueue.GetPlaylist()[0].ItemId);

        var user = new User("test-user", "auth-provider", "pwdreset-provider");
        var result = group.HasAccessToPlayQueue(user);

        Assert.False(result);
    }

    [Fact]
    public void CreateGroup_SessionAlreadyPlaying_StartsFromReportedQueue()
    {
        var queue = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var session = CreatePlayingSession(queue[1], queue);

        var group = CreateGroup(session);

        Assert.Equal(queue, group.PlayQueue.GetPlaylist().Select(item => item.ItemId));
        Assert.Equal(1, group.PlayQueue.PlayingItemIndex);
        Assert.Equal(session.PlayState.PositionTicks, group.PositionTicks);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void CreateGroup_ReportedQueueLacksPlayingItem_StartsFromPlayingItem(int queueLength)
    {
        var playingItemId = Guid.NewGuid();
        var queue = Enumerable.Range(0, queueLength).Select(_ => Guid.NewGuid()).ToArray();
        var session = CreatePlayingSession(playingItemId, queue);

        var group = CreateGroup(session);

        Assert.Equal(playingItemId, Assert.Single(group.PlayQueue.GetPlaylist()).ItemId);
        Assert.Equal(0, group.PlayQueue.PlayingItemIndex);
    }

    private Emby.Server.Implementations.SyncPlay.Group CreateGroup(SessionInfo session)
    {
        var group = new Emby.Server.Implementations.SyncPlay.Group(MockLoggerFactory.Object, MockUserManager.Object, MockSessionManager.Object, MockLibraryManager.Object);
        group.CreateGroup(session, new NewGroupRequest("group"), CancellationToken.None);
        return group;
    }

    private SessionInfo CreatePlayingSession(Guid playingItemId, Guid[] queue)
    {
        var playingItem = new Mock<BaseItem>().Object;
        playingItem.Id = playingItemId;

        var session = new SessionInfo(MockSessionManager.Object, NullLogger.Instance)
        {
            Id = "session",
            FullNowPlayingItem = playingItem,
            NowPlayingQueue = queue.Select(id => new QueueItem { Id = id }).ToArray()
        };
        session.PlayState.PositionTicks = TimeSpan.FromMinutes(10).Ticks;

        return session;
    }
}
