using System.Net;
using Jellyfin.Networking.Manager;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Networking.Tests
{
    public class NetworkManagerTests
    {
        /// <summary>
        /// Checks that the given IP address is in the specified network(s).
        /// </summary>
        /// <param name="network">Network address(es).</param>
        /// <param name="value">The IP to check.</param>
        [Theory]
        [InlineData("192.168.2.1/24", "192.168.2.123")]
        [InlineData("192.168.2.1/24, !192.168.2.122/32", "192.168.2.123")]
        [InlineData("fd23:184f:2029:0::/56", "fd23:184f:2029:0:3139:7386:67d7:d517")]
        [InlineData("fd23:184f:2029:0::/56, !fd23:184f:2029:0:3139:7386:67d7:d518/128", "fd23:184f:2029:0:3139:7386:67d7:d517")]
        public void InNetwork_True_Success(string network, string value)
        {
            var ip = IPAddress.Parse(value);
            var conf = new NetworkConfiguration()
            {
                EnableIPv6 = true,
                EnableIPv4 = true,
                LocalNetworkSubnets = network.Split(',')
            };

            var startupConf = new Mock<IConfiguration>();
            using var networkManager = new NetworkManager(NetworkParseTests.GetMockConfig(conf), startupConf.Object, new NullLogger<NetworkManager>());

            Assert.True(networkManager.IsInLocalNetwork(ip));
        }

        /// <summary>
        /// Checks that the given IP address is not in the network provided.
        /// </summary>
        /// <param name="network">Network address(es).</param>
        /// <param name="value">The IP to check.</param>
        [Theory]
        [InlineData("192.168.10.0/24", "192.168.11.1")]
        [InlineData("192.168.10.0/24, !192.168.10.60/32", "192.168.10.60")]
        [InlineData("192.168.10.0/24", "fd23:184f:2029:0:3139:7386:67d7:d517")]
        [InlineData("fd23:184f:2029:0::/56", "fd24:184f:2029:0:3139:7386:67d7:d517")]
        [InlineData("fd23:184f:2029:0::/56, !fd23:184f:2029:0:3139:7386:67d7:d500/120", "fd23:184f:2029:0:3139:7386:67d7:d517")]
        [InlineData("fd23:184f:2029:0::/56", "192.168.10.60")]
        [InlineData("2001:abcd:abcd:6b40::0/60", "192.168.10.60")]
        public void InNetwork_False_Success(string network, string value)
        {
            var ip = IPAddress.Parse(value);
            var conf = new NetworkConfiguration()
            {
                EnableIPv6 = true,
                EnableIPv4 = true,
                LocalNetworkSubnets = network.Split(',')
            };

            var startupConf = new Mock<IConfiguration>();
            using var networkManager = new NetworkManager(NetworkParseTests.GetMockConfig(conf), startupConf.Object, new NullLogger<NetworkManager>());

            Assert.False(networkManager.IsInLocalNetwork(ip));
        }
    }
}
