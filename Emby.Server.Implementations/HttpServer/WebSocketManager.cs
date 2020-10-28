#pragma warning disable CS1591

using System;
using System.Collections.Generic;
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
        private readonly Lazy<IEnumerable<IWebSocketListener>> _webSocketListeners;
        private readonly ILogger<WebSocketManager> _logger;
        private readonly ILoggerFactory _loggerFactory;

        private bool _disposed = false;

        public WebSocketManager(
            Lazy<IEnumerable<IWebSocketListener>> webSocketListeners,
            ILogger<WebSocketManager> logger,
            ILoggerFactory loggerFactory)
        {
            _webSocketListeners = webSocketListeners;
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
                var listeners = _webSocketListeners.Value;
                foreach (var x in listeners)
                {
                    yield return x.ProcessMessageAsync(result);
                }
            }

            return Task.WhenAll(GetTasks());
        }
    }
}
