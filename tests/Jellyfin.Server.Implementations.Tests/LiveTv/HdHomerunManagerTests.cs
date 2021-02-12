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

            Assert.Equal(expected.Length, len);
            Assert.True(expected.SequenceEqual(buffer.Slice(0, len)));
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

            Assert.Equal(expected.Length, len);
            Assert.True(expected.SequenceEqual(buffer.Slice(0, len)));
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

            Assert.Equal(expected.Length, len);
            Assert.True(expected.SequenceEqual(buffer.Slice(0, len)));
        }
    }
}
