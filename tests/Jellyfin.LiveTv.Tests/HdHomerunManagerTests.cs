using System;
using System.Text;
using Jellyfin.LiveTv.TunerHosts.HdHomerun;
using Xunit;

namespace Jellyfin.LiveTv.Tests
{
    public class HdHomerunManagerTests
    {
        [Fact]
        public void WriteNullTerminatedString_Empty_Success()
        {
            ReadOnlySpan<byte> expected = stackalloc byte[]
            {
                1, 0
            };

            Span<byte> buffer = stackalloc byte[128];
            int len = HdHomerunManager.WriteNullTerminatedString(buffer, string.Empty);

            Assert.Equal(
                Convert.ToHexString(expected),
                Convert.ToHexString(buffer.Slice(0, len)));
        }

        [Fact]
        public void WriteNullTerminatedString_Valid_Success()
        {
            ReadOnlySpan<byte> expected = stackalloc byte[]
            {
                10, (byte)'T', (byte)'h', (byte)'e', (byte)' ', (byte)'q', (byte)'u', (byte)'i', (byte)'c', (byte)'k', 0
            };

            Span<byte> buffer = stackalloc byte[128];
            int len = HdHomerunManager.WriteNullTerminatedString(buffer, "The quick");

            Assert.Equal(
                Convert.ToHexString(expected),
                Convert.ToHexString(buffer.Slice(0, len)));
        }

        [Fact]
        public void WriteGetMessage_Valid_Success()
        {
            ReadOnlySpan<byte> expected = stackalloc byte[]
            {
                0, 4,
                0, 12,
                3,
                10, (byte)'/', (byte)'t', (byte)'u', (byte)'n', (byte)'e', (byte)'r', (byte)'0', (byte)'/', (byte)'N', 0,
                0xc0, 0xc9, 0x87, 0x33
            };

            Span<byte> buffer = stackalloc byte[128];
            int len = HdHomerunManager.WriteGetMessage(buffer, 0, "N");

            Assert.Equal(
                Convert.ToHexString(expected),
                Convert.ToHexString(buffer.Slice(0, len)));
        }

        [Fact]
        public void WriteSetMessage_NoLockKey_Success()
        {
            ReadOnlySpan<byte> expected = stackalloc byte[]
            {
                0, 4,
                0, 20,
                3,
                10, (byte)'/', (byte)'t', (byte)'u', (byte)'n', (byte)'e', (byte)'r', (byte)'0', (byte)'/', (byte)'N', 0,
                4,
                6, (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 0,
                0xa9, 0x49, 0xd0, 0x68
            };

            Span<byte> buffer = stackalloc byte[128];
            int len = HdHomerunManager.WriteSetMessage(buffer, 0, "N", "value", null);

            Assert.Equal(
                Convert.ToHexString(expected),
                Convert.ToHexString(buffer.Slice(0, len)));
        }

        [Fact]
        public void WriteSetMessage_LockKey_Success()
        {
            ReadOnlySpan<byte> expected = stackalloc byte[]
            {
                0, 4,
                0, 26,
                3,
                10, (byte)'/', (byte)'t', (byte)'u', (byte)'n', (byte)'e', (byte)'r', (byte)'0', (byte)'/', (byte)'N', 0,
                4,
                6, (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 0,
                21,
                4, 0x00, 0x01, 0x38, 0xd5,
                0x8e, 0xb6, 0x06, 0x82
            };

            Span<byte> buffer = stackalloc byte[128];
            int len = HdHomerunManager.WriteSetMessage(buffer, 0, "N", "value", 80085);

            Assert.Equal(
                Convert.ToHexString(expected),
                Convert.ToHexString(buffer.Slice(0, len)));
        }

        [Fact]
        public void TryGetReturnValueOfGetSet_Valid_Success()
        {
            ReadOnlySpan<byte> packet = new byte[]
            {
                0, 5,
                0, 20,
                3,
                10, (byte)'/', (byte)'t', (byte)'u', (byte)'n', (byte)'e', (byte)'r', (byte)'0', (byte)'/', (byte)'N', 0,
                4,
                6, (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 0,
                0x7d, 0xa3, 0xa3, 0xf3
            };

            Assert.True(HdHomerunManager.TryGetReturnValueOfGetSet(packet, out var value));
            Assert.Equal("value", Encoding.UTF8.GetString(value));
        }

        [Fact]
        public void TryGetReturnValueOfGetSet_InvalidCrc_False()
        {
            ReadOnlySpan<byte> packet = new byte[]
            {
                0, 5,
                0, 20,
                3,
                10, (byte)'/', (byte)'t', (byte)'u', (byte)'n', (byte)'e', (byte)'r', (byte)'0', (byte)'/', (byte)'N', 0,
                4,
                6, (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 0,
                0x7d, 0xa3, 0xa3, 0xf4
            };

            Assert.False(HdHomerunManager.TryGetReturnValueOfGetSet(packet, out _));
        }

        [Fact]
        public void TryGetReturnValueOfGetSet_InvalidPacketType_False()
        {
            ReadOnlySpan<byte> packet = new byte[]
            {
                0, 4,
                0, 20,
                3,
                10, (byte)'/', (byte)'t', (byte)'u', (byte)'n', (byte)'e', (byte)'r', (byte)'0', (byte)'/', (byte)'N', 0,
                4,
                6, (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 0,
                0xa9, 0x49, 0xd0, 0x68
            };

            Assert.False(HdHomerunManager.TryGetReturnValueOfGetSet(packet, out _));
        }

        [Fact]
        public void TryGetReturnValueOfGetSet_InvalidPacket_False()
        {
            ReadOnlySpan<byte> packet = new byte[]
            {
                0, 5,
                0, 20,
                0x7d, 0xa3, 0xa3
            };

            Assert.False(HdHomerunManager.TryGetReturnValueOfGetSet(packet, out _));
        }

        [Fact]
        public void TryGetReturnValueOfGetSet_TooSmallMessageLength_False()
        {
            ReadOnlySpan<byte> packet = new byte[]
            {
                0, 5,
                0, 19,
                3,
                10, (byte)'/', (byte)'t', (byte)'u', (byte)'n', (byte)'e', (byte)'r', (byte)'0', (byte)'/', (byte)'N', 0,
                4,
                6, (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 0,
                0x25, 0x25, 0x44, 0x9a
            };

            Assert.False(HdHomerunManager.TryGetReturnValueOfGetSet(packet, out _));
        }

        [Fact]
        public void TryGetReturnValueOfGetSet_TooLargeMessageLength_False()
        {
            ReadOnlySpan<byte> packet = new byte[]
            {
                0, 5,
                0, 21,
                3,
                10, (byte)'/', (byte)'t', (byte)'u', (byte)'n', (byte)'e', (byte)'r', (byte)'0', (byte)'/', (byte)'N', 0,
                4,
                6, (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 0,
                0xe3, 0x20, 0x79, 0x6c
            };

            Assert.False(HdHomerunManager.TryGetReturnValueOfGetSet(packet, out _));
        }

        [Fact]
        public void TryGetReturnValueOfGetSet_TooLargeNameLength_False()
        {
            ReadOnlySpan<byte> packet = new byte[]
            {
                0, 5,
                0, 20,
                3,
                20, (byte)'/', (byte)'t', (byte)'u', (byte)'n', (byte)'e', (byte)'r', (byte)'0', (byte)'/', (byte)'N', 0,
                4,
                6, (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 0,
                0xe1, 0x8e, 0x9c, 0x74
            };

            Assert.False(HdHomerunManager.TryGetReturnValueOfGetSet(packet, out _));
        }

        [Fact]
        public void TryGetReturnValueOfGetSet_InvalidGetSetNameTag_False()
        {
            ReadOnlySpan<byte> packet = new byte[]
            {
                0, 5,
                0, 20,
                4,
                10, (byte)'/', (byte)'t', (byte)'u', (byte)'n', (byte)'e', (byte)'r', (byte)'0', (byte)'/', (byte)'N', 0,
                4,
                6, (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 0,
                0xee, 0x05, 0xe7, 0x12
            };

            Assert.False(HdHomerunManager.TryGetReturnValueOfGetSet(packet, out _));
        }

        [Fact]
        public void TryGetReturnValueOfGetSet_InvalidGetSetValueTag_False()
        {
            ReadOnlySpan<byte> packet = new byte[]
            {
                0, 5,
                0, 20,
                3,
                10, (byte)'/', (byte)'t', (byte)'u', (byte)'n', (byte)'e', (byte)'r', (byte)'0', (byte)'/', (byte)'N', 0,
                3,
                6, (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 0,
                0x64, 0xaa, 0x66, 0xf9
            };

            Assert.False(HdHomerunManager.TryGetReturnValueOfGetSet(packet, out _));
        }

        [Fact]
        public void TryGetReturnValueOfGetSet_TooLargeValueLength_False()
        {
            ReadOnlySpan<byte> packet = new byte[]
            {
                0, 5,
                0, 20,
                3,
                10, (byte)'/', (byte)'t', (byte)'u', (byte)'n', (byte)'e', (byte)'r', (byte)'0', (byte)'/', (byte)'N', 0,
                4,
                7, (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 0,
                0xc9, 0xa8, 0xd4, 0x55
            };

            Assert.False(HdHomerunManager.TryGetReturnValueOfGetSet(packet, out _));
        }

        [Fact]
        public void VerifyReturnValueOfGetSet_Valid_True()
        {
            ReadOnlySpan<byte> packet = new byte[]
            {
                0, 5,
                0, 20,
                3,
                10, (byte)'/', (byte)'t', (byte)'u', (byte)'n', (byte)'e', (byte)'r', (byte)'0', (byte)'/', (byte)'N', 0,
                4,
                6, (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 0,
                0x7d, 0xa3, 0xa3, 0xf3
            };

            Assert.True(HdHomerunManager.VerifyReturnValueOfGetSet(packet, "value"));
        }

        [Fact]
        public void VerifyReturnValueOfGetSet_WrongValue_False()
        {
            ReadOnlySpan<byte> packet = new byte[]
            {
                0, 5,
                0, 20,
                3,
                10, (byte)'/', (byte)'t', (byte)'u', (byte)'n', (byte)'e', (byte)'r', (byte)'0', (byte)'/', (byte)'N', 0,
                4,
                6, (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 0,
                0x7d, 0xa3, 0xa3, 0xf3
            };

            Assert.False(HdHomerunManager.VerifyReturnValueOfGetSet(packet, "none"));
        }

        [Fact]
        public void VerifyReturnValueOfGetSet_InvalidPacket_False()
        {
            ReadOnlySpan<byte> packet = new byte[]
            {
                0, 4,
                0, 20,
                3,
                10, (byte)'/', (byte)'t', (byte)'u', (byte)'n', (byte)'e', (byte)'r', (byte)'0', (byte)'/', (byte)'N', 0,
                4,
                6, (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e', 0,
                0x7d, 0xa3, 0xa3, 0xf3
            };

            Assert.False(HdHomerunManager.VerifyReturnValueOfGetSet(packet, "value"));
        }
    }
}
