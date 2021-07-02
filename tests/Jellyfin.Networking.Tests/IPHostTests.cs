using FsCheck;
using FsCheck.Xunit;
using MediaBrowser.Common.Net;
using Xunit;

namespace Jellyfin.Networking.Tests
{
    public static class IPHostTests
    {
        /// <summary>
        /// Checks IP address formats.
        /// </summary>
        /// <param name="address">IP Address.</param>
        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("127.0.0.1:123")]
        [InlineData("localhost")]
        [InlineData("localhost:1345")]
        [InlineData("www.google.co.uk")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517/56")]
        [InlineData("[fd23:184f:2029:0:3139:7386:67d7:d517]:124")]
        [InlineData("fe80::7add:12ff:febb:c67b%16")]
        [InlineData("[fe80::7add:12ff:febb:c67b%16]:123")]
        [InlineData("fe80::7add:12ff:febb:c67b%16:123")]
        [InlineData("[fe80::7add:12ff:febb:c67b%16]")]
        public static void TryParse_ValidHostStrings_True(string address)
            => Assert.True(IPHost.TryParse(address, out _, IpClassType.IpBoth));

        [Property]
        public static Property TryParse_IPv4Address_True(IPv4Address address)
            => IPHost.TryParse(address.Item.ToString(), out _, IpClassType.Ip4Only).ToProperty();

        [Property]
        public static Property TryParse_IPv6Address_True(IPv6Address address)
            => IPHost.TryParse(address.Item.ToString(), out _, IpClassType.Ip6Only).ToProperty();

        /// <summary>
        /// All should be invalid address strings.
        /// </summary>
        /// <param name="address">Invalid address strings.</param>
        [Theory]
        [InlineData("256.128.0.0.0.1")]
        [InlineData("127.0.0.1#")]
        [InlineData("localhost!")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517:1231")]
        [InlineData("[fd23:184f:2029:0:3139:7386:67d7:d517:1231]")]
        public static void TryParse_InvalidAddressString_False(string address)
            => Assert.False(IPHost.TryParse(address, out _, IpClassType.IpBoth));
    }
}
