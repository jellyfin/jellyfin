using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Models.PlaylistDtos;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class PlaylistsControllerShareTests
{
    private readonly PlaylistsController _controller;
    private readonly Mock<IPlaylistManager> _mockPlaylistManager;
    private readonly Mock<IDtoService> _mockDtoService;
    private readonly Mock<IUserManager> _mockUserManager;
    private readonly Mock<ILibraryManager> _mockLibraryManager;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpRequest> _mockRequest;

    public PlaylistsControllerShareTests()
    {
        _mockPlaylistManager = new Mock<IPlaylistManager>();
        _mockDtoService = new Mock<IDtoService>();
        _mockUserManager = new Mock<IUserManager>();
        _mockLibraryManager = new Mock<ILibraryManager>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockRequest = new Mock<HttpRequest>();

        _mockHttpContext.Setup(c => c.Request).Returns(_mockRequest.Object);
        _mockRequest.Setup(r => r.Scheme).Returns("https");
        _mockRequest.Setup(r => r.Host).Returns(new HostString("example.com"));

        _controller = new PlaylistsController(
            _mockDtoService.Object,
            _mockPlaylistManager.Object,
            _mockUserManager.Object,
            _mockLibraryManager.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _mockHttpContext.Object
            }
        };
    }

    [Fact]
    public async Task GenerateShareLink_PlaylistNotFound_ReturnsNotFound()
    {
        // Arrange
        var playlistId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupUser(userId);

        _mockPlaylistManager
            .Setup(m => m.GetPlaylistForUser(playlistId, userId))
            .Returns((Playlist)null!);

        // Act
        var result = await _controller.GenerateShareLink(playlistId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Playlist not found", notFoundResult.Value);
    }

    [Fact]
    public async Task GenerateShareLink_UserNotOwner_ReturnsForbid()
    {
        // Arrange
        var playlistId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var playlist = CreatePlaylist(playlistId, ownerId);
        SetupUser(userId);

        _mockPlaylistManager
            .Setup(m => m.GetPlaylistForUser(playlistId, userId))
            .Returns(playlist);

        // Act
        var result = await _controller.GenerateShareLink(playlistId);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GenerateShareLink_ValidRequest_ReturnsShareLink()
    {
        // Arrange
        var playlistId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var playlist = CreatePlaylist(playlistId, userId);
        var shareToken = "test-token-1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";
        SetupUser(userId);

        _mockPlaylistManager
            .Setup(m => m.GetPlaylistForUser(playlistId, userId))
            .Returns(playlist);

        _mockPlaylistManager
            .Setup(m => m.GenerateShareToken(playlistId, userId))
            .ReturnsAsync(shareToken);

        // Act
        var result = await _controller.GenerateShareLink(playlistId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ShareLinkDto>(okResult.Value);
        Assert.Equal(shareToken, dto.ShareToken);
        Assert.Equal($"https://example.com/Playlists/Share/{shareToken}", dto.ShareLink);
    }

    [Fact]
    public async Task RevokeShareLink_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        var playlistId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var playlist = CreatePlaylist(playlistId, userId);
        SetupUser(userId);

        _mockPlaylistManager
            .Setup(m => m.GetPlaylistForUser(playlistId, userId))
            .Returns(playlist);

        _mockPlaylistManager
            .Setup(m => m.RevokeShareToken(playlistId, userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RevokeShareLink(playlistId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RevokeShareLink_UserNotOwner_ReturnsForbid()
    {
        // Arrange
        var playlistId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var playlist = CreatePlaylist(playlistId, ownerId);
        SetupUser(userId);

        _mockPlaylistManager
            .Setup(m => m.GetPlaylistForUser(playlistId, userId))
            .Returns(playlist);

        // Act
        var result = await _controller.RevokeShareLink(playlistId);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public void GetShareLink_NoToken_ReturnsNotFound()
    {
        // Arrange
        var playlistId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var playlist = CreatePlaylist(playlistId, userId);
        playlist.ShareToken = null;
        SetupUser(userId);

        _mockPlaylistManager
            .Setup(m => m.GetPlaylistForUser(playlistId, userId))
            .Returns(playlist);

        // Act
        var result = _controller.GetShareLink(playlistId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("No share token exists for this playlist", notFoundResult.Value);
    }

    [Fact]
    public void GetShareLink_ValidToken_ReturnsShareLink()
    {
        // Arrange
        var playlistId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var shareToken = "test-token-1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";
        var playlist = CreatePlaylist(playlistId, userId);
        playlist.ShareToken = shareToken;
        SetupUser(userId);

        _mockPlaylistManager
            .Setup(m => m.GetPlaylistForUser(playlistId, userId))
            .Returns(playlist);

        // Act
        var result = _controller.GetShareLink(playlistId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ShareLinkDto>(okResult.Value);
        Assert.Equal(shareToken, dto.ShareToken);
        Assert.Equal($"https://example.com/Playlists/Share/{shareToken}", dto.ShareLink);
    }

    private Playlist CreatePlaylist(Guid playlistId, Guid ownerId)
    {
        return new Playlist
        {
            Id = playlistId,
            OwnerUserId = ownerId,
            Name = "Test Playlist",
            Path = "/test/path"
        };
    }

    private void SetupUser(Guid userId)
    {
        var claims = new[]
        {
            new Claim(InternalClaimTypes.UserId, userId.ToString("N", CultureInfo.InvariantCulture))
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext.HttpContext.User = principal;
    }
}
