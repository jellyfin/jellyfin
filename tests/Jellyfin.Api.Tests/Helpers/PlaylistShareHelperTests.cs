using System;
using System.Linq;
using Jellyfin.Api.Helpers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public class PlaylistShareHelperTests
{
    private readonly Mock<IPlaylistManager> _mockPlaylistManager;
    private readonly Mock<ILibraryManager> _mockLibraryManager;

    public PlaylistShareHelperTests()
    {
        _mockPlaylistManager = new Mock<IPlaylistManager>();
        _mockLibraryManager = new Mock<ILibraryManager>();
    }

    [Fact]
    public void ValidateShareTokenAccess_NoToken_ReturnsNull()
    {
        // Act
        var result = PlaylistShareHelper.ValidateShareTokenAccess(
            _mockPlaylistManager.Object,
            _mockLibraryManager.Object,
            null,
            Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ValidateShareTokenAccess_InvalidToken_ReturnsNotFound()
    {
        // Arrange
        var shareToken = "invalid-token";
        var itemId = Guid.NewGuid();

        _mockPlaylistManager
            .Setup(m => m.GetPlaylistByShareToken(shareToken))
            .Returns((Playlist?)null);

        // Act
        var result = PlaylistShareHelper.ValidateShareTokenAccess(
            _mockPlaylistManager.Object,
            _mockLibraryManager.Object,
            shareToken,
            itemId);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void ValidateShareTokenAccess_ItemNotInPlaylist_ReturnsForbid()
    {
        // Arrange
        var shareToken = "valid-token";
        var playlistId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var otherItemId = Guid.NewGuid();

        var otherItem = new Audio
        {
            Id = otherItemId,
            Name = "Other Audio",
            Path = "/test/other.mp3"
        };

        var playlist = new Playlist
        {
            Id = playlistId,
            ShareToken = shareToken
        };

        var linkedChild = LinkedChild.Create(otherItem);
        linkedChild.ItemId = otherItemId;
        playlist.LinkedChildren = new[] { linkedChild };

        _mockPlaylistManager
            .Setup(m => m.GetPlaylistByShareToken(shareToken))
            .Returns(playlist);

        var item = new Audio
        {
            Id = itemId,
            Name = "Test Audio",
            Path = "/test/audio.mp3"
        };

        _mockLibraryManager
            .Setup(m => m.GetItemById(itemId))
            .Returns(item);

        // Act
        var result = PlaylistShareHelper.ValidateShareTokenAccess(
            _mockPlaylistManager.Object,
            _mockLibraryManager.Object,
            shareToken,
            itemId);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public void ValidateShareTokenAccess_ItemInPlaylist_ReturnsNull()
    {
        // Arrange
        var shareToken = "valid-token";
        var playlistId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var item = new Audio
        {
            Id = itemId,
            Name = "Test Audio",
            Path = "/test/audio.mp3"
        };

        var playlist = new Playlist
        {
            Id = playlistId,
            ShareToken = shareToken
        };

        var linkedChild = LinkedChild.Create(item);
        linkedChild.ItemId = itemId;
        playlist.LinkedChildren = new[] { linkedChild };

        _mockPlaylistManager
            .Setup(m => m.GetPlaylistByShareToken(shareToken))
            .Returns(playlist);

        _mockLibraryManager
            .Setup(m => m.GetItemById(itemId))
            .Returns(item);

        // Act
        var result = PlaylistShareHelper.ValidateShareTokenAccess(
            _mockPlaylistManager.Object,
            _mockLibraryManager.Object,
            shareToken,
            itemId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ValidateShareTokenAccess_ItemNotFound_ReturnsNotFound()
    {
        // Arrange
        var shareToken = "valid-token";
        var playlistId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var playlist = new Playlist
        {
            Id = playlistId,
            ShareToken = shareToken
        };

        _mockPlaylistManager
            .Setup(m => m.GetPlaylistByShareToken(shareToken))
            .Returns(playlist);

        _mockLibraryManager
            .Setup(m => m.GetItemById(itemId))
            .Returns((BaseItem?)null);

        // Act
        var result = PlaylistShareHelper.ValidateShareTokenAccess(
            _mockPlaylistManager.Object,
            _mockLibraryManager.Object,
            shareToken,
            itemId);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
