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

        [Theory]
        [InlineData(0x414fa339, "54686520717569636B2062726F776E20666F78206A756D7073206F76657220746865206C617A7920646F67")]
        [InlineData(0x190a55ad, "0000000000000000000000000000000000000000000000000000000000000000")]
        [InlineData(0xff6cab0b, "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF")]
        [InlineData(0x91267e8a, "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F")]
        public static void Compute_ValidHex_Success(uint expected, string data)
        {
            Assert.Equal(expected, Crc32.Compute(Convert.FromHexString(data)));
        }
    }
}
