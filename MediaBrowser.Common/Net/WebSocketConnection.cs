using MediaBrowser.Common.Serialization;
using MediaBrowser.Model.Logging;
using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Class WebSocketConnection
    /// </summary>
    public class WebSocketConnection : IDisposable
    {
        /// <summary>
        /// The _socket
        /// </summary>
        private readonly IWebSocket _socket;

        /// <summary>
        /// The _remote end point
        /// </summary>
        public readonly EndPoint RemoteEndPoint;

        /// <summary>
        /// The _cancellation token source
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The _send semaphore
        /// </summary>
        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1,1);

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketConnection" /> class.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="receiveAction">The receive action.</param>
        /// <exception cref="System.ArgumentNullException">socket</exception>
        public WebSocketConnection(IWebSocket socket, EndPoint remoteEndPoint, Action<WebSocketMessageInfo> receiveAction, ILogger logger)
        {
            if (socket == null)
            {
                throw new ArgumentNullException("socket");
            }
            if (remoteEndPoint == null)
            {
                throw new ArgumentNullException("remoteEndPoint");
            }
            if (receiveAction == null)
            {
                throw new ArgumentNullException("receiveAction");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _socket = socket;
            _socket.OnReceiveDelegate = info => OnReceive(info, receiveAction);
            RemoteEndPoint = remoteEndPoint;
            _logger = logger;
        }

        /// <summary>
        /// Called when [receive].
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="callback">The callback.</param>
        private void OnReceive(WebSocketMessageInfo info, Action<WebSocketMessageInfo> callback)
        {
            try
            {
                info.Connection = this;

                callback(info);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error processing web socket message", ex);
            }
        }

        /// <summary>
        /// Sends a message asynchronously.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">message</exception>
        public Task SendAsync<T>(WebSocketMessage<T> message, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }
            
            var bytes = JsonSerializer.SerializeToBytes(message);

            return SendAsync(bytes, cancellationToken);
        }

        /// <summary>
        /// Sends a message asynchronously.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendAsync(byte[] buffer, CancellationToken cancellationToken)
        {
            return SendAsync(buffer, WebSocketMessageType.Text, cancellationToken);
        }

        /// <summary>
        /// Sends a message asynchronously.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="type">The type.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        public async Task SendAsync(byte[] buffer, WebSocketMessageType type, CancellationToken cancellationToken)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }
            
            cancellationToken.ThrowIfCancellationRequested();

            // Per msdn docs, attempting to send simultaneous messages will result in one failing.
            // This should help us workaround that and ensure all messages get sent
            await _sendSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await _socket.SendAsync(buffer, type, true, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.Info("WebSocket message to {0} was cancelled", RemoteEndPoint);

                throw;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending WebSocket message {0}", ex, RemoteEndPoint);

                throw;
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>The state.</value>
        public WebSocketState State
        {
            get { return _socket.State; }
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
            if (dispose)
            {
                _cancellationTokenSource.Dispose();
                _socket.Dispose();
            }
        }
    }

    /// <summary>
    /// Class WebSocketMessage
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WebSocketMessage<T>
    {
        /// <summary>
        /// Gets or sets the type of the message.
        /// </summary>
        /// <value>The type of the message.</value>
        public string MessageType { get; set; }
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public T Data { get; set; }
    }

    /// <summary>
    /// Class WebSocketMessageInfo
    /// </summary>
    public class WebSocketMessageInfo : WebSocketMessage<string>
    {
        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
        /// <value>The connection.</value>
        public WebSocketConnection Connection { get; set; }
    }
}
