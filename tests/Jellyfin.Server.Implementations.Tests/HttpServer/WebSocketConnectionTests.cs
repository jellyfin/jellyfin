using System;
using System.Buffers;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json;
using Emby.Server.Implementations.HttpServer;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.HttpServer
{
    public class WebSocketConnectionTests
    {
        [Fact]
        public void DeserializeWebSocketMessage_SingleSegment_Success()
        {
            using var con = GetTestWebSocketConnection();
            var bytes = File.ReadAllBytes("Test Data/HttpServer/ForceKeepAlive.json");
            con.DeserializeWebSocketMessage(new ReadOnlySequence<byte>(bytes), out var bytesConsumed);
            Assert.Equal(109, bytesConsumed);
        }

        [Fact]
        public void DeserializeWebSocketMessage_MultipleSegments_Success()
        {
            const int SplitPos = 64;
            using var con = GetTestWebSocketConnection();
            var bytes = File.ReadAllBytes("Test Data/HttpServer/ForceKeepAlive.json");
            var seg1 = new BufferSegment(new Memory<byte>(bytes, 0, SplitPos));
            var seg2 = seg1.Append(new Memory<byte>(bytes, SplitPos, bytes.Length - SplitPos));
            con.DeserializeWebSocketMessage(new ReadOnlySequence<byte>(seg1, 0, seg2, seg2.Memory.Length - 1), out var bytesConsumed);
            Assert.Equal(109, bytesConsumed);
        }

        [Fact]
        public void DeserializeWebSocketMessage_ValidPartial_Success()
        {
            using var con = GetTestWebSocketConnection();
            var bytes = File.ReadAllBytes("Test Data/HttpServer/ValidPartial.json");
            con.DeserializeWebSocketMessage(new ReadOnlySequence<byte>(bytes), out var bytesConsumed);
            Assert.Equal(109, bytesConsumed);
        }

        [Fact]
        public void DeserializeWebSocketMessage_Partial_ThrowJsonException()
        {
            using var con = GetTestWebSocketConnection();
            var bytes = File.ReadAllBytes("Test Data/HttpServer/Partial.json");
            Assert.Throws<JsonException>(() => con.DeserializeWebSocketMessage(new ReadOnlySequence<byte>(bytes), out var bytesConsumed));
        }

        private static WebSocketConnection GetTestWebSocketConnection()
        {
            var socket = new Mock<WebSocket>();
            return new WebSocketConnection(new NullLogger<WebSocketConnection>(), socket.Object, null!, null!);
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
