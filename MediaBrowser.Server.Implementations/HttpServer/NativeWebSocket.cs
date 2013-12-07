using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using WebSocketMessageType = MediaBrowser.Model.Net.WebSocketMessageType;
using WebSocketState = MediaBrowser.Model.Net.WebSocketState;

namespace MediaBrowser.Server.Implementations.HttpServer
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
        private System.Net.WebSockets.WebSocket WebSocket { get; set; }

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeWebSocket" /> class.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentNullException">socket</exception>
        public NativeWebSocket(System.Net.WebSockets.WebSocket socket, ILogger logger)
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
            get
            {
                WebSocketState commonState;

                if (!Enum.TryParse(WebSocket.State.ToString(), true, out commonState))
                {
                    _logger.Warn("Unrecognized WebSocketState: {0}", WebSocket.State.ToString());
                }

                return commonState;
            }
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
                    bytes = await ReceiveBytesAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (WebSocketException ex)
                {
                    _logger.ErrorException("Error receiving web socket message", ex);

                    break;
                }

                if (bytes == null)
                {
                    // Connection closed
                    break;
                }

                if (OnReceiveBytes != null)
                {
                    OnReceiveBytes(bytes);
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
                _logger.Info("Web socket connection closed by client. Reason: {0}", result.CloseStatus.Value);
                return null;
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
            System.Net.WebSockets.WebSocketMessageType nativeType;

            if (!Enum.TryParse(type.ToString(), true, out nativeType))
            {
                _logger.Warn("Unrecognized WebSocketMessageType: {0}", type.ToString());
            }

            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

            return WebSocket.SendAsync(new ArraySegment<byte>(bytes), nativeType, true, linkedTokenSource.Token);
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
                _cancellationTokenSource.Cancel();

                WebSocket.Dispose();
            }
        }

        /// <summary>
        /// Gets or sets the receive action.
        /// </summary>
        /// <value>The receive action.</value>
        public Action<byte[]> OnReceiveBytes { get; set; }

        /// <summary>
        /// Gets or sets the on receive.
        /// </summary>
        /// <value>The on receive.</value>
        public Action<string> OnReceive { get; set; }

        /// <summary>
        /// The _supports native web socket
        /// </summary>
        private static bool? _supportsNativeWebSocket;

        /// <summary>
        /// Gets a value indicating whether [supports web sockets].
        /// </summary>
        /// <value><c>true</c> if [supports web sockets]; otherwise, <c>false</c>.</value>
        public static bool IsSupported
        {
            get
            {
#if __MonoCS__
				return false;
#else
#endif

                if (!_supportsNativeWebSocket.HasValue)
                {
                    try
                    {
                        new ClientWebSocket();

                        _supportsNativeWebSocket = true;
                    }
                    catch (PlatformNotSupportedException)
                    {
                        _supportsNativeWebSocket = false;
                    }
                }

                return _supportsNativeWebSocket.Value;
            }
        }
    }
}
