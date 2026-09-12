using System;
using System.Buffers;
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
        public async Task ReceiveAsync_SocketTornDownWhileAnswering_RaisesClosedWithoutThrowing()
        {
            // The keep-alive watchdog can dispose a connection while the receive loop is
            // answering a message on it. The failing answer must not escape into the request
            // handler, as that would skip the Closed event the session needs to release it.
            var socket = new DisposedOnSendWebSocket(Encoding.UTF8.GetBytes("{\"MessageType\":\"KeepAlive\"}"));
            var con = new WebSocketConnection(new NullLogger<WebSocketConnection>(), socket, null!, null!)
            {
                OnReceive = _ => Task.CompletedTask
            };

            var closed = false;
            con.Closed += (_, _) => closed = true;

            await con.ReceiveAsync(TestContext.Current.CancellationToken);

            Assert.True(closed);
            Assert.Equal(1, socket.SendAttempts);
        }

        /// <summary>
        /// A socket that hands out a single message and then behaves like a socket that was
        /// disposed underneath the receive loop.
        /// </summary>
        internal sealed class DisposedOnSendWebSocket : WebSocket
        {
            private readonly byte[] _message;
            private bool _received;

            public DisposedOnSendWebSocket(byte[] message)
            {
                _message = message;
            }

            public int SendAttempts { get; private set; }

            public override WebSocketCloseStatus? CloseStatus => null;

            public override string? CloseStatusDescription => null;

            public override string? SubProtocol => null;

            public override WebSocketState State => SendAttempts == 0 ? WebSocketState.Open : WebSocketState.Closed;

            public override void Abort()
            {
            }

            public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public override void Dispose()
            {
            }

            public override ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                ObjectDisposedException.ThrowIf(_received, this);

                _received = true;
                _message.CopyTo(buffer);
                return ValueTask.FromResult(new ValueWebSocketReceiveResult(_message.Length, WebSocketMessageType.Text, true));
            }

            public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
                => throw new NotImplementedException();

            public override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
                => throw FailSend();

            public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
                => throw FailSend();

            private WebSocketException FailSend()
            {
                SendAttempts++;
                return new WebSocketException(
                    WebSocketError.InvalidState,
                    "The WebSocket is in an invalid state ('Closed') for this operation. Valid states are: 'Open, CloseReceived'");
            }
        }

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
    }
}
