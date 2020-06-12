#nullable enable

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Json;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Http;
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
        private readonly ILogger _logger;

        /// <summary>
        /// The json serializer options.
        /// </summary>
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// The socket.
        /// </summary>
        private readonly WebSocket _socket;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketConnection" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="socket">The socket.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="query">The query.</param>
        public WebSocketConnection(
            ILogger<WebSocketConnection> logger,
            WebSocket socket,
            IPAddress? remoteEndPoint,
            IQueryCollection query)
        {
            _logger = logger;
            _socket = socket;
            RemoteEndPoint = remoteEndPoint;
            QueryString = query;

            _jsonOptions = JsonDefaults.GetOptions();
            LastActivityDate = DateTime.Now;
        }

        /// <inheritdoc />
        public event EventHandler<EventArgs>? Closed;

        /// <summary>
        /// Gets or sets the remote end point.
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
        /// Gets or sets the query string.
        /// </summary>
        /// <value>The query string.</value>
        public IQueryCollection QueryString { get; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>The state.</value>
        public WebSocketState State => _socket.State;

        /// <summary>
        /// Sends a message asynchronously.
        /// </summary>
        /// <typeparam name="T"></typeparam>
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
                    receiveresult = await _socket.ReceiveAsync(memory, cancellationToken);
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
                FlushResult flushResult = await writer.FlushAsync();
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
            } while (
                (_socket.State == WebSocketState.Open || _socket.State == WebSocketState.Connecting)
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

            WebSocketMessage<object> stub;
            try
            {

                if (buffer.IsSingleSegment)
                {
                    stub = JsonSerializer.Deserialize<WebSocketMessage<object>>(buffer.FirstSpan, _jsonOptions);
                }
                else
                {
                    var buf = ArrayPool<byte>.Shared.Rent(Convert.ToInt32(buffer.Length));
                    try
                    {
                        buffer.CopyTo(buf);
                        stub = JsonSerializer.Deserialize<WebSocketMessage<object>>(buf, _jsonOptions);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buf);
                    }
                }
            }
            catch (JsonException ex)
            {
                // Tell the PipeReader how much of the buffer we have consumed
                reader.AdvanceTo(buffer.End);
                _logger.LogError(ex, "Error processing web socket message");
                return;
            }

            // Tell the PipeReader how much of the buffer we have consumed
            reader.AdvanceTo(buffer.End);

            _logger.LogDebug("WS {IP} received message: {@Message}", RemoteEndPoint, stub);

            var info = new WebSocketMessageInfo
            {
                MessageType = stub.MessageType,
                Data = stub.Data?.ToString(), // Data can be null
                Connection = this
            };

            if (info.MessageType.Equals("KeepAlive", StringComparison.Ordinal))
            {
                await SendKeepAliveResponse();
            }
            else
            {
                await OnReceive(info).ConfigureAwait(false);
            }
        }

        private Task SendKeepAliveResponse()
        {
            LastKeepAliveDate = DateTime.UtcNow;
            return SendAsync(new WebSocketMessage<string>
            {
                MessageType = "KeepAlive"
            }, CancellationToken.None);
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
