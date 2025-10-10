using System;
using System.Collections.Generic;
using Emby.Server.Implementations.SyncPlay;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SyncPlay
{
    public class GroupTests
    {
        [Fact]
        public void HasAccessToPlayQueue_ReturnsTrue_WhenItemsAreVisible()
        {
            var mockLogger = new Mock<ILogger<Emby.Server.Implementations.SyncPlay.Group>>();
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);

            var mockUserManager = new Mock<IUserManager>();
            var mockSessionManager = new Mock<ISessionManager>();
            var mockLibraryManager = new Mock<ILibraryManager>();

            var mockItem = new Mock<BaseItem>();
            mockItem.Setup(i => i.IsVisibleStandalone(It.IsAny<User>())).Returns(true);

            mockLibraryManager.Setup(m => m.GetItemById(It.IsAny<Guid>())).Returns(mockItem.Object);

            var group = new Emby.Server.Implementations.SyncPlay.Group(mockLoggerFactory.Object, mockUserManager.Object, mockSessionManager.Object, mockLibraryManager.Object);

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
            var mockLogger = new Mock<ILogger<Emby.Server.Implementations.SyncPlay.Group>>();
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);

            var mockUserManager = new Mock<IUserManager>();
            var mockSessionManager = new Mock<ISessionManager>();
            var mockLibraryManager = new Mock<ILibraryManager>();

            var mockItem = new Mock<BaseItem>();
            mockItem.Setup(i => i.IsVisibleStandalone(It.IsAny<User>())).Returns(true);

            mockLibraryManager.Setup(m => m.GetItemById(It.IsAny<Guid>())).Returns((BaseItem?)null);
            Assert.Null(
                mockLibraryManager.Object.GetItemById(Guid.NewGuid()));
            var group = new Emby.Server.Implementations.SyncPlay.Group(mockLoggerFactory.Object, mockUserManager.Object, mockSessionManager.Object, mockLibraryManager.Object);

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
    }
}
