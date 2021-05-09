using Jellyfin.Networking.Udp;
using Xunit;

namespace Jellyfin.Networking.Tests
{
    /// <summary>
    /// Defines the tests for parsing udp port strings.
    /// </summary>
    public class UdpPortParsing
    {
        /// <summary>
        /// Checks the parsing of ranges.
        /// </summary>
        /// <param name="rangeStr">The test string to parse.</param>
        [Theory]
        [InlineData("65536")]
        [InlineData("-1-65536")]
        [InlineData("-1--2")]
        [InlineData("Rodger")]
        [InlineData("65535-1")]
        public void Invalid_Ranges(string rangeStr)
        {
            Assert.False(rangeStr.TryParseRange(out var _));
        }

        [Theory]
        [InlineData("-1", UdpHelper.UDPMinPort, UdpHelper.UDPMinPort)]
        [InlineData("-65535", UdpHelper.UDPLowerUserPort, UdpHelper.UDPMaxPort)]
        [InlineData("1-", UdpHelper.UDPMinPort, UdpHelper.UDPMaxPort)]
        [InlineData("1-65535", 1, UdpHelper.UDPMaxPort)]
        [InlineData("10-65535", 10, UdpHelper.UDPMaxPort)]
        [InlineData("1-655", 1, 655)]
        [InlineData("-", UdpHelper.UDPLowerUserPort, UdpHelper.UDPMaxPort)]
        [InlineData("65535", UdpHelper.UDPMaxPort, UdpHelper.UDPMaxPort)]
        /// <param name="rangeStr">The test string to parse.</param>
        /// <param name="min">The min port returned.</param>
        /// <param name="max">The max port returned.</param>
        public void Valid_Ranges(string rangeStr, int min, int max)
        {
            Assert.True(rangeStr.TryParseRange(out var range));
            Assert.True(range.Min == min && range.Max == max);
        }
    }
}
