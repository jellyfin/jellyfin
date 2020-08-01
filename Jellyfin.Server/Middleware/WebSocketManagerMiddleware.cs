using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.WebSockets;
using MediaBrowser.Model.IO;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Server.Middleware
{
    /// <summary>
    /// Web socket manager middleware.
    /// </summary>
    public class WebSocketManagerMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketManagerMiddleware"/> class.
        /// </summary>
        /// <param name="next">Next request delegate.</param>
        /// <param name="webSocketHandler">The web socket handler.</param>
        public WebSocketManagerMiddleware(RequestDelegate next, BaseWebSocketHandler webSocketHandler)
        {
            _next = next;
            WebSocketHandler = webSocketHandler;
        }

        private BaseWebSocketHandler WebSocketHandler { get; }

        /// <summary>
        /// Invoke handler on context.
        /// </summary>
        /// <param name="context">Http context.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await _next.Invoke(context).ConfigureAwait(false);
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            await WebSocketHandler.OnConnected(socket).ConfigureAwait(false);

            await Receive(socket, async (result, buffer) =>
            {
                switch (result.MessageType)
                {
                    case WebSocketMessageType.Text:
                        await WebSocketHandler.ReceiveAsync(socket, result, buffer).ConfigureAwait(false);
                        return;
                    case WebSocketMessageType.Close:
                        await WebSocketHandler.OnDisconnected(socket).ConfigureAwait(false);
                        return;
                }
            }).ConfigureAwait(false);
        }

        private static async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(IODefaults.CopyToBufferSize);
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None).ConfigureAwait(false);

                handleMessage(result, buffer);
            }
        }
    }
}
