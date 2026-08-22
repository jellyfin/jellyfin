using System.Net;
using Jellyfin.Networking.HappyEyeballs;
using Xunit;

namespace Jellyfin.Networking.Tests
{
    public static class HttpClientExtensionTests
    {
        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("127.1.2.3")]
        [InlineData("0.0.0.0")]
        [InlineData("0.1.2.3")]
        [InlineData("169.254.169.254")]
        [InlineData("169.254.0.1")]
        [InlineData("224.0.0.1")]
        [InlineData("239.255.255.250")]
        [InlineData("255.255.255.255")]
        [InlineData("::1")]
        [InlineData("::")]
        [InlineData("fe80::1")]
        [InlineData("fec0::1")]
        [InlineData("ff02::1")]
        [InlineData("::ffff:127.0.0.1")]
        [InlineData("::ffff:169.254.169.254")]
        [InlineData("::127.0.0.1")]
        [InlineData("2002:7f00:1::")]
        [InlineData("2002:a9fe:a9fe::")]
        [InlineData("64:ff9b::7f00:1")]
        [InlineData("64:ff9b::a9fe:a9fe")]
        public static void IsRestrictedAddress_RestrictedTarget_True(string address)
        {
            Assert.True(HttpClientExtension.IsRestrictedAddress(IPAddress.Parse(address)));
        }

        [Theory]
        // Public addresses.
        [InlineData("1.1.1.1")]
        [InlineData("93.184.216.34")]
        [InlineData("2606:4700:4700::1111")]
        // RFC1918 and the IPv6 unique local range stay reachable for LAN tuners and local services.
        [InlineData("10.0.0.5")]
        [InlineData("172.16.0.5")]
        [InlineData("192.168.1.10")]
        [InlineData("fd00::1")]
        [InlineData("::ffff:192.168.1.10")]
        // 6to4 and NAT64 wrapping a routable address are not restricted.
        [InlineData("2002:0101:0101::")]
        [InlineData("64:ff9b::0101:0101")]
        public static void IsRestrictedAddress_AllowedTarget_False(string address)
        {
            Assert.False(HttpClientExtension.IsRestrictedAddress(IPAddress.Parse(address)));
        }
    }
}
