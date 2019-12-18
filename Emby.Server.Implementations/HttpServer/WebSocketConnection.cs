using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UtfUnknown;

namespace Emby.Server.Implementations.HttpServer
{
    /// <summary>
    /// Class WebSocketConnection.
    /// </summary>
    public class WebSocketConnection : IWebSocketConnection
    {
        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The json serializer.
        /// </summary>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// The socket.
        /// </summary>
        private readonly IWebSocket _socket;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketConnection" /> class.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">socket</exception>
        public WebSocketConnection(IWebSocket socket, string remoteEndPoint, IJsonSerializer jsonSerializer, ILogger logger)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            if (string.IsNullOrEmpty(remoteEndPoint))
            {
                throw new ArgumentNullException(nameof(remoteEndPoint));
            }

            if (jsonSerializer == null)
            {
                throw new ArgumentNullException(nameof(jsonSerializer));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Id = Guid.NewGuid();
            _jsonSerializer = jsonSerializer;
            _socket = socket;
            _socket.OnReceiveBytes = OnReceiveInternal;

            RemoteEndPoint = remoteEndPoint;
            _logger = logger;

            socket.Closed += OnSocketClosed;
        }

        /// <inheritdoc />
        public event EventHandler<EventArgs> Closed;

        /// <summary>
        /// Gets or sets the remote end point.
        /// </summary>
        public string RemoteEndPoint { get; private set; }

        /// <summary>
        /// Gets or sets the receive action.
        /// </summary>
        /// <value>The receive action.</value>
        public Func<WebSocketMessageInfo, Task> OnReceive { get; set; }

        /// <summary>
        /// Gets the last activity date.
        /// </summary>
        /// <value>The last activity date.</value>
        public DateTime LastActivityDate { get; private set; }

        /// <summary>
        /// Gets the id.
        /// </summary>
        /// <value>The id.</value>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the query string.
        /// </summary>
        /// <value>The query string.</value>
        public IQueryCollection QueryString { get; set; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>The state.</value>
        public WebSocketState State => _socket.State;

        void OnSocketClosed(object sender, EventArgs e)
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when [receive].
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        private void OnReceiveInternal(byte[] bytes)
        {
            LastActivityDate = DateTime.UtcNow;

            if (OnReceive == null)
            {
                return;
            }
            var charset = CharsetDetector.DetectFromBytes(bytes).Detected?.EncodingName;

            if (string.Equals(charset, "utf-8", StringComparison.OrdinalIgnoreCase))
            {
                OnReceiveInternal(Encoding.UTF8.GetString(bytes, 0, bytes.Length));
            }
            else
            {
                OnReceiveInternal(Encoding.ASCII.GetString(bytes, 0, bytes.Length));
            }
        }

        private void OnReceiveInternal(string message)
        {
            LastActivityDate = DateTime.UtcNow;

            if (!message.StartsWith("{", StringComparison.OrdinalIgnoreCase))
            {
                // This info is useful sometimes but also clogs up the log
                _logger.LogDebug("Received web socket message that is not a json structure: {message}", message);
                return;
            }

            if (OnReceive == null)
            {
                return;
            }

            try
            {
                var stub = (WebSocketMessage<object>)_jsonSerializer.DeserializeFromString(message, typeof(WebSocketMessage<object>));

                var info = new WebSocketMessageInfo
                {
                    MessageType = stub.MessageType,
                    Data = stub.Data?.ToString(),
                    Connection = this
                };

                OnReceive(info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing web socket message");
            }
        }

        /// <summary>
        /// Sends a message asynchronously.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">message</exception>
        public Task SendAsync<T>(WebSocketMessage<T> message, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var json = _jsonSerializer.SerializeToString(message);

            return SendAsync(json, cancellationToken);
        }

        /// <summary>
        /// Sends a message asynchronously.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendAsync(byte[] buffer, CancellationToken cancellationToken)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            cancellationToken.ThrowIfCancellationRequested();

            return _socket.SendAsync(buffer, true, cancellationToken);
        }

        /// <inheritdoc />
        public Task SendAsync(string text, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException(nameof(text));
            }

            cancellationToken.ThrowIfCancellationRequested();

            return _socket.SendAsync(text, true, cancellationToken);
        }

        /// <inheritdoc />
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
                _socket.Dispose();
            }
        }
    }
}
