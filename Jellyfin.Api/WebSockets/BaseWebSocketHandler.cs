using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Api.WebSockets
{
    /// <summary>
    /// Base web socket handler.
    /// </summary>
    public abstract class BaseWebSocketHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseWebSocketHandler"/> class.
        /// </summary>
        /// <param name="webSocketConnectionManager">Instance of the <see cref="WebSocketConnectionManager"/> singleton.</param>
        public BaseWebSocketHandler(WebSocketConnectionManager webSocketConnectionManager)
        {
            WebSocketConnectionManager = webSocketConnectionManager;
        }

        /// <summary>
        /// Gets web socket connection manager.
        /// </summary>
        protected WebSocketConnectionManager WebSocketConnectionManager { get; }

        /// <summary>
        /// Socket connected handler.
        /// </summary>
        /// <param name="socket">Socket that connected.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        public virtual Task OnConnected(WebSocket socket)
        {
            WebSocketConnectionManager.AddSocket(socket);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Socket disconnected handler.
        /// </summary>
        /// <param name="socket">Socket that disconnected.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        public virtual async Task OnDisconnected(WebSocket socket)
        {
            var socketId = WebSocketConnectionManager.GetId(socket);
            if (socketId is null)
            {
                return;
            }

            await WebSocketConnectionManager.RemoveSocket(socketId.Value).ConfigureAwait(false);
        }

        /// <summary>
        /// Send message to socket.
        /// </summary>
        /// <param name="socket">Socket to send to.</param>
        /// <param name="message">Message to send.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        public async Task SendMessageAsync(WebSocket socket, string message)
        {
            if (socket.State != WebSocketState.Open)
            {
                return;
            }

            await socket.SendAsync(
                    new ArraySegment<byte>(
                        Encoding.UTF8.GetBytes(message),
                        0,
                        message.Length),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Send message to socketId.
        /// </summary>
        /// <param name="socketId">Socket id to send message to.</param>
        /// <param name="message">Message to send.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        public async Task SendMessageAsync(Guid socketId, string message)
        {
            var socket = WebSocketConnectionManager.GetSocketById(socketId);
            if (socket == null)
            {
                return;
            }

            await SendMessageAsync(socket, message).ConfigureAwait(false);
        }

        /// <summary>
        /// Send message to all sockets.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        public async Task SendMessageToAllAsync(string message)
        {
            foreach (var socket in WebSocketConnectionManager.GetAll())
            {
                if (socket.State == WebSocketState.Open)
                {
                    await SendMessageAsync(socket, message).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Receive message.
        /// </summary>
        /// <param name="socket">Socket message was received from.</param>
        /// <param name="result">Socket receive result.</param>
        /// <param name="buffer">Message buffer.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        public abstract Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer);
    }
}
