using System;
using System.Net;
using Jellyfin.Networking.Manager;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Net;
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

        /// <summary>
        /// Without explicit bind addresses the wildcard address must be returned. With IPv6 enabled this is
        /// always the IPv6 wildcard - keeping the socket IPv6-only when IPv4 is disabled is handled by the
        /// Kestrel socket transport setup. Binding the wildcard instead of individual interface addresses
        /// avoids bind failures for addresses that are not (yet) usable, e.g. during IPv6 duplicate address
        /// detection.
        /// </summary>
        /// <param name="enableIPv4">If IPv4 is enabled.</param>
        /// <param name="enableIPv6">If IPv6 is enabled.</param>
        /// <param name="expected">The expected wildcard bind address.</param>
        [Theory]
        [InlineData(true, true, "::")]
        [InlineData(false, true, "::")]
        [InlineData(true, false, "0.0.0.0")]
        public void GetAllBindInterfaces_NoExplicitBinds_ReturnsWildcard(bool enableIPv4, bool enableIPv6, string expected)
        {
            var conf = new NetworkConfiguration()
            {
                EnableIPv4 = enableIPv4,
                EnableIPv6 = enableIPv6
            };

            var result = NetworkManager.GetAllBindInterfaces(
                NullLogger<NetworkManager>.Instance,
                false,
                NetworkParseTests.GetMockConfig(conf),
                Array.Empty<IPData>(),
                enableIPv4,
                enableIPv6);

            Assert.Single(result);
            Assert.Equal(IPAddress.Parse(expected), result[0].Address);
        }

        /// <summary>
        /// Explicitly configured bind addresses must still be returned as concrete addresses,
        /// even when only IPv6 is enabled.
        /// </summary>
        [Fact]
        public void GetAllBindInterfaces_ExplicitBindAddress_ReturnsConcreteAddress()
        {
            var conf = new NetworkConfiguration()
            {
                EnableIPv4 = false,
                EnableIPv6 = true,
                LocalNetworkAddresses = new[] { "fd00::1" }
            };
            var knownInterfaces = new[]
            {
                new IPData(IPAddress.Parse("fd00::1"), new IPNetwork(IPAddress.Parse("fd00::1"), 64), "eth0")
            };

            var result = NetworkManager.GetAllBindInterfaces(
                NullLogger<NetworkManager>.Instance,
                false,
                NetworkParseTests.GetMockConfig(conf),
                knownInterfaces,
                false,
                true);

            Assert.Single(result);
            Assert.Equal(IPAddress.Parse("fd00::1"), result[0].Address);
        }
    }
}
