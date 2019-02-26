using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.HttpServer;
using Emby.Server.Implementations.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

 namespace Emby.Server.Implementations.SocketSharp
{
    public class WebSocketSharpListener : IHttpListener
    {
        private readonly ILogger _logger;

        private CancellationTokenSource _disposeCancellationTokenSource = new CancellationTokenSource();
        private CancellationToken _disposeCancellationToken;

        public WebSocketSharpListener(
            ILogger logger)
        {
            _logger = logger;

            _disposeCancellationToken = _disposeCancellationTokenSource.Token;
        }

        public Func<Exception, IRequest, bool, bool, Task> ErrorHandler { get; set; }
        public Func<IHttpRequest, string, string, string, CancellationToken, Task> RequestHandler { get; set; }

        public Action<WebSocketConnectingEventArgs> WebSocketConnecting { get; set; }

        public Action<WebSocketConnectEventArgs> WebSocketConnected { get; set; }

        private static void LogRequest(ILogger logger, HttpRequest request)
        {
            var url = request.GetDisplayUrl();

            logger.LogInformation("{0} {1}. UserAgent: {2}", "WS", url, request.Headers["User-Agent"].ToString());
        }

        public async Task ProcessWebSocketRequest(HttpContext ctx)
        {
            try
            {
                LogRequest(_logger, ctx.Request);
                var endpoint = ctx.Connection.RemoteIpAddress.ToString();
                var url = ctx.Request.GetDisplayUrl();

                var queryString = new QueryParamCollection(ctx.Request.Query);

                var connectingArgs = new WebSocketConnectingEventArgs
                {
                    Url = url,
                    QueryString = queryString,
                    Endpoint = endpoint
                };

                WebSocketConnecting?.Invoke(connectingArgs);

                if (connectingArgs.AllowConnection)
                {
                    _logger.LogDebug("Web socket connection allowed");

                    var webSocketContext = await ctx.WebSockets.AcceptWebSocketAsync(null).ConfigureAwait(false);
                    var socket = new SharpWebSocket(webSocketContext, _logger);

                    WebSocketConnected(new WebSocketConnectEventArgs
                    {
                        Url = url,
                        QueryString = queryString,
                        WebSocket = socket,
                        Endpoint = endpoint
                    });

                    var buffer = WebSocket.CreateClientBuffer(4096, 4096);
                    WebSocketReceiveResult result;
                    var message = new List<byte>();

                    do
                    {
                        result = await webSocketContext.ReceiveAsync(buffer, _disposeCancellationToken);
                        socket.OnReceiveBytes(buffer.Array);
                        message.AddRange(buffer.Array.Take(result.Count));
                    } while (!result.EndOfMessage && result.MessageType != WebSocketMessageType.Close);

                    socket.OnReceiveBytes(message.ToArray());
                    await webSocketContext.CloseAsync(result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                        result.CloseStatusDescription, _disposeCancellationToken);
                    socket.Dispose();
                }
                else
                {
                    _logger.LogWarning("Web socket connection not allowed");
                    ctx.Response.StatusCode = 401;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AcceptWebSocketAsync error");
                ctx.Response.StatusCode = 500;
            }
        }

        public Task Stop()
        {
            _disposeCancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Releases the unmanaged resources and disposes of the managed resources used.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed;

        /// <summary>
        /// Releases the unmanaged resources and disposes of the managed resources used.
        /// </summary>
        /// <param name="disposing">Whether or not the managed resources should be disposed</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                Stop().GetAwaiter().GetResult();
            }

            _disposed = true;
        }
    }
}
