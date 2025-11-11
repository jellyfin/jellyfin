using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Server.Implementations.SyncPlay;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Emby.Server.Implementations.Tests.SyncPlay
{
    public class GroupSetPlayQueueTests
    {
        [Fact]
        public void SetPlayQueue_ShouldExpandFolderIntoPlayableChildren()
        {
            // Arrange
            var loggerFactory = NullLoggerFactory.Instance;
            var userManagerMock = new Mock<IUserManager>();
            var sessionManagerMock = new Mock<ISessionManager>();
            var libraryManagerMock = new Mock<ILibraryManager>();

            var fileId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var childId = Guid.NewGuid();

            var fileVideo = new Video { Id = fileId, RunTimeTicks = 1_000_000L };
            var childVideo = new Video { Id = childId, RunTimeTicks = 1_500_000L };
            var folder = new Folder { Id = folderId };
            folder.Children = new List<BaseItem> { childVideo };

            libraryManagerMock.Setup(x => x.GetItemById(fileId)).Returns(fileVideo);
            libraryManagerMock.Setup(x => x.GetItemById(folderId)).Returns(folder);

            var group = new Group(
                loggerFactory,
                userManagerMock.Object,
                sessionManagerMock.Object,
                libraryManagerMock.Object);

            // Act
            var incomingQueue = new List<Guid> { fileId, folderId };
            var result = group.SetPlayQueue(incomingQueue, 0, startPositionTicks: 0);

            // Assert
            Assert.True(result, "Expected SetPlayQueue to accept the queue");

            // After normalization we expect the PlayQueue playlist item IDs to be [fileId, childId]
            var playlistItemIds = group.PlayQueue.GetPlaylist().Select(i => i.ItemId).ToList();
            Assert.Equal(2, playlistItemIds.Count);
            Assert.Equal(fileId, playlistItemIds[0]);
            Assert.Equal(childId, playlistItemIds[1]);

            // The playing item index should still correspond to the first playable item (0)
            Assert.Equal(0, group.PlayQueue.PlayingItemIndex);
            Assert.Equal(fileId, group.PlayQueue.GetPlayingItemId());
        }
    }
}
