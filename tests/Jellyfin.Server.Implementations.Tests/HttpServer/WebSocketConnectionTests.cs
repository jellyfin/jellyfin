using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.HttpServer;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.HttpServer
{
    public class WebSocketConnectionTests
    {
        [Fact]
        public void DeserializeWebSocketMessage_SingleSegment_Success()
        {
            var con = new WebSocketConnection(new NullLogger<WebSocketConnection>(), null!, null!, null!);
            var bytes = File.ReadAllBytes("Test Data/HttpServer/ForceKeepAlive.json");
            con.DeserializeWebSocketMessage(new ReadOnlySequence<byte>(bytes), out var bytesConsumed);
            Assert.Equal(109, bytesConsumed);
        }

        [Fact]
        public void DeserializeWebSocketMessage_MultipleSegments_Success()
        {
            const int SplitPos = 64;
            var con = new WebSocketConnection(new NullLogger<WebSocketConnection>(), null!, null!, null!);
            var bytes = File.ReadAllBytes("Test Data/HttpServer/ForceKeepAlive.json");
            var seg1 = new BufferSegment(new Memory<byte>(bytes, 0, SplitPos));
            var seg2 = seg1.Append(new Memory<byte>(bytes, SplitPos, bytes.Length - SplitPos));
            con.DeserializeWebSocketMessage(new ReadOnlySequence<byte>(seg1, 0, seg2, seg2.Memory.Length - 1), out var bytesConsumed);
            Assert.Equal(109, bytesConsumed);
        }

        [Fact]
        public void DeserializeWebSocketMessage_ValidPartial_Success()
        {
            var con = new WebSocketConnection(new NullLogger<WebSocketConnection>(), null!, null!, null!);
            var bytes = File.ReadAllBytes("Test Data/HttpServer/ValidPartial.json");
            con.DeserializeWebSocketMessage(new ReadOnlySequence<byte>(bytes), out var bytesConsumed);
            Assert.Equal(109, bytesConsumed);
        }

        [Fact]
        public void DeserializeWebSocketMessage_Partial_ThrowJsonException()
        {
            var con = new WebSocketConnection(new NullLogger<WebSocketConnection>(), null!, null!, null!);
            var bytes = File.ReadAllBytes("Test Data/HttpServer/Partial.json");
            Assert.Throws<JsonException>(() => con.DeserializeWebSocketMessage(new ReadOnlySequence<byte>(bytes), out var bytesConsumed));
        }

        [Fact]
        public async Task ReceiveAsync_OversizedMessage_ClosesInsteadOfBlocking()
        {
            var socket = new ScriptedWebSocket(Message(new string('a', 128 * 1024)));
            var received = 0;
            var con = new WebSocketConnection(new NullLogger<WebSocketConnection>(), socket, null!, null!)
            {
                OnReceive = _ =>
                {
                    received++;
                    return Task.CompletedTask;
                }
            };

            var receive = con.ReceiveAsync(TestContext.Current.CancellationToken);
            var finished = await Task.WhenAny(receive, Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)).ConfigureAwait(true);

            Assert.Same(receive, finished);
            await receive.ConfigureAwait(true);
            Assert.Equal(WebSocketCloseStatus.MessageTooBig, socket.CloseStatusSent);
            Assert.Equal(0, received);
        }

        [Fact]
        public async Task ReceiveAsync_NullMessage_KeepsProcessingLaterMessages()
        {
            var socket = new ScriptedWebSocket(Raw("null"), Message("payload"));
            var received = 0;
            var con = new WebSocketConnection(new NullLogger<WebSocketConnection>(), socket, null!, null!)
            {
                OnReceive = _ =>
                {
                    received++;
                    return Task.CompletedTask;
                }
            };

            await con.ReceiveAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal(1, received);
        }

        [Fact]
        public async Task ReceiveAsync_UnhandledException_StillRaisesClosed()
        {
            var socket = new ScriptedWebSocket(Message("payload"))
            {
                ReceiveException = new InvalidOperationException("receive failed")
            };
            var con = new WebSocketConnection(new NullLogger<WebSocketConnection>(), socket, null!, null!);
            var closed = 0;
            con.Closed += (_, _) => closed++;

            await Assert.ThrowsAsync<InvalidOperationException>(() => con.ReceiveAsync(TestContext.Current.CancellationToken)).ConfigureAwait(true);

            Assert.Equal(1, closed);
        }

        [Fact]
        public async Task DisposeAsync_CloseOutputFails_StillDisposesSocket()
        {
            var socket = new ScriptedWebSocket
            {
                CloseOutputException = new WebSocketException(WebSocketError.ConnectionClosedPrematurely)
            };
            var con = new WebSocketConnection(new NullLogger<WebSocketConnection>(), socket, null!, null!);

            await con.DisposeAsync().ConfigureAwait(true);

            Assert.True(socket.IsDisposed);
        }

        private static byte[] Message(string data)
            => Raw($"{{\"MessageType\":\"SessionsStart\",\"Data\":\"{data}\"}}");

        private static byte[] Raw(string json)
            => Encoding.UTF8.GetBytes(json);

        internal sealed class BufferSegment : ReadOnlySequenceSegment<byte>
        {
            public BufferSegment(Memory<byte> memory)
            {
                Memory = memory;
            }

            public BufferSegment Append(Memory<byte> memory)
            {
                var segment = new BufferSegment(memory)
                {
                    RunningIndex = RunningIndex + Memory.Length
                };
                Next = segment;
                return segment;
            }
        }

        internal sealed class ScriptedWebSocket : WebSocket
        {
            private readonly IReadOnlyList<byte[]> _messages;
            private WebSocketState _state = WebSocketState.Open;
            private int _message;
            private int _offset;

            public ScriptedWebSocket(params byte[][] messages)
            {
                _messages = messages;
            }

            public Exception? ReceiveException { get; set; }

            public Exception? CloseOutputException { get; set; }

            public bool IsDisposed { get; private set; }

            public WebSocketCloseStatus? CloseStatusSent { get; private set; }

            public override WebSocketCloseStatus? CloseStatus => null;

            public override string? CloseStatusDescription => null;

            public override string? SubProtocol => null;

            public override WebSocketState State => _state;

            public override void Abort() => _state = WebSocketState.Aborted;

            public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
            {
                CloseStatusSent = closeStatus;
                _state = WebSocketState.Closed;
                return Task.CompletedTask;
            }

            public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
            {
                if (CloseOutputException is not null)
                {
                    return Task.FromException(CloseOutputException);
                }

                CloseStatusSent = closeStatus;
                _state = WebSocketState.CloseSent;
                return Task.CompletedTask;
            }

            public override void Dispose()
            {
                IsDisposed = true;
                _state = WebSocketState.Closed;
            }

            public override ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                if (ReceiveException is not null)
                {
                    return ValueTask.FromException<ValueWebSocketReceiveResult>(ReceiveException);
                }

                if (_message >= _messages.Count)
                {
                    _state = WebSocketState.CloseReceived;
                    return ValueTask.FromResult(new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true));
                }

                var payload = _messages[_message];
                var count = Math.Min(payload.Length - _offset, buffer.Length);
                payload.AsSpan(_offset, count).CopyTo(buffer.Span);
                _offset += count;

                var endOfMessage = _offset == payload.Length;
                if (endOfMessage)
                {
                    _message++;
                    _offset = 0;
                }

                return ValueTask.FromResult(new ValueWebSocketReceiveResult(count, WebSocketMessageType.Text, endOfMessage));
            }

            public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
                => throw new NotSupportedException();

            public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
                => ValueTask.CompletedTask;
        }
    }
}
