using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Serialization;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
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
        private static ILogger Logger = LogManager.GetLogger("NativeWebSocket");

        /// <summary>
        /// Gets or sets the web socket.
        /// </summary>
        /// <value>The web socket.</value>
        private WebSocket WebSocket { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeWebSocket" /> class.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <exception cref="System.ArgumentNullException">socket</exception>
        public NativeWebSocket(WebSocket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException("socket");
            }

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
                    Logger.ErrorException("Error reveiving web socket message", ex);

                    break;
                }

                if (OnReceiveDelegate != null)
                {
                    using (var memoryStream = new MemoryStream(bytes))
                    {
                        try
                        {
                            var messageResult = JsonSerializer.DeserializeFromStream<WebSocketMessageInfo>(memoryStream);

                            OnReceiveDelegate(messageResult);
                        }
                        catch (Exception ex)
                        {
                            Logger.ErrorException("Error processing web socket message", ex);
                        }
                    }
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
        public Action<WebSocketMessageInfo> OnReceiveDelegate { get; set; }
    }
}
