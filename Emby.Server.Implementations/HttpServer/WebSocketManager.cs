#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Emby.Server.Implementations.Session;
using Jellyfin.Api.WebSocketListeners;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.HttpServer
{
    public class WebSocketManager : IWebSocketManager
    {
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger<WebSocketManager> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private bool _disposed = false;

        public WebSocketManager(
            IServerApplicationHost appHost,
            ILogger<WebSocketManager> logger,
            ILoggerFactory loggerFactory)
        {
            _appHost = appHost;
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc />
        public async Task WebSocketRequestHandler(HttpContext context)
        {
            if (_disposed)
            {
                return;
            }

            var listener = _appHost.Resolve<ISessionWebSocketListener>();

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

                listener?.ProcessWebSocketConnected(connection);

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

            Parallel.Invoke(
                () => _appHost.Resolve<IActivityLogWebSocketListener>(),
                () => _appHost.Resolve<IScheduledTasksWebSocketListener>(),
                () => _appHost.Resolve<ISessionInfoWebSocketListener>());

            return Task.CompletedTask;
        }
    }
}
