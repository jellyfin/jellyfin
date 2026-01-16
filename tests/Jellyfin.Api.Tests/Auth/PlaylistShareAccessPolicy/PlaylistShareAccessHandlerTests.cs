using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Api.Auth.PlaylistShareAccessPolicy;
using MediaBrowser.Controller.Playlists;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Auth.PlaylistShareAccessPolicy;

public class PlaylistShareAccessHandlerTests
{
    private readonly PlaylistShareAccessHandler _handler;
    private readonly Mock<IPlaylistManager> _mockPlaylistManager;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<HttpRequest> _mockRequest;
    private readonly Mock<IQueryCollection> _mockQuery;
    private readonly Mock<RouteData> _mockRouteData;

    public PlaylistShareAccessHandlerTests()
    {
        _mockPlaylistManager = new Mock<IPlaylistManager>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockRequest = new Mock<HttpRequest>();
        _mockQuery = new Mock<IQueryCollection>();
        _mockRouteData = new Mock<RouteData>();

        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(c => c.Request).Returns(_mockRequest.Object);
        _mockRequest.Setup(r => r.Query).Returns(_mockQuery.Object);
        _mockHttpContext.Setup(c => c.GetRouteData()).Returns(_mockRouteData.Object);
        _mockHttpContext.Setup(c => c.Items).Returns(new Dictionary<object, object?>());

        _handler = new PlaylistShareAccessHandler(
            _mockPlaylistManager.Object,
            _mockHttpContextAccessor.Object);
    }

    [Fact]
    public async Task HandleRequirementAsync_NoToken_Fails()
    {
        // Arrange
        _mockRouteData.Setup(r => r.Values).Returns(new RouteValueDictionary());
        _mockQuery.Setup(q => q["shareToken"]).Returns(Microsoft.Extensions.Primitives.StringValues.Empty);

        var context = new AuthorizationHandlerContext(
            new[] { new PlaylistShareAccessRequirement() },
            null!,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_InvalidToken_Fails()
    {
        // Arrange
        var shareToken = "invalid-token";
        var routeValues = new RouteValueDictionary { ["shareToken"] = shareToken };
        _mockRouteData.Setup(r => r.Values).Returns(routeValues);

        _mockPlaylistManager
            .Setup(m => m.GetPlaylistByShareToken(shareToken))
            .Returns((Playlist?)null);

        var context = new AuthorizationHandlerContext(
            new[] { new PlaylistShareAccessRequirement() },
            null!,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_ValidTokenFromRoute_Succeeds()
    {
        // Arrange
        var shareToken = "valid-token";
        var playlistId = Guid.NewGuid();
        var playlist = new Playlist
        {
            Id = playlistId,
            ShareToken = shareToken
        };

        var routeValues = new RouteValueDictionary { ["shareToken"] = shareToken };
        _mockRouteData.Setup(r => r.Values).Returns(routeValues);

        _mockPlaylistManager
            .Setup(m => m.GetPlaylistByShareToken(shareToken))
            .Returns(playlist);

        var context = new AuthorizationHandlerContext(
            new[] { new PlaylistShareAccessRequirement() },
            null!,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
        _mockHttpContext.Verify(c => c.Items, Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleRequirementAsync_ValidTokenFromQuery_Succeeds()
    {
        // Arrange
        var shareToken = "valid-token";
        var playlistId = Guid.NewGuid();
        var playlist = new Playlist
        {
            Id = playlistId,
            ShareToken = shareToken
        };

        _mockRouteData.Setup(r => r.Values).Returns(new RouteValueDictionary());
        _mockQuery.Setup(q => q["shareToken"]).Returns(new Microsoft.Extensions.Primitives.StringValues(shareToken));

        _mockPlaylistManager
            .Setup(m => m.GetPlaylistByShareToken(shareToken))
            .Returns(playlist);

        var context = new AuthorizationHandlerContext(
            new[] { new PlaylistShareAccessRequirement() },
            null!,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_HttpContextNull_Fails()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var context = new AuthorizationHandlerContext(
            new[] { new PlaylistShareAccessRequirement() },
            null!,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }
}
