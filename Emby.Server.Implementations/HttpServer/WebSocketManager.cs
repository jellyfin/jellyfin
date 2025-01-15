#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.HttpServer
{
    public class WebSocketManager : IWebSocketManager
    {
        private readonly IWebSocketListener[] _webSocketListeners;
        private readonly IAuthService _authService;
        private readonly ILogger<WebSocketManager> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public WebSocketManager(
            IAuthService authService,
            IEnumerable<IWebSocketListener> webSocketListeners,
            ILogger<WebSocketManager> logger,
            ILoggerFactory loggerFactory)
        {
            _webSocketListeners = webSocketListeners.ToArray();
            _authService = authService;
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc />
        public async Task WebSocketRequestHandler(HttpContext context)
        {
            var authorizationInfo = await _authService.Authenticate(context.Request).ConfigureAwait(false);
            if (!authorizationInfo.IsAuthenticated)
            {
                throw new SecurityException("Token is required");
            }

            try
            {
                _logger.LogInformation("WS {IP} request", context.Connection.RemoteIpAddress);

                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);

                var connection = new WebSocketConnection(
                    _loggerFactory.CreateLogger<WebSocketConnection>(),
                    webSocket,
                    authorizationInfo,
                    context.GetNormalizedRemoteIP())
                {
                    OnReceive = ProcessWebSocketMessageReceived
                };
                await using (connection.ConfigureAwait(false))
                {
                    var tasks = new Task[_webSocketListeners.Length];
                    for (var i = 0; i < _webSocketListeners.Length; ++i)
                    {
                        tasks[i] = _webSocketListeners[i].ProcessWebSocketConnectedAsync(connection, context);
                    }

                    await Task.WhenAll(tasks).ConfigureAwait(false);

                    await connection.ReceiveAsync().ConfigureAwait(false);
                    _logger.LogInformation("WS {IP} closed", context.Connection.RemoteIpAddress);
                }
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
        private async Task ProcessWebSocketMessageReceived(WebSocketMessageInfo result)
        {
            var tasks = new Task[_webSocketListeners.Length];
            for (var i = 0; i < _webSocketListeners.Length; ++i)
            {
                tasks[i] = _webSocketListeners[i].ProcessMessageAsync(result);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
