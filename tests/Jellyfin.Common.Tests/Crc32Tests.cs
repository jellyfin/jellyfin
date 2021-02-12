using System;
using System.Text;
using MediaBrowser.Common;
using Xunit;

namespace Jellyfin.Common.Tests
{
    public static class Crc32Tests
    {
        [Fact]
        public static void Compute_Empty_Zero()
        {
            Assert.Equal<uint>(0, Crc32.Compute(Array.Empty<byte>()));
        }

        [Theory]
        [InlineData(0x414fa339, "The quick brown fox jumps over the lazy dog")]
        public static void Compute_Valid_Success(uint expected, string data)
        {
            Assert.Equal(expected, Crc32.Compute(Encoding.UTF8.GetBytes(data)));
        }
    }
}
