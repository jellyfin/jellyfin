using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Net.WebSocketMessages.Outbound;
using MediaBrowser.Controller.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Session
{
    /// <summary>
    /// Class SessionWebSocketListener.
    /// </summary>
    public sealed class SessionWebSocketListener : IWebSocketListener, IDisposable
    {
        /// <summary>
        /// The timeout in seconds after which a WebSocket is considered to be lost.
        /// </summary>
        private const int WebSocketLostTimeout = 60;

        /// <summary>
        /// The keep-alive interval factor; controls how often the watcher will check on the status of the WebSockets.
        /// </summary>
        private const float IntervalFactor = 0.2f;

        /// <summary>
        /// The ForceKeepAlive factor; controls when a ForceKeepAlive is sent.
        /// </summary>
        private const float ForceKeepAliveFactor = 0.75f;

        /// <summary>
        /// The WebSocket watchlist.
        /// </summary>
        private readonly HashSet<IWebSocketConnection> _webSockets = new HashSet<IWebSocketConnection>();

        /// <summary>
        /// Lock used for accessing the WebSockets watchlist.
        /// </summary>
        private readonly Lock _webSocketsLock = new();

        private readonly ISessionManager _sessionManager;
        private readonly ILogger<SessionWebSocketListener> _logger;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// The KeepAlive cancellation token.
        /// </summary>
        private System.Timers.Timer _keepAlive;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionWebSocketListener" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public SessionWebSocketListener(
            ILogger<SessionWebSocketListener> logger,
            ISessionManager sessionManager,
            ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _sessionManager = sessionManager;
            _loggerFactory = loggerFactory;
            _keepAlive = new System.Timers.Timer(TimeSpan.FromSeconds(WebSocketLostTimeout * IntervalFactor))
            {
                AutoReset = true,
                Enabled = false
            };
            _keepAlive.Elapsed += KeepAliveSockets;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_keepAlive is not null)
            {
                _keepAlive.Stop();
                _keepAlive.Elapsed -= KeepAliveSockets;
                _keepAlive.Dispose();
                _keepAlive = null!;
            }

            lock (_webSocketsLock)
            {
                foreach (var webSocket in _webSockets)
                {
                    webSocket.Closed -= OnWebSocketClosed;
                }

                _webSockets.Clear();
            }
        }

        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>Task.</returns>
        public Task ProcessMessageAsync(WebSocketMessageInfo message)
            => Task.CompletedTask;

        /// <inheritdoc />
        public async Task ProcessWebSocketConnectedAsync(IWebSocketConnection connection, HttpContext httpContext)
        {
            var session = await GetSession(httpContext, connection.RemoteEndPoint?.ToString()).ConfigureAwait(false);
            if (session is not null)
            {
                EnsureController(session, connection);
                await KeepAliveWebSocket(connection).ConfigureAwait(false);
            }
            else
            {
                _logger.LogWarning("Unable to determine session based on query string: {0}", httpContext.Request.QueryString);
            }
        }

        private async Task<SessionInfo?> GetSession(HttpContext httpContext, string? remoteEndpoint)
        {
            if (!httpContext.User.Identity?.IsAuthenticated ?? false)
            {
                return null;
            }

            var deviceId = httpContext.User.GetDeviceId();
            if (httpContext.Request.Query.TryGetValue("deviceId", out var queryDeviceId))
            {
                deviceId = queryDeviceId;
            }

            return await _sessionManager.GetSessionByAuthenticationToken(httpContext.User.GetToken(), deviceId, remoteEndpoint)
                .ConfigureAwait(false);
        }

        private void EnsureController(SessionInfo session, IWebSocketConnection connection)
        {
            var controllerInfo = session.EnsureController<WebSocketController>(
                s => new WebSocketController(_loggerFactory.CreateLogger<WebSocketController>(), s, _sessionManager));

            var controller = (WebSocketController)controllerInfo.Item1;
            controller.AddWebSocket(connection);

            _sessionManager.OnSessionControllerConnected(session);
        }

        /// <summary>
        /// Called when a WebSocket is closed.
        /// </summary>
        /// <param name="sender">The WebSocket.</param>
        /// <param name="e">The event arguments.</param>
        private void OnWebSocketClosed(object? sender, EventArgs e)
        {
            if (sender is null)
            {
                return;
            }

            var webSocket = (IWebSocketConnection)sender;
            _logger.LogDebug("WebSocket {0} is closed.", webSocket);
            RemoveWebSocket(webSocket);
        }

        /// <summary>
        /// Adds a WebSocket to the KeepAlive watchlist.
        /// </summary>
        /// <param name="webSocket">The WebSocket to monitor.</param>
        private async Task KeepAliveWebSocket(IWebSocketConnection webSocket)
        {
            lock (_webSocketsLock)
            {
                if (!_webSockets.Add(webSocket))
                {
                    _logger.LogWarning("Multiple attempts to keep alive single WebSocket {0}", webSocket);
                    return;
                }

                webSocket.Closed += OnWebSocketClosed;
                webSocket.LastKeepAliveDate = DateTime.UtcNow;

                _keepAlive.Start();
            }

            // Notify WebSocket about timeout
            try
            {
                await SendForceKeepAlive(webSocket).ConfigureAwait(false);
            }
            catch (WebSocketException exception)
            {
                _logger.LogWarning(exception, "Cannot send ForceKeepAlive message to WebSocket {0}.", webSocket);
            }
        }

        /// <summary>
        /// Removes a WebSocket from the KeepAlive watchlist.
        /// </summary>
        /// <param name="webSocket">The WebSocket to remove.</param>
        private void RemoveWebSocket(IWebSocketConnection webSocket)
        {
            lock (_webSocketsLock)
            {
                if (_webSockets.Remove(webSocket))
                {
                    webSocket.Closed -= OnWebSocketClosed;
                }
                else
                {
                    _logger.LogWarning("WebSocket {0} not on watchlist.", webSocket);
                }

                if (_webSockets.Count == 0)
                {
                    _keepAlive.Stop();
                }
            }
        }

        /// <summary>
        /// Checks status of KeepAlive of WebSockets.
        /// </summary>
        private async void KeepAliveSockets(object? o, EventArgs? e)
        {
            List<IWebSocketConnection> inactive;
            List<IWebSocketConnection> lost;

            lock (_webSocketsLock)
            {
                _logger.LogDebug("Watching {0} WebSockets.", _webSockets.Count);

                inactive = _webSockets.Where(i =>
                {
                    var elapsed = (DateTime.UtcNow - i.LastKeepAliveDate).TotalSeconds;
                    return (elapsed > WebSocketLostTimeout * ForceKeepAliveFactor) && (elapsed < WebSocketLostTimeout);
                }).ToList();
                lost = _webSockets.Where(i => (DateTime.UtcNow - i.LastKeepAliveDate).TotalSeconds >= WebSocketLostTimeout).ToList();
            }

            if (inactive.Count > 0)
            {
                _logger.LogInformation("Sending ForceKeepAlive message to {0} inactive WebSockets.", inactive.Count);
            }

            foreach (var webSocket in inactive)
            {
                try
                {
                    await SendForceKeepAlive(webSocket).ConfigureAwait(false);
                }
                catch (WebSocketException exception)
                {
                    _logger.LogInformation(exception, "Error sending ForceKeepAlive message to WebSocket.");
                    lost.Add(webSocket);
                }
            }

            lock (_webSocketsLock)
            {
                if (lost.Count > 0)
                {
                    _logger.LogInformation("Lost {0} WebSockets.", lost.Count);
                    foreach (var webSocket in lost)
                    {
                        // TODO: handle session relative to the lost webSocket
                        RemoveWebSocket(webSocket);
                    }
                }
            }
        }

        /// <summary>
        /// Sends a ForceKeepAlive message to a WebSocket.
        /// </summary>
        /// <param name="webSocket">The WebSocket.</param>
        /// <returns>Task.</returns>
        private Task SendForceKeepAlive(IWebSocketConnection webSocket)
        {
            return webSocket.SendAsync(
                new ForceKeepAliveMessage(WebSocketLostTimeout),
                CancellationToken.None);
        }
    }
}
