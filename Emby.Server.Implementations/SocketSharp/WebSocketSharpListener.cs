using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.HttpServer;
using Emby.Server.Implementations.Net;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

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

        public Action<WebSocketConnectEventArgs> WebSocketConnected { get; set; }

        private static void LogRequest(ILogger logger, HttpRequest request)
        {
            var url = request.GetDisplayUrl();

            logger.LogInformation("WS {Url}. UserAgent: {UserAgent}", url, request.Headers[HeaderNames.UserAgent].ToString());
        }

        public async Task ProcessWebSocketRequest(HttpContext ctx)
        {
            try
            {
                LogRequest(_logger, ctx.Request);
                var endpoint = ctx.Connection.RemoteIpAddress.ToString();
                var url = ctx.Request.GetDisplayUrl();

                var webSocketContext = await ctx.WebSockets.AcceptWebSocketAsync(null).ConfigureAwait(false);
                var socket = new SharpWebSocket(webSocketContext, _logger);

                WebSocketConnected(new WebSocketConnectEventArgs
                {
                    Url = url,
                    QueryString = ctx.Request.Query,
                    WebSocket = socket,
                    Endpoint = endpoint
                });

                WebSocketReceiveResult result;
                var message = new List<byte>();

                do
                {
                    var buffer = WebSocket.CreateServerBuffer(4096);
                    result = await webSocketContext.ReceiveAsync(buffer, _disposeCancellationToken);
                    message.AddRange(buffer.Array.Take(result.Count));

                    if (result.EndOfMessage)
                    {
                        socket.OnReceiveBytes(message.ToArray());
                        message.Clear();
                    }
                } while (socket.State == WebSocketState.Open && result.MessageType != WebSocketMessageType.Close);


                if (webSocketContext.State == WebSocketState.Open)
                {
                    await webSocketContext.CloseAsync(
                        result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                        result.CloseStatusDescription,
                        _disposeCancellationToken).ConfigureAwait(false);
                }

                socket.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AcceptWebSocketAsync error");
                if (!ctx.Response.HasStarted)
                {
                    ctx.Response.StatusCode = 500;
                }
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
        /// <param name="disposing">Whether or not the managed resources should be disposed.</param>
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
