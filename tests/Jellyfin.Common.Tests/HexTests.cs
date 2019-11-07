using MediaBrowser.Common;
using Xunit;

namespace Jellyfin.Common.Tests
{
    public class HexTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("00")]
        [InlineData("01")]
        [InlineData("000102030405060708090a0b0c0d0e0f")]
        [InlineData("0123456789abcdef")]
        public void RoundTripTest(string data)
        {
            Assert.Equal(data, Hex.Encode(Hex.Decode(data)));
        }
    }
}
