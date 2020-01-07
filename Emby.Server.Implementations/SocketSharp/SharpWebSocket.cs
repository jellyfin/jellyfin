using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Net;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.SocketSharp
{
    public class SharpWebSocket : IWebSocket
    {
        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        public event EventHandler<EventArgs> Closed;

        /// <summary>
        /// Gets or sets the web socket.
        /// </summary>
        /// <value>The web socket.</value>
        private readonly WebSocket _webSocket;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _disposed;

        public SharpWebSocket(WebSocket socket, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _webSocket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>The state.</value>
        public WebSocketState State => _webSocket.State;

        /// <summary>
        /// Sends the async.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="endOfMessage">if set to <c>true</c> [end of message].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendAsync(byte[] bytes, bool endOfMessage, CancellationToken cancellationToken)
        {
            return _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, endOfMessage, cancellationToken);
        }

        /// <summary>
        /// Sends the asynchronous.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="endOfMessage">if set to <c>true</c> [end of message].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendAsync(string text, bool endOfMessage, CancellationToken cancellationToken)
        {
            return _webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)), WebSocketMessageType.Text, endOfMessage, cancellationToken);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (_disposed)
            {
                return;
            }

            if (dispose)
            {
                _cancellationTokenSource.Cancel();
                if (_webSocket.State == WebSocketState.Open)
                {
                    _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client",
                        CancellationToken.None);
                }
                Closed?.Invoke(this, EventArgs.Empty);
            }

            _disposed = true;
        }

        /// <summary>
        /// Gets or sets the receive action.
        /// </summary>
        /// <value>The receive action.</value>
        public Action<byte[]> OnReceiveBytes { get; set; }
    }
}
