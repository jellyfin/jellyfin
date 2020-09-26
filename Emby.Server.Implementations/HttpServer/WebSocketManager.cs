#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.HttpServer
{
    public class WebSocketManager : IWebSocketManager
    {
        private readonly ILogger<WebSocketManager> _logger;
        private readonly ILoggerFactory _loggerFactory;

        private IWebSocketListener[] _webSocketListeners = Array.Empty<IWebSocketListener>();
        private bool _disposed = false;

        public WebSocketManager(
            ILogger<WebSocketManager> logger,
            ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        public event EventHandler<GenericEventArgs<IWebSocketConnection>> WebSocketConnected;

        /// <inheritdoc />
        public async Task WebSocketRequestHandler(HttpContext context)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _logger.LogInformation("WS {IP} request", context.Connection.RemoteIpAddress);

                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);

                using var connection = new WebSocketConnection(
                    _loggerFactory.CreateLogger<WebSocketConnection>(),
                    webSocket,
                    context.Connection.RemoteIpAddress,
                    context.Request.Query)
                {
                    OnReceive = ProcessWebSocketMessageReceived
                };

                WebSocketConnected?.Invoke(this, new GenericEventArgs<IWebSocketConnection>(connection));

                await connection.ProcessAsync().ConfigureAwait(false);
                _logger.LogInformation("WS {IP} closed", context.Connection.RemoteIpAddress);
            }
            catch (Exception ex) // Otherwise ASP.Net will ignore the exception
            {
                _logger.LogError(ex, "WS {IP} WebSocketRequestHandler error", context.Connection.RemoteIpAddress);
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = 500;
                }
            }
        }

        /// <summary>
        /// Adds the rest handlers.
        /// </summary>
        /// <param name="listeners">The web socket listeners.</param>
        public void Init(IEnumerable<IWebSocketListener> listeners)
        {
            _webSocketListeners = listeners.ToArray();
        }

        /// <summary>
        /// Processes the web socket message received.
        /// </summary>
        /// <param name="result">The result.</param>
        private Task ProcessWebSocketMessageReceived(WebSocketMessageInfo result)
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            IEnumerable<Task> GetTasks()
            {
                foreach (var x in _webSocketListeners)
                {
                    yield return x.ProcessMessageAsync(result);
                }
            }

            return Task.WhenAll(GetTasks());
        }
    }
}
