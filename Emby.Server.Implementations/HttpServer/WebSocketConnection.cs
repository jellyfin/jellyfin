using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<WebSocketConnection> _logger;

        /// <summary>
        /// The json serializer options.
        /// </summary>
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// The socket.
        /// </summary>
        private readonly WebSocket _socket;

        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketConnection" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="socket">The socket.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        public WebSocketConnection(
            ILogger<WebSocketConnection> logger,
            WebSocket socket,
            IPAddress? remoteEndPoint)
        {
            _logger = logger;
            _socket = socket;
            RemoteEndPoint = remoteEndPoint;

            _jsonOptions = JsonDefaults.Options;
            LastActivityDate = DateTime.Now;
        }

        /// <inheritdoc />
        public event EventHandler<EventArgs>? Closed;

        /// <summary>
        /// Gets the remote end point.
        /// </summary>
        public IPAddress? RemoteEndPoint { get; }

        /// <summary>
        /// Gets or sets the receive action.
        /// </summary>
        /// <value>The receive action.</value>
        public Func<WebSocketMessageInfo, Task>? OnReceive { get; set; }

        /// <summary>
        /// Gets the last activity date.
        /// </summary>
        /// <value>The last activity date.</value>
        public DateTime LastActivityDate { get; private set; }

        /// <inheritdoc />
        public DateTime LastKeepAliveDate { get; set; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>The state.</value>
        public WebSocketState State => _socket.State;

        /// <summary>
        /// Sends a message asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SendAsync<T>(WebSocketMessage<T> message, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);
            return _socket.SendAsync(json, WebSocketMessageType.Text, true, cancellationToken);
        }

        /// <inheritdoc />
        public async Task ProcessAsync(CancellationToken cancellationToken = default)
        {
            var pipe = new Pipe();
            var writer = pipe.Writer;

            ValueWebSocketReceiveResult receiveresult;
            do
            {
                // Allocate at least 512 bytes from the PipeWriter
                Memory<byte> memory = writer.GetMemory(512);
                try
                {
                    receiveresult = await _socket.ReceiveAsync(memory, cancellationToken).ConfigureAwait(false);
                }
                catch (WebSocketException ex)
                {
                    _logger.LogWarning("WS {IP} error receiving data: {Message}", RemoteEndPoint, ex.Message);
                    break;
                }

                int bytesRead = receiveresult.Count;
                if (bytesRead == 0)
                {
                    break;
                }

                // Tell the PipeWriter how much was read from the Socket
                writer.Advance(bytesRead);

                // Make the data available to the PipeReader
                FlushResult flushResult = await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                if (flushResult.IsCompleted)
                {
                    // The PipeReader stopped reading
                    break;
                }

                LastActivityDate = DateTime.UtcNow;

                if (receiveresult.EndOfMessage)
                {
                    await ProcessInternal(pipe.Reader).ConfigureAwait(false);
                }
            }
            while ((_socket.State == WebSocketState.Open || _socket.State == WebSocketState.Connecting)
                && receiveresult.MessageType != WebSocketMessageType.Close);

            Closed?.Invoke(this, EventArgs.Empty);

            if (_socket.State == WebSocketState.Open
                || _socket.State == WebSocketState.CloseReceived
                || _socket.State == WebSocketState.CloseSent)
            {
                await _socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    string.Empty,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ProcessInternal(PipeReader reader)
        {
            ReadResult result = await reader.ReadAsync().ConfigureAwait(false);
            ReadOnlySequence<byte> buffer = result.Buffer;

            if (OnReceive == null)
            {
                // Tell the PipeReader how much of the buffer we have consumed
                reader.AdvanceTo(buffer.End);
                return;
            }

            WebSocketMessage<object>? stub;
            long bytesConsumed;
            try
            {
                stub = DeserializeWebSocketMessage(buffer, out bytesConsumed);
            }
            catch (JsonException ex)
            {
                // Tell the PipeReader how much of the buffer we have consumed
                reader.AdvanceTo(buffer.End);
                _logger.LogError(ex, "Error processing web socket message: {Data}", Encoding.UTF8.GetString(buffer));
                return;
            }

            if (stub == null)
            {
                _logger.LogError("Error processing web socket message");
                return;
            }

            // Tell the PipeReader how much of the buffer we have consumed
            reader.AdvanceTo(buffer.GetPosition(bytesConsumed));

            _logger.LogDebug("WS {IP} received message: {@Message}", RemoteEndPoint, stub);

            if (stub.MessageType == SessionMessageType.KeepAlive)
            {
                await SendKeepAliveResponse().ConfigureAwait(false);
            }
            else
            {
                await OnReceive(
                    new WebSocketMessageInfo
                    {
                        MessageType = stub.MessageType,
                        Data = stub.Data?.ToString(), // Data can be null
                        Connection = this
                    }).ConfigureAwait(false);
            }
        }

        internal WebSocketMessage<object>? DeserializeWebSocketMessage(ReadOnlySequence<byte> bytes, out long bytesConsumed)
        {
            var jsonReader = new Utf8JsonReader(bytes);
            var ret = JsonSerializer.Deserialize<WebSocketMessage<object>>(ref jsonReader, _jsonOptions);
            bytesConsumed = jsonReader.BytesConsumed;
            return ret;
        }

        private Task SendKeepAliveResponse()
        {
            LastKeepAliveDate = DateTime.UtcNow;
            return SendAsync(
                new WebSocketMessage<string>
                {
                    MessageId = Guid.NewGuid(),
                    MessageType = SessionMessageType.KeepAlive
                },
                CancellationToken.None);
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
            if (_disposed)
            {
                return;
            }

            if (dispose)
            {
                _socket.Dispose();
            }

            _disposed = true;
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Used to perform asynchronous cleanup of managed resources or for cascading calls to <see cref="DisposeAsync"/>.
        /// </summary>
        /// <returns>A ValueTask.</returns>
        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_socket.State == WebSocketState.Open)
            {
                await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "System Shutdown", CancellationToken.None).ConfigureAwait(false);
            }

            _socket.Dispose();
        }
    }
}
