using MediaBrowser.Model.Logging;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Class NativeWebSocket
    /// </summary>
    public class NativeWebSocket : IWebSocket
    {
        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Gets or sets the web socket.
        /// </summary>
        /// <value>The web socket.</value>
        private WebSocket WebSocket { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeWebSocket" /> class.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentNullException">socket</exception>
        public NativeWebSocket(WebSocket socket, ILogger logger)
        {
            if (socket == null)
            {
                throw new ArgumentNullException("socket");
            }

            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _logger = logger;
            WebSocket = socket;

            Receive();
        }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>The state.</value>
        public WebSocketState State
        {
            get { return WebSocket.State; }
        }

        /// <summary>
        /// Receives this instance.
        /// </summary>
        private async void Receive()
        {
            while (true)
            {
                byte[] bytes;

                try
                {
                    bytes = await ReceiveBytesAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (WebSocketException ex)
                {
                    _logger.ErrorException("Error reveiving web socket message", ex);

                    break;
                }

                if (OnReceiveDelegate != null)
                {
                    OnReceiveDelegate(bytes);
                }
            }
        }

        /// <summary>
        /// Receives the async.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{WebSocketMessageInfo}.</returns>
        /// <exception cref="System.Net.WebSockets.WebSocketException">Connection closed</exception>
        private async Task<byte[]> ReceiveBytesAsync(CancellationToken cancellationToken)
        {
            var bytes = new byte[4096];
            var buffer = new ArraySegment<byte>(bytes);

            var result = await WebSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (result.CloseStatus.HasValue)
            {
                throw new WebSocketException("Connection closed");
            }

            return buffer.Array;
        }

        /// <summary>
        /// Sends the async.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="type">The type.</param>
        /// <param name="endOfMessage">if set to <c>true</c> [end of message].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendAsync(byte[] bytes, WebSocketMessageType type, bool endOfMessage, CancellationToken cancellationToken)
        {
            return WebSocket.SendAsync(new ArraySegment<byte>(bytes), type, true, cancellationToken);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                WebSocket.Dispose();
            }
        }

        /// <summary>
        /// Gets or sets the receive action.
        /// </summary>
        /// <value>The receive action.</value>
        public Action<byte[]> OnReceiveDelegate { get; set; }
    }
}
