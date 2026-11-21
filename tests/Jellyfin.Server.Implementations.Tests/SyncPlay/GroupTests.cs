using System;
using System.Collections.Generic;
using System.Threading;
using Emby.Server.Implementations.SyncPlay;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay.Requests;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.SyncPlay;
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
    public void CreateGroup_AttributesInheritedQueueToCreator()
    {
        var itemId = Guid.NewGuid();
        MockItem.Object.Id = itemId;
        var group = new Emby.Server.Implementations.SyncPlay.Group(
            MockLoggerFactory.Object, MockUserManager.Object, MockSessionManager.Object, MockLibraryManager.Object);
        var session = new SessionInfo(MockSessionManager.Object, NullLogger.Instance)
        {
            Id = "creator-session",
            UserId = Guid.NewGuid(),
            UserName = "creator",
            FullNowPlayingItem = MockItem.Object,
            NowPlayingQueue = new[] { new QueueItem { Id = itemId }, new QueueItem { Id = Guid.NewGuid() } }
        };

        group.CreateGroup(session, new NewGroupRequest("group"), CancellationToken.None);

        var playlist = group.GetPlayQueueUpdate(PlayQueueUpdateReason.NewPlaylist).Playlist;
        Assert.Equal(2, playlist.Count);
        Assert.All(playlist, item =>
        {
            Assert.Equal(session.UserId, item.AddedByUserId);
            Assert.Equal(session.UserName, item.AddedByUserName);
        });
    }
}
