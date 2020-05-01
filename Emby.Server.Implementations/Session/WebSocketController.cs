#pragma warning disable CS1591
#pragma warning disable SA1600
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Session
{
    public sealed class WebSocketController : ISessionController, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ISessionManager _sessionManager;
        private readonly SessionInfo _session;

        private readonly List<IWebSocketConnection> _sockets;
        private bool _disposed = false;

        public WebSocketController(
            ILogger<WebSocketController> logger,
            SessionInfo session,
            ISessionManager sessionManager)
        {
            _logger = logger;
            _session = session;
            _sessionManager = sessionManager;
            _sockets = new List<IWebSocketConnection>();
        }

        private bool HasOpenSockets => GetActiveSockets().Any();

        /// <inheritdoc />
        public bool SupportsMediaControl => HasOpenSockets;

        /// <inheritdoc />
        public bool IsSessionActive => HasOpenSockets;

        private IEnumerable<IWebSocketConnection> GetActiveSockets()
            => _sockets.Where(i => i.State == WebSocketState.Open);

        public void AddWebSocket(IWebSocketConnection connection)
        {
            _logger.LogDebug("Adding websocket to session {Session}", _session.Id);
            _sockets.Add(connection);

            connection.Closed += OnConnectionClosed;
        }

        private void OnConnectionClosed(object sender, EventArgs e)
        {
            var connection = (IWebSocketConnection)sender;
            _logger.LogDebug("Removing websocket from session {Session}", _session.Id);
            _sockets.Remove(connection);
            connection.Closed -= OnConnectionClosed;
            _sessionManager.CloseIfNeeded(_session);
        }

        /// <inheritdoc />
        public Task SendMessage<T>(
            string name,
            Guid messageId,
            T data,
            CancellationToken cancellationToken)
        {
            var socket = GetActiveSockets()
                .OrderByDescending(i => i.LastActivityDate)
                .FirstOrDefault();

            if (socket == null)
            {
                return Task.CompletedTask;
            }

            return socket.SendAsync(
                new WebSocketMessage<T>
                {
                    Data = data,
                    MessageType = name,
                    MessageId = messageId
                },
                cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var socket in _sockets)
            {
                socket.Closed -= OnConnectionClosed;
            }

            _disposed = true;
        }
    }
}
