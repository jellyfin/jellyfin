using System.Text;
using MediaBrowser.Common.Events;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.IO;
using UniversalDetector;

namespace MediaBrowser.Server.Implementations.ServerManager
{
    /// <summary>
    /// Class WebSocketConnection
    /// </summary>
    public class WebSocketConnection : IWebSocketConnection
    {
        public event EventHandler<EventArgs> Closed;

        /// <summary>
        /// The _socket
        /// </summary>
        private readonly IWebSocket _socket;

        /// <summary>
        /// The _remote end point
        /// </summary>
        public string RemoteEndPoint { get; private set; }

        /// <summary>
        /// The _cancellation token source
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The _json serializer
        /// </summary>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// Gets or sets the receive action.
        /// </summary>
        /// <value>The receive action.</value>
        public Action<WebSocketMessageInfo> OnReceive { get; set; }

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
        public NameValueCollection QueryString { get; set; }
        private readonly IMemoryStreamProvider _memoryStreamProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketConnection" /> class.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentNullException">socket</exception>
        public WebSocketConnection(IWebSocket socket, string remoteEndPoint, IJsonSerializer jsonSerializer, ILogger logger, IMemoryStreamProvider memoryStreamProvider)
        {
            if (socket == null)
            {
                throw new ArgumentNullException("socket");
            }
            if (string.IsNullOrEmpty(remoteEndPoint))
            {
                throw new ArgumentNullException("remoteEndPoint");
            }
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            Id = Guid.NewGuid();
            _jsonSerializer = jsonSerializer;
            _socket = socket;
            _socket.OnReceiveBytes = OnReceiveInternal;
            _socket.OnReceive = OnReceiveInternal;
            RemoteEndPoint = remoteEndPoint;
            _logger = logger;
            _memoryStreamProvider = memoryStreamProvider;

            socket.Closed += socket_Closed;
        }

        void socket_Closed(object sender, EventArgs e)
        {
            EventHelper.FireEventIfNotNull(Closed, this, EventArgs.Empty, _logger);
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
            var charset = DetectCharset(bytes);

            if (string.Equals(charset, "utf-8", StringComparison.OrdinalIgnoreCase))
            {
                OnReceiveInternal(Encoding.UTF8.GetString(bytes));
            }
            else
            {
                OnReceiveInternal(Encoding.ASCII.GetString(bytes));
            }
        }
        private string DetectCharset(byte[] bytes)
        {
            try
            {
                using (var ms = _memoryStreamProvider.CreateNew(bytes))
                {
                    var detector = new CharsetDetector();
                    detector.Feed(ms);
                    detector.DataEnd();

                    var charset = detector.Charset;

                    if (!string.IsNullOrWhiteSpace(charset))
                    {
                        //_logger.Debug("UniversalDetector detected charset {0}", charset);
                    }

                    return charset;
                }
            }
            catch (IOException ex)
            {
                _logger.ErrorException("Error attempting to determine web socket message charset", ex);
            }

            return null;
        }

        private void OnReceiveInternal(string message)
        {
            LastActivityDate = DateTime.UtcNow;

            if (!message.StartsWith("{", StringComparison.OrdinalIgnoreCase))
            {
                // This info is useful sometimes but also clogs up the log
                //_logger.Error("Received web socket message that is not a json structure: " + message);
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
                    Data = stub.Data == null ? null : stub.Data.ToString(),
                    Connection = this
                };

                OnReceive(info);
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
                throw new ArgumentNullException("buffer");
            }

            cancellationToken.ThrowIfCancellationRequested();

            return _socket.SendAsync(buffer, true, cancellationToken);
        }

        public Task SendAsync(string text, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentNullException("text");
            }

            cancellationToken.ThrowIfCancellationRequested();

            return _socket.SendAsync(text, true, cancellationToken);
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
}
