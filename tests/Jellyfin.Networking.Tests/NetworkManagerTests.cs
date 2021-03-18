using System;
using System.Collections.ObjectModel;
using System.Net;
using Jellyfin.Networking.Configuration;
using Jellyfin.Networking.Manager;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
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
        [InlineData("192.168.2.1/24, !192.168.2.122/32", "192.168.2.123")]
        [InlineData("fd23:184f:2029:0::/56", "fd23:184f:2029:0:3139:7386:67d7:d517")]
        [InlineData("[fd23:184f:2029:0::/56]", "fd23:184f:2029:0:3139:7386:67d7:d517")]
        public void InNetwork_True_Success(string network, string value)
        {
            var ip = IPAddress.Parse(value);
            var conf = new NetworkConfiguration()
            {
                EnableIPV6 = true,
                EnableIPV4 = true,
                LocalNetworkSubnets = network.Split(',')
            };

            using var networkManager = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());

            Assert.True(networkManager.IsInLocalNetwork(ip));
        }

        /// <summary>
        /// Checks that thge iven IP address is not in the network provided.
        /// </summary>
        /// <param name="network">Network address(es).</param>
        /// <param name="value">The IP to check.</param>
        [Theory]
        [InlineData("192.168.10.0/24, !192.168.10.60/32", "192.168.10.60")]
        [InlineData("192.168.10.0/24", "fd23:184f:2029:0:3139:7386:67d7:d517")]
        [InlineData("fd23:184f:2029:0::/56, !fd23:184f:2029:0:3139:7386:67d7:d500/120", "fd23:184f:2029:0:3139:7386:67d7:d517")]
        public void InNetwork_False_Success(string network, string value)
        {
            var ip = IPAddress.Parse(value);
            var conf = new NetworkConfiguration()
            {
                EnableIPV6 = true,
                EnableIPV4 = true,
                LocalNetworkSubnets = network.Split(',')
            };

            using var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());

            Assert.False(nm.IsInLocalNetwork(ip));
        }

        internal static IConfigurationManager GetMockConfig(NetworkConfiguration conf)
        {
            var configManager = new Mock<IConfigurationManager>
            {
                CallBase = true
            };
            configManager.Setup(x => x.GetConfiguration(It.IsAny<string>())).Returns(conf);
            return configManager.Object;
        }

        /// <summary>
        /// Test collection parsing.
        /// </summary>
        /// <param name="settings">Collection to parse.</param>
        /// <param name="result1">Included addresses from the collection.</param>
        /// <param name="result2">Included IP4 addresses from the collection.</param>
        /// <param name="result3">Excluded addresses from the collection.</param>
        /// <param name="result4">Excluded IP4 addresses from the collection.</param>
        /// <param name="result5">Network addresses of the collection.</param>
        [Theory]
        [InlineData(
            "127.0.0.1#",
            "[]",
            "[]",
            "[]",
            "[]",
            "[]")]
        [InlineData(
            "!127.0.0.1",
            "[]",
            "[]",
            "[127.0.0.1/32]",
            "[127.0.0.1/32]",
            "[]")]
        [InlineData(
            "",
            "[]",
            "[]",
            "[]",
            "[]",
            "[]")]
        [InlineData(
            "192.158.1.2/16, localhost, fd23:184f:2029:0:3139:7386:67d7:d517,    !10.10.10.10",
            "[192.158.1.2/16,[127.0.0.1/32,::1/128],fd23:184f:2029:0:3139:7386:67d7:d517/128]",
            "[192.158.1.2/16,127.0.0.1/32]",
            "[10.10.10.10/32]",
            "[10.10.10.10/32]",
            "[192.158.0.0/16,127.0.0.1/32,::1/128,fd23:184f:2029:0:3139:7386:67d7:d517/128]")]
        [InlineData(
            "192.158.1.2/255.255.0.0,192.169.1.2/8",
            "[192.158.1.2/16,192.169.1.2/8]",
            "[192.158.1.2/16,192.169.1.2/8]",
            "[]",
            "[]",
            "[192.158.0.0/16,192.0.0.0/8]")]
        public void TestCollections(string settings, string result1, string result2, string result3, string result4, string result5)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var conf = new NetworkConfiguration()
            {
                EnableIPV6 = true,
                EnableIPV4 = true,
            };

            using var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());

            // Test included.
            Collection<IPObject> nc = nm.CreateIPCollection(settings.Split(","), false);
            Assert.Equal(nc.AsString(), result1);

            // Test excluded.
            nc = nm.CreateIPCollection(settings.Split(","), true);
            Assert.Equal(nc.AsString(), result3);

            conf.EnableIPV6 = false;
            nm.UpdateSettings(conf);

            // Test IP4 included.
            nc = nm.CreateIPCollection(settings.Split(","), false);
            Assert.Equal(nc.AsString(), result2);

            // Test IP4 excluded.
            nc = nm.CreateIPCollection(settings.Split(","), true);
            Assert.Equal(nc.AsString(), result4);

            conf.EnableIPV6 = true;
            nm.UpdateSettings(conf);

            // Test network addresses of collection.
            nc = nm.CreateIPCollection(settings.Split(","), false);
            nc = nc.AsNetworks();
            Assert.Equal(nc.AsString(), result5);
        }

        /// <summary>
        /// Union two collections.
        /// </summary>
        /// <param name="settings">Source.</param>
        /// <param name="compare">Destination.</param>
        /// <param name="result">Result.</param>
        [Theory]
        [InlineData("127.0.0.1", "fd23:184f:2029:0:3139:7386:67d7:d517/64,fd23:184f:2029:0:c0f0:8a8a:7605:fffa/128,fe80::3139:7386:67d7:d517%16/64,192.168.1.208/24,::1/128,127.0.0.1/8", "[127.0.0.1/32]")]
        [InlineData("127.0.0.1", "127.0.0.1/8", "[127.0.0.1/32]")]
        public void UnionCheck(string settings, string compare, string result)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (compare == null)
            {
                throw new ArgumentNullException(nameof(compare));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var conf = new NetworkConfiguration()
            {
                EnableIPV6 = true,
                EnableIPV4 = true,
            };

            using var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());

            Collection<IPObject> nc1 = nm.CreateIPCollection(settings.Split(","), false);
            Collection<IPObject> nc2 = nm.CreateIPCollection(compare.Split(","), false);

            Assert.Equal(nc1.Union(nc2).AsString(), result);
        }

        /// <summary>
        /// Checks the ability to ignore virtual interfaces.
        /// </summary>
        /// <param name="interfaces">Mock network setup, in the format (IP address, interface index, interface name) | .... </param>
        /// <param name="lan">LAN addresses.</param>
        /// <param name="value">Bind addresses that are excluded.</param>
        [Theory]
        // All valid
        [InlineData("192.168.1.208/24,-16,eth16|200.200.200.200/24,11,eth11", "192.168.1.0/24;200.200.200.0/24", "[192.168.1.208/24,200.200.200.200/24]")]
        // eth16 only
        [InlineData("192.168.1.208/24,-16,eth16|200.200.200.200/24,11,eth11", "192.168.1.0/24", "[192.168.1.208/24]")]
        // All interfaces excluded.
        [InlineData("192.168.1.208/24,-16,vEthernet1|192.168.2.208/24,-16,vEthernet212|200.200.200.200/24,11,eth11", "192.168.1.0/24", "[]")]
        // vEthernet1 and vEthernet212 should be excluded.
        [InlineData("192.168.1.200/24,-20,vEthernet1|192.168.2.208/24,-16,vEthernet212|200.200.200.200/24,11,eth11", "192.168.1.0/24;200.200.200.200/24", "[200.200.200.200/24]")]
        public void IgnoreVirtualInterfaces(string interfaces, string lan, string value)
        {
            var conf = new NetworkConfiguration()
            {
                EnableIPV6 = true,
                EnableIPV4 = true,
                LocalNetworkSubnets = lan.Split(';') ?? throw new ArgumentNullException(nameof(lan))
            };

            NetworkManager.MockNetworkSettings = interfaces;
            using var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());
            NetworkManager.MockNetworkSettings = string.Empty;

            Assert.Equal(nm.GetInternalBindAddresses().AsString(), value);
        }
    }
}
