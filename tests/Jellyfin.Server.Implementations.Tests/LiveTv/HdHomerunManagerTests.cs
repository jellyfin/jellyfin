using System;
using Emby.Server.Implementations.LiveTv.TunerHosts.HdHomerun;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.LiveTv
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
    }
}
