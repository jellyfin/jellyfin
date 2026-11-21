using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Session;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Net.WebSocketMessages;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SessionManager;

public class WebSocketControllerTests
{
    [Fact]
    public async Task SendMessage_SessionWithSeveralClients_ReachesOnlyTheMostRecentlyActiveOne()
    {
        var harness = new ControllerHarness();

        await harness.Controller.SendMessage(SessionMessageType.SyncPlayCommand, Guid.NewGuid(), "data", CancellationToken.None);

        Assert.Empty(harness.Older);
        Assert.Single(harness.Newer);
    }

    [Fact]
    public async Task SendMessageToAllClients_SessionWithSeveralClients_ReachesEveryOne()
    {
        var harness = new ControllerHarness();

        // Two browser tabs share a device id, and therefore a session. Both are members of
        // whatever SyncPlay group the session joined, so both have to be told about it.
        await harness.Controller.SendMessageToAllClients(SessionMessageType.SyncPlayCommand, Guid.NewGuid(), "data", CancellationToken.None);

        Assert.Single(harness.Older);
        Assert.Single(harness.Newer);
    }

    [Fact]
    public async Task SendMessageToAllClients_ClosedClient_IsSkipped()
    {
        var harness = new ControllerHarness();
        harness.CloseOlder();

        await harness.Controller.SendMessageToAllClients(SessionMessageType.SyncPlayCommand, Guid.NewGuid(), "data", CancellationToken.None);

        Assert.Empty(harness.Older);
        Assert.Single(harness.Newer);
    }

    private sealed class ControllerHarness
    {
        private readonly Mock<IWebSocketConnection> _older;

        public ControllerHarness()
        {
            var sessionManager = new Mock<ISessionManager>();
            var session = new SessionInfo(sessionManager.Object, NullLogger.Instance) { Id = "session" };

            _older = NewConnection(DateTime.UtcNow.AddMinutes(-1), Older);
            var newer = NewConnection(DateTime.UtcNow, Newer);

            Controller = new WebSocketController(NullLogger<WebSocketController>.Instance, session, sessionManager.Object);
            Controller.AddWebSocket(_older.Object);
            Controller.AddWebSocket(newer.Object);
        }

        public WebSocketController Controller { get; }

        public List<object> Older { get; } = new();

        public List<object> Newer { get; } = new();

        public void CloseOlder()
            => _older.SetupGet(c => c.State).Returns(WebSocketState.Closed);

        private static Mock<IWebSocketConnection> NewConnection(DateTime lastActivity, List<object> sink)
        {
            var connection = new Mock<IWebSocketConnection>();
            connection.SetupGet(c => c.State).Returns(WebSocketState.Open);
            connection.SetupGet(c => c.LastActivityDate).Returns(lastActivity);
            connection
                .Setup(c => c.SendAsync(It.IsAny<OutboundWebSocketMessage<string>>(), It.IsAny<CancellationToken>()))
                .Callback<OutboundWebSocketMessage<string>, CancellationToken>((message, _) => sink.Add(message))
                .Returns(Task.CompletedTask);

            return connection;
        }
    }
}
