using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Session
{
    /// <summary>
    /// Class SessionWebSocketListener
    /// </summary>
    public class SessionWebSocketListener : IWebSocketListener, IDisposable
    {
        /// <summary>
        /// The timeout in seconds after which a WebSocket is considered to be lost.
        /// </summary>
        public readonly int WebSocketLostTimeout = 60;

        /// <summary>
        /// The timer factor; controls the frequency of the timer.
        /// </summary>
        public readonly double TimerFactor = 0.2;

        /// <summary>
        /// The ForceKeepAlive factor; controls when a ForceKeepAlive is sent.
        /// </summary>
        public readonly double ForceKeepAliveFactor = 0.75;

        /// <summary>
        /// The _session manager
        /// </summary>
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The _dto service
        /// </summary>
        private readonly IJsonSerializer _json;

        private readonly IHttpServer _httpServer;

        /// <summary>
        /// The KeepAlive timer.
        /// </summary>
        private Timer _keepAliveTimer;

        /// <summary>
        /// The WebSocket watchlist.
        /// </summary>
        private readonly ConcurrentDictionary<IWebSocketConnection, byte> _webSockets = new ConcurrentDictionary<IWebSocketConnection, byte>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionWebSocketListener" /> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="json">The json.</param>
        /// <param name="httpServer">The HTTP server.</param>
        public SessionWebSocketListener(ISessionManager sessionManager, ILoggerFactory loggerFactory, IJsonSerializer json, IHttpServer httpServer)
        {
            _sessionManager = sessionManager;
            _logger = loggerFactory.CreateLogger(GetType().Name);
            _json = json;
            _httpServer = httpServer;
            httpServer.WebSocketConnected += _serverManager_WebSocketConnected;
        }

        void _serverManager_WebSocketConnected(object sender, GenericEventArgs<IWebSocketConnection> e)
        {
            var session = GetSession(e.Argument.QueryString, e.Argument.RemoteEndPoint);

            if (session != null)
            {
                EnsureController(session, e.Argument);
                KeepAliveWebSocket(e.Argument);
            }
            else
            {
                _logger.LogWarning("Unable to determine session based on url: {0}", e.Argument.Url);
            }
        }

        private SessionInfo GetSession(IQueryCollection queryString, string remoteEndpoint)
        {
            if (queryString == null)
            {
                return null;
            }

            var token = queryString["api_key"];
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var deviceId = queryString["deviceId"];
            return _sessionManager.GetSessionByAuthenticationToken(token, deviceId, remoteEndpoint);
        }

        public void Dispose()
        {
            _httpServer.WebSocketConnected -= _serverManager_WebSocketConnected;
            StopKeepAliveTimer();
        }

        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>Task.</returns>
        public Task ProcessMessageAsync(WebSocketMessageInfo message)
            => Task.CompletedTask;

        private void EnsureController(SessionInfo session, IWebSocketConnection connection)
        {
            var controllerInfo = session.EnsureController<WebSocketController>(s => new WebSocketController(s, _logger, _sessionManager));

            var controller = (WebSocketController)controllerInfo.Item1;
            controller.AddWebSocket(connection);
        }

        /// <summary>
        /// Called when a WebSocket is closed.
        /// </summary>
        /// <param name="sender">The WebSocket.</param>
        /// <param name="e">The event arguments.</param>
        private void _webSocket_Closed(object sender, EventArgs e)
        {
            var webSocket = (IWebSocketConnection) sender;
            webSocket.Closed -= _webSocket_Closed;
            _webSockets.TryRemove(webSocket, out _);
        }

        /// <summary>
        /// Adds a WebSocket to the KeepAlive watchlist.
        /// </summary>
        /// <param name="webSocket">The WebSocket to monitor.</param>
        private async void KeepAliveWebSocket(IWebSocketConnection webSocket)
        {
            _webSockets.TryAdd(webSocket, 0);
            webSocket.Closed += _webSocket_Closed;
            webSocket.LastKeepAliveDate = DateTime.UtcNow;

            // Notify WebSocket about timeout
            try
            {
                await SendForceKeepAlive(webSocket);
            }
            catch (WebSocketException exception)
            {
                _logger.LogDebug(exception, "Error sending ForceKeepAlive message to WebSocket.");
            }

            StartKeepAliveTimer();
        }

        /// <summary>
        /// Starts the KeepAlive timer.
        /// </summary>
        private void StartKeepAliveTimer()
        {
            if (_keepAliveTimer == null)
            {
                _keepAliveTimer = new Timer(
                    KeepAliveSockets,
                    null,
                    TimeSpan.FromSeconds(WebSocketLostTimeout * TimerFactor),
                    TimeSpan.FromSeconds(WebSocketLostTimeout * TimerFactor)
                );
            }
        }

        /// <summary>
        /// Stops the KeepAlive timer.
        /// </summary>
        private void StopKeepAliveTimer()
        {
            if (_keepAliveTimer != null)
            {
                _keepAliveTimer.Dispose();
                _keepAliveTimer = null;
            }

            foreach (var pair in _webSockets)
            {
                pair.Key.Closed -= _webSocket_Closed;
            }
        }

        /// <summary>
        /// Checks status of KeepAlive of WebSockets.
        /// </summary>
        /// <param name="state">The state.</param>
        private async void KeepAliveSockets(object state)
        {
            var inactive = _webSockets.Keys.Where(i =>
            {
                var elapsed = (DateTime.UtcNow - i.LastKeepAliveDate).TotalSeconds;
                return (elapsed > WebSocketLostTimeout * ForceKeepAliveFactor) && (elapsed < WebSocketLostTimeout);
            });
            var lost = _webSockets.Keys.Where(i => (DateTime.UtcNow - i.LastKeepAliveDate).TotalSeconds >= WebSocketLostTimeout);

            if (inactive.Any())
            {
                _logger.LogDebug("Sending ForceKeepAlive message to {0} WebSockets.", inactive.Count());
            }

            foreach (var webSocket in inactive)
            {
                try
                {
                    await SendForceKeepAlive(webSocket);
                }
                catch (WebSocketException exception)
                {
                    _logger.LogDebug(exception, "Error sending ForceKeepAlive message to WebSocket.");
                    lost.Append(webSocket);
                }
            }

            if (lost.Any())
            {
                // TODO: handle lost webSockets
                _logger.LogDebug("Lost {0} WebSockets.", lost.Count());
            }

            if (!_webSockets.Any())
            {
                StopKeepAliveTimer();
            }
        }

        /// <summary>
        /// Sends a ForceKeepAlive message to a WebSocket.
        /// </summary>
        /// <param name="webSocket">The WebSocket.</param>
        /// <returns>Task.</returns>
        private Task SendForceKeepAlive(IWebSocketConnection webSocket)
        {
            return webSocket.SendAsync(new WebSocketMessage<int>
            {
                MessageType = "ForceKeepAlive",
                Data = WebSocketLostTimeout
            }, CancellationToken.None);
        }
    }
}
