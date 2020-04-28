using System;
using System.Collections.Generic;
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
        /// The keep-alive interval factor; controls how often the watcher will check on the status of the WebSockets.
        /// </summary>
        public readonly double IntervalFactor = 0.2;

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
        /// The KeepAlive cancellation token.
        /// </summary>
        private CancellationTokenSource _keepAliveCancellationToken;

        /// <summary>
        /// Lock used for accesing the KeepAlive cancellation token.
        /// </summary>
        private readonly object _keepAliveLock = new object();

        /// <summary>
        /// The WebSocket watchlist.
        /// </summary>
        private readonly HashSet<IWebSocketConnection> _webSockets = new HashSet<IWebSocketConnection>();

        /// <summary>
        /// Lock used for accesing the WebSockets watchlist.
        /// </summary>
        private readonly object _webSocketsLock = new object();

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
            StopKeepAlive();
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
        private void OnWebSocketClosed(object sender, EventArgs e)
        {
            var webSocket = (IWebSocketConnection) sender;
            _logger.LogDebug("WebSockets {0} closed.", webSocket);
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

                StartKeepAlive();
            }

            // Notify WebSocket about timeout
            try
            {
                await SendForceKeepAlive(webSocket);
            }
            catch (WebSocketException exception)
            {
                _logger.LogWarning(exception, "Error sending ForceKeepAlive message to WebSocket {0}.", webSocket);
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
                if (!_webSockets.Remove(webSocket))
                {
                    _logger.LogWarning("WebSocket {0} not on watchlist.", webSocket);
                }
                else
                {
                    webSocket.Closed -= OnWebSocketClosed;
                }
            }
        }

        /// <summary>
        /// Starts the KeepAlive watcher.
        /// </summary>
        private void StartKeepAlive()
        {
            lock (_keepAliveLock)
            {
                if (_keepAliveCancellationToken == null)
                {
                    _keepAliveCancellationToken = new CancellationTokenSource();
                    // Start KeepAlive watcher
                    KeepAliveSockets(
                        TimeSpan.FromSeconds(WebSocketLostTimeout * IntervalFactor),
                        _keepAliveCancellationToken.Token);
                }
            }
        }

        /// <summary>
        /// Stops the KeepAlive watcher.
        /// </summary>
        private void StopKeepAlive()
        {
            lock (_keepAliveLock)
            {
                if (_keepAliveCancellationToken != null)
                {
                    _keepAliveCancellationToken.Cancel();
                    _keepAliveCancellationToken = null;
                }
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
        /// Checks status of KeepAlive of WebSockets once every the specified interval time.
        /// </summary>
        /// <param name="interval">The interval.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task KeepAliveSockets(TimeSpan interval, CancellationToken cancellationToken)
        {
            while (true)
            {
                _logger.LogDebug("Watching {0} WebSockets.", _webSockets.Count());

                IEnumerable<IWebSocketConnection> inactive;
                IEnumerable<IWebSocketConnection> lost;
                lock (_webSocketsLock)
                {
                    inactive = _webSockets.Where(i =>
                    {
                        var elapsed = (DateTime.UtcNow - i.LastKeepAliveDate).TotalSeconds;
                        return (elapsed > WebSocketLostTimeout * ForceKeepAliveFactor) && (elapsed < WebSocketLostTimeout);
                    });
                    lost = _webSockets.Where(i => (DateTime.UtcNow - i.LastKeepAliveDate).TotalSeconds >= WebSocketLostTimeout);
                }

                if (inactive.Any())
                {
                    _logger.LogInformation("Sending ForceKeepAlive message to {0} inactive WebSockets.", inactive.Count());
                }

                foreach (var webSocket in inactive)
                {
                    try
                    {
                        await SendForceKeepAlive(webSocket);
                    }
                    catch (WebSocketException exception)
                    {
                        _logger.LogInformation(exception, "Error sending ForceKeepAlive message to WebSocket.");
                        lost = lost.Append(webSocket);
                    }
                }

                lock (_webSocketsLock)
                {
                    if (lost.Any())
                    {
                        _logger.LogInformation("Lost {0} WebSockets.", lost.Count());
                        foreach (var webSocket in lost.ToList())
                        {
                            // TODO: handle session relative to the lost webSocket
                            RemoveWebSocket(webSocket);
                        }
                    }

                    if (!_webSockets.Any())
                    {
                        StopKeepAlive();
                    }
                }

                // Wait for next interval
                Task task = Task.Delay(interval, cancellationToken);
                try
                {
                    await task;
                }
                catch (TaskCanceledException)
                {
                    return;
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
            return webSocket.SendAsync(new WebSocketMessage<int>
            {
                MessageType = "ForceKeepAlive",
                Data = WebSocketLostTimeout
            }, CancellationToken.None);
        }
    }
}
