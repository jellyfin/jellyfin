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

namespace Emby.Server.Implementations.Tests.SyncPlay;

public class GroupSetPlayQueueTests
{
    /// <summary>
    /// Test: SetPlayQueue should accept a valid queue with regular items.
    /// </summary>
    [Fact]
    public void SetPlayQueue_WithValidQueue_ReturnsTrue()
    {
        // Arrange
        var loggerFactory = NullLoggerFactory.Instance;
        var userManagerMock = new Mock<IUserManager>();
        var sessionManagerMock = new Mock<ISessionManager>();
        var libraryManagerMock = new Mock<ILibraryManager>();

        var itemId1 = Guid.NewGuid();
        var itemId2 = Guid.NewGuid();

        var video1 = new Video { Id = itemId1, RunTimeTicks = 1_000_000L };
        var video2 = new Video { Id = itemId2, RunTimeTicks = 2_000_000L };

        libraryManagerMock.Setup(x => x.GetItemById(itemId1)).Returns(video1);
        libraryManagerMock.Setup(x => x.GetItemById(itemId2)).Returns(video2);

        var group = new Group(
            loggerFactory,
            userManagerMock.Object,
            sessionManagerMock.Object,
            libraryManagerMock.Object);

        // Act
        var queue = new List<Guid> { itemId1, itemId2 };
        var result = group.SetPlayQueue(queue, 0, startPositionTicks: 0);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Test: SetPlayQueue should set the playlist correctly with regular items.
    /// </summary>
    [Fact]
    public void SetPlayQueue_WithValidQueue_SetsPlaylistCorrectly()
    {
        // Arrange
        var loggerFactory = NullLoggerFactory.Instance;
        var userManagerMock = new Mock<IUserManager>();
        var sessionManagerMock = new Mock<ISessionManager>();
        var libraryManagerMock = new Mock<ILibraryManager>();

        var itemId1 = Guid.NewGuid();
        var itemId2 = Guid.NewGuid();
        var itemId3 = Guid.NewGuid();

        var video1 = new Video { Id = itemId1, RunTimeTicks = 1_000_000L };
        var video2 = new Video { Id = itemId2, RunTimeTicks = 2_000_000L };
        var video3 = new Video { Id = itemId3, RunTimeTicks = 3_000_000L };

        libraryManagerMock.Setup(x => x.GetItemById(itemId1)).Returns(video1);
        libraryManagerMock.Setup(x => x.GetItemById(itemId2)).Returns(video2);
        libraryManagerMock.Setup(x => x.GetItemById(itemId3)).Returns(video3);

        var group = new Group(
            loggerFactory,
            userManagerMock.Object,
            sessionManagerMock.Object,
            libraryManagerMock.Object);

        // Act
        var queue = new List<Guid> { itemId1, itemId2, itemId3 };
        var result = group.SetPlayQueue(queue, 0, startPositionTicks: 0);

        // Assert
        Assert.True(result);
        var playlistItemIds = group.PlayQueue.GetPlaylist().Select(i => i.ItemId).ToList();
        Assert.Equal(3, playlistItemIds.Count);
        Assert.Equal(itemId1, playlistItemIds[0]);
        Assert.Equal(itemId2, playlistItemIds[1]);
        Assert.Equal(itemId3, playlistItemIds[2]);
    }

    /// <summary>
    /// Test: SetPlayQueue should set the playing item index to the specified position.
    /// </summary>
    [Fact]
    public void SetPlayQueue_WithPlayingItemPosition_SetsCorrectPlayingIndex()
    {
        // Arrange
        var loggerFactory = NullLoggerFactory.Instance;
        var userManagerMock = new Mock<IUserManager>();
        var sessionManagerMock = new Mock<ISessionManager>();
        var libraryManagerMock = new Mock<ILibraryManager>();

        var itemId1 = Guid.NewGuid();
        var itemId2 = Guid.NewGuid();
        var itemId3 = Guid.NewGuid();

        var video1 = new Video { Id = itemId1, RunTimeTicks = 1_000_000L };
        var video2 = new Video { Id = itemId2, RunTimeTicks = 2_000_000L };
        var video3 = new Video { Id = itemId3, RunTimeTicks = 3_000_000L };

        libraryManagerMock.Setup(x => x.GetItemById(itemId1)).Returns(video1);
        libraryManagerMock.Setup(x => x.GetItemById(itemId2)).Returns(video2);
        libraryManagerMock.Setup(x => x.GetItemById(itemId3)).Returns(video3);

        var group = new Group(
            loggerFactory,
            userManagerMock.Object,
            sessionManagerMock.Object,
            libraryManagerMock.Object);

        // Act - Set playing position to second item
        var queue = new List<Guid> { itemId1, itemId2, itemId3 };
        var result = group.SetPlayQueue(queue, 1, startPositionTicks: 500_000);

        // Assert
        Assert.True(result);
        Assert.Equal(1, group.PlayQueue.PlayingItemIndex);
        Assert.Equal(itemId2, group.PlayQueue.GetPlayingItemId());
        Assert.Equal(2_000_000L, group.RunTimeTicks);
        Assert.Equal(500_000L, group.PositionTicks);
    }

    /// <summary>
    /// Test: SetPlayQueue should reject empty queue.
    /// </summary>
    [Fact]
    public void SetPlayQueue_WithEmptyQueue_ReturnsFalse()
    {
        // Arrange
        var loggerFactory = NullLoggerFactory.Instance;
        var userManagerMock = new Mock<IUserManager>();
        var sessionManagerMock = new Mock<ISessionManager>();
        var libraryManagerMock = new Mock<ILibraryManager>();

        var group = new Group(
            loggerFactory,
            userManagerMock.Object,
            sessionManagerMock.Object,
            libraryManagerMock.Object);

        // Act
        var queue = new List<Guid>();
        var result = group.SetPlayQueue(queue, 0, startPositionTicks: 0);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Test: SetPlayQueue should reject invalid playing item position (negative).
    /// </summary>
    [Fact]
    public void SetPlayQueue_WithNegativePlayingPosition_ReturnsFalse()
    {
        // Arrange
        var loggerFactory = NullLoggerFactory.Instance;
        var userManagerMock = new Mock<IUserManager>();
        var sessionManagerMock = new Mock<ISessionManager>();
        var libraryManagerMock = new Mock<ILibraryManager>();

        var itemId1 = Guid.NewGuid();
        var video1 = new Video { Id = itemId1, RunTimeTicks = 1_000_000L };

        libraryManagerMock.Setup(x => x.GetItemById(itemId1)).Returns(video1);

        var group = new Group(
            loggerFactory,
            userManagerMock.Object,
            sessionManagerMock.Object,
            libraryManagerMock.Object);

        // Act
        var queue = new List<Guid> { itemId1 };
        var result = group.SetPlayQueue(queue, -1, startPositionTicks: 0);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Test: SetPlayQueue should reject invalid playing item position (out of bounds).
    /// </summary>
    [Fact]
    public void SetPlayQueue_WithOutOfBoundsPlayingPosition_ReturnsFalse()
    {
        // Arrange
        var loggerFactory = NullLoggerFactory.Instance;
        var userManagerMock = new Mock<IUserManager>();
        var sessionManagerMock = new Mock<ISessionManager>();
        var libraryManagerMock = new Mock<ILibraryManager>();

        var itemId1 = Guid.NewGuid();
        var itemId2 = Guid.NewGuid();

        var video1 = new Video { Id = itemId1, RunTimeTicks = 1_000_000L };
        var video2 = new Video { Id = itemId2, RunTimeTicks = 2_000_000L };

        libraryManagerMock.Setup(x => x.GetItemById(itemId1)).Returns(video1);
        libraryManagerMock.Setup(x => x.GetItemById(itemId2)).Returns(video2);

        var group = new Group(
            loggerFactory,
            userManagerMock.Object,
            sessionManagerMock.Object,
            libraryManagerMock.Object);

        // Act
        var queue = new List<Guid> { itemId1, itemId2 };
        var result = group.SetPlayQueue(queue, 2, startPositionTicks: 0); // Position 2 is out of bounds for 2-item queue

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Test: SetPlayQueue should set RunTimeTicks based on the playing item.
    /// </summary>
    [Fact]
    public void SetPlayQueue_SetsRunTimeTicksFromPlayingItem()
    {
        // Arrange
        var loggerFactory = NullLoggerFactory.Instance;
        var userManagerMock = new Mock<IUserManager>();
        var sessionManagerMock = new Mock<ISessionManager>();
        var libraryManagerMock = new Mock<ILibraryManager>();

        var itemId1 = Guid.NewGuid();
        var itemId2 = Guid.NewGuid();

        var video1 = new Video { Id = itemId1, RunTimeTicks = 1_000_000L };
        var video2 = new Video { Id = itemId2, RunTimeTicks = 5_000_000L };

        libraryManagerMock.Setup(x => x.GetItemById(itemId1)).Returns(video1);
        libraryManagerMock.Setup(x => x.GetItemById(itemId2)).Returns(video2);

        var group = new Group(
            loggerFactory,
            userManagerMock.Object,
            sessionManagerMock.Object,
            libraryManagerMock.Object);

        // Act - Set playing position to second item
        var queue = new List<Guid> { itemId1, itemId2 };
        var result = group.SetPlayQueue(queue, 1, startPositionTicks: 0);

        // Assert
        Assert.True(result);
        Assert.Equal(5_000_000L, group.RunTimeTicks); // Should be from video2
    }

    /// <summary>
    /// Test: SetPlayQueue should set PositionTicks from startPositionTicks parameter.
    /// </summary>
    [Fact]
    public void SetPlayQueue_SetsPositionTicksFromParameter()
    {
        // Arrange
        var loggerFactory = NullLoggerFactory.Instance;
        var userManagerMock = new Mock<IUserManager>();
        var sessionManagerMock = new Mock<ISessionManager>();
        var libraryManagerMock = new Mock<ILibraryManager>();

        var itemId1 = Guid.NewGuid();
        var video1 = new Video { Id = itemId1, RunTimeTicks = 10_000_000L };

        libraryManagerMock.Setup(x => x.GetItemById(itemId1)).Returns(video1);

        var group = new Group(
            loggerFactory,
            userManagerMock.Object,
            sessionManagerMock.Object,
            libraryManagerMock.Object);

        // Act
        var queue = new List<Guid> { itemId1 };
        var startPosition = 3_750_000L;
        var result = group.SetPlayQueue(queue, 0, startPositionTicks: startPosition);

        // Assert
        Assert.True(result);
        Assert.Equal(startPosition, group.PositionTicks);
    }

    /// <summary>
    /// Test: SetPlayQueue with a folder as the playing item should map the playing index correctly.
    /// </summary>
    [Fact]
    public void SetPlayQueue_WithFolderAsPlayingItem_MapsPlayingIndexCorrectly()
    {
        // Arrange
        var loggerFactory = NullLoggerFactory.Instance;
        var userManagerMock = new Mock<IUserManager>();
        var sessionManagerMock = new Mock<ISessionManager>();
        var libraryManagerMock = new Mock<ILibraryManager>();

        var videoId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var childId1 = Guid.NewGuid();
        var childId2 = Guid.NewGuid();

        var video = new Video { Id = videoId, RunTimeTicks = 1_000_000L };
        var childVideo1 = new Video { Id = childId1, RunTimeTicks = 2_000_000L };
        var childVideo2 = new Video { Id = childId2, RunTimeTicks = 3_000_000L };
        var folder = new Folder { Id = folderId };
        folder.Children = new List<BaseItem> { childVideo1, childVideo2 };

        libraryManagerMock.Setup(x => x.GetItemById(videoId)).Returns(video);
        libraryManagerMock.Setup(x => x.GetItemById(folderId)).Returns(folder);
        libraryManagerMock.Setup(x => x.GetItemById(childId1)).Returns(childVideo1);

        var group = new Group(
            loggerFactory,
            userManagerMock.Object,
            sessionManagerMock.Object,
            libraryManagerMock.Object);

        // Act - Set playing position to the folder (index 1), should map to first child
        var queue = new List<Guid> { videoId, folderId };
        var result = group.SetPlayQueue(queue, 1, startPositionTicks: 0);

        // Assert
        Assert.True(result);

        // After expansion: [videoId, childId1, childId2]
        // Playing index should map to position 1 (childId1)
        var playlistItemIds = group.PlayQueue.GetPlaylist().Select(i => i.ItemId).ToList();
        Assert.Equal(3, playlistItemIds.Count);
        Assert.Equal(videoId, playlistItemIds[0]);
        Assert.Equal(childId1, playlistItemIds[1]);
        Assert.Equal(childId2, playlistItemIds[2]);
        Assert.Equal(1, group.PlayQueue.PlayingItemIndex);
        Assert.Equal(childId1, group.PlayQueue.GetPlayingItemId());
    }

    /// <summary>
    /// Test: SetPlayQueue should reject queue with only empty folders.
    /// </summary>
    [Fact]
    public void SetPlayQueue_WithOnlyEmptyFolder_ReturnsFalse()
    {
        // Arrange
        var loggerFactory = NullLoggerFactory.Instance;
        var userManagerMock = new Mock<IUserManager>();
        var sessionManagerMock = new Mock<ISessionManager>();
        var libraryManagerMock = new Mock<ILibraryManager>();

        var folderId = Guid.NewGuid();
        var folder = new Folder { Id = folderId };
        folder.Children = new List<BaseItem>(); // Empty folder

        libraryManagerMock.Setup(x => x.GetItemById(folderId)).Returns(folder);

        var group = new Group(
            loggerFactory,
            userManagerMock.Object,
            sessionManagerMock.Object,
            libraryManagerMock.Object);

        // Act
        var queue = new List<Guid> { folderId };
        var result = group.SetPlayQueue(queue, 0, startPositionTicks: 0);

        // Assert
        Assert.False(result); // Should reject because expanded queue is empty
    }

    /// <summary>
    /// Test: SetPlayQueue should expand folder into its playable children.
    /// </summary>
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
