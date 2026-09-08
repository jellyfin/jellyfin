#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Net.WebSocketMessages;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Session
{
    public sealed class WebSocketController : ISessionController, IAsyncDisposable, IDisposable
    {
        private readonly ILogger<WebSocketController> _logger;
        private readonly ISessionManager _sessionManager;
        private readonly SessionInfo _session;

        private readonly List<IWebSocketConnection> _sockets;
        private readonly ReaderWriterLockSlim _socketsLock;
        private int _disposed;

        public WebSocketController(
            ILogger<WebSocketController> logger,
            SessionInfo session,
            ISessionManager sessionManager)
        {
            _logger = logger;
            _session = session;
            _sessionManager = sessionManager;
            _sockets = new();
            _socketsLock = new();
        }

        private bool HasOpenSockets
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed != 0, this);
                try
                {
                    _socketsLock.EnterReadLock();
                    return _sockets.Any(i => i.State == WebSocketState.Open);
                }
                finally
                {
                    _socketsLock.ExitReadLock();
                }
            }
        }

        /// <inheritdoc />
        public bool SupportsMediaControl => HasOpenSockets;

        /// <inheritdoc />
        public bool IsSessionActive => HasOpenSockets;

        public void AddWebSocket(IWebSocketConnection connection)
        {
            _logger.LogDebug("Adding websocket to session {Session}", _session.Id);
            ObjectDisposedException.ThrowIf(_disposed != 0, this);
            try
            {
                _socketsLock.EnterWriteLock();
                _sockets.Add(connection);
                connection.Closed += OnConnectionClosed;
            }
            finally
            {
                _socketsLock.ExitWriteLock();
            }
        }

        private async void OnConnectionClosed(object? sender, EventArgs e)
        {
            var connection = sender as IWebSocketConnection ?? throw new ArgumentException($"{nameof(sender)} is not of type {nameof(IWebSocketConnection)}", nameof(sender));
            _logger.LogDebug("Removing websocket from session {Session}", _session.Id);

            // A close can arrive after disposal, and this handler is async void, so throwing here
            // would terminate the process instead of reaching the raiser.
            if (_disposed != 0)
            {
                return;
            }

            try
            {
                _socketsLock.EnterWriteLock();
                _sockets.Remove(connection);
                connection.Closed -= OnConnectionClosed;
            }
            finally
            {
                _socketsLock.ExitWriteLock();
            }

            await _sessionManager.CloseIfNeededAsync(_session).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task SendMessage<T>(
            SessionMessageType name,
            Guid messageId,
            T data,
            CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed != 0, this);
            IWebSocketConnection? socket;
            try
            {
                _socketsLock.EnterReadLock();
                socket = _sockets.Where(i => i.State == WebSocketState.Open).MaxBy(i => i.LastActivityDate);
            }
            finally
            {
                _socketsLock.ExitReadLock();
            }

            if (socket is null)
            {
                return Task.CompletedTask;
            }

            return socket.SendAsync(
                new OutboundWebSocketMessage<T>
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
            foreach (var socket in DetachSockets())
            {
                socket.Dispose();
            }
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var socket in DetachSockets())
            {
                await socket.DisposeAsync().ConfigureAwait(false);
            }
        }

        // The lock is thread affine, so it cannot be held across the await in DisposeAsync.
        private IWebSocketConnection[] DetachSockets()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return [];
            }

            try
            {
                _socketsLock.EnterWriteLock();
                foreach (var socket in _sockets)
                {
                    socket.Closed -= OnConnectionClosed;
                }

                var sockets = _sockets.ToArray();
                _sockets.Clear();
                return sockets;
            }
            finally
            {
                _socketsLock.ExitWriteLock();
            }
        }
    }
}
