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
    }
}
