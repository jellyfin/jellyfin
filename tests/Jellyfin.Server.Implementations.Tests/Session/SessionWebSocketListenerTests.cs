using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Session;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Net.WebSocketMessages;
using MediaBrowser.Controller.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Session;

public class SessionWebSocketListenerTests
{
    [Fact]
    public async Task ProcessWebSocketConnectedAsync_KeepAliveSendThrowsObjectDisposed_DoesNotThrow()
    {
        // A disposed socket (abruptly disconnected client) makes SendAsync throw ObjectDisposedException.
        // The keep-alive send runs in an async void timer handler, so an unhandled exception crashes the
        // whole server (#15709, #14837). Sending the initial ForceKeepAlive must swallow it instead.
        var sessionManager = new Mock<ISessionManager>();
        var userManager = new Mock<IUserManager>();
        var loggerFactory = NullLoggerFactory.Instance;

        var session = new SessionInfo(sessionManager.Object, NullLogger.Instance);
        sessionManager
            .Setup(s => s.LogSessionActivity(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Jellyfin.Database.Implementations.Entities.User>()))
            .ReturnsAsync(session);

        var connection = new Mock<IWebSocketConnection>();
        connection
            .Setup(c => c.SendAsync(It.IsAny<OutboundWebSocketMessage<int>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ObjectDisposedException("socket"));

        using var listener = new SessionWebSocketListener(
            new NullLogger<SessionWebSocketListener>(),
            sessionManager.Object,
            userManager.Object,
            loggerFactory);

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

        var exception = await Record.ExceptionAsync(
            () => listener.ProcessWebSocketConnectedAsync(connection.Object, httpContext));

        Assert.Null(exception);
        connection.Verify(
            c => c.SendAsync(It.IsAny<OutboundWebSocketMessage<int>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
