using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Jellyfin.Networking.Manager;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using IConfigurationManager = MediaBrowser.Common.Configuration.IConfigurationManager;

namespace Jellyfin.Networking.Tests
{
    public class NetworkParseTests
    {
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
        // eth16 only without mask
        [InlineData("192.168.1.208,-16,eth16|200.200.200.200,11,eth11", "192.168.1.0/24", "[192.168.1.208/32]")]
        // All interfaces excluded. (including loopbacks)
        [InlineData("192.168.1.208/24,-16,vEthernet1|192.168.2.208/24,-16,vEthernet212|200.200.200.200/24,11,eth11", "192.168.1.0/24", "[]")]
        // vEthernet1 and vEthernet212 should be excluded.
        [InlineData("192.168.1.200/24,-20,vEthernet1|192.168.2.208/24,-16,vEthernet212|200.200.200.200/24,11,eth11", "192.168.1.0/24;200.200.200.200/24", "[200.200.200.200/24]")]
        // Overlapping interface,
        [InlineData("192.168.1.110/24,-20,br0|192.168.1.10/24,-16,br0|200.200.200.200/24,11,eth11", "192.168.1.0/24", "[192.168.1.110/24,192.168.1.10/24]")]
        public void IgnoreVirtualInterfaces(string interfaces, string lan, string value)
        {
            var conf = new NetworkConfiguration()
            {
                EnableIPv6 = true,
                EnableIPv4 = true,
                LocalNetworkSubnets = lan?.Split(';') ?? throw new ArgumentNullException(nameof(lan))
            };

            NetworkManager.MockNetworkSettings = interfaces;
            var startupConf = new Mock<IConfiguration>();
            using var nm = new NetworkManager(NetworkParseTests.GetMockConfig(conf), startupConf.Object, new NullLogger<NetworkManager>());
            NetworkManager.MockNetworkSettings = string.Empty;

            Assert.Equal(value, "[" + string.Join(",", nm.GetInternalBindAddresses().Select(x => x.Address + "/" + x.Subnet.PrefixLength)) + "]");
        }

        /// <summary>
        /// Checks valid IP address formats.
        /// </summary>
        /// <param name="address">IP Address.</param>
        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("127.0.0.1/8")]
        [InlineData("192.168.1.2")]
        [InlineData("192.168.1.2/24")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517")]
        [InlineData("[fd23:184f:2029:0:3139:7386:67d7:d517]")]
        [InlineData("fe80::7add:12ff:febb:c67b%16")]
        [InlineData("[fe80::7add:12ff:febb:c67b%16]:123")]
        [InlineData("fe80::7add:12ff:febb:c67b%16:123")]
        [InlineData("[fe80::7add:12ff:febb:c67b%16]")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517/56")]
        public static void TryParseValidIPStringsTrue(string address)
            => Assert.True(NetworkUtils.TryParseToSubnet(address, out _));

        /// <summary>
        /// Checks invalid IP address formats.
        /// </summary>
        /// <param name="address">IP Address.</param>
        [Theory]
        [InlineData("127.0.0.1#")]
        [InlineData("localhost!")]
        [InlineData("256.128.0.0.0.1")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517:1231")]
        [InlineData("[fd23:184f:2029:0:3139:7386:67d7:d517:1231]")]
        public static void TryParseInvalidIPStringsFalse(string address)
            => Assert.False(NetworkUtils.TryParseToSubnet(address, out _));

        /// <summary>
        /// Checks if IPv4 address is within a defined subnet.
        /// </summary>
        /// <param name="netMask">Network mask.</param>
        /// <param name="ipAddress">IP Address.</param>
        [Theory]
        [InlineData("192.168.5.85/24", "192.168.5.1")]
        [InlineData("192.168.5.85/24", "192.168.5.254")]
        [InlineData("10.128.240.50/30", "10.128.240.48")]
        [InlineData("10.128.240.50/30", "10.128.240.49")]
        [InlineData("10.128.240.50/30", "10.128.240.50")]
        [InlineData("10.128.240.50/30", "10.128.240.51")]
        [InlineData("127.0.0.1/8", "127.0.0.1")]
        public void IPv4SubnetMaskMatchesValidIPAddress(string netMask, string ipAddress)
        {
            var ipa = IPAddress.Parse(ipAddress);
            Assert.True(NetworkUtils.TryParseToSubnet(netMask, out var subnet) && subnet.Contains(IPAddress.Parse(ipAddress)));
        }

        /// <summary>
        /// Checks if IPv4 address is not within a defined subnet.
        /// </summary>
        /// <param name="netMask">Network mask.</param>
        /// <param name="ipAddress">IP Address.</param>
        [Theory]
        [InlineData("192.168.5.85/24", "192.168.4.254")]
        [InlineData("192.168.5.85/24", "191.168.5.254")]
        [InlineData("10.128.240.50/30", "10.128.240.47")]
        [InlineData("10.128.240.50/30", "10.128.240.52")]
        [InlineData("10.128.240.50/30", "10.128.239.50")]
        [InlineData("10.128.240.50/30", "10.127.240.51")]
        public void IPv4SubnetMaskDoesNotMatchInvalidIPAddress(string netMask, string ipAddress)
        {
            var ipa = IPAddress.Parse(ipAddress);
            Assert.False(NetworkUtils.TryParseToSubnet(netMask, out var subnet) && subnet.Contains(IPAddress.Parse(ipAddress)));
        }

        /// <summary>
        /// Checks if IPv6 address is within a defined subnet.
        /// </summary>
        /// <param name="netMask">Network mask.</param>
        /// <param name="ipAddress">IP Address.</param>
        [Theory]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:0000:0000:0000:0000")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:FFFF:FFFF:FFFF:FFFF")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:0001:0000:0000:0000")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:FFFF:FFFF:FFFF:FFF0")]
        [InlineData("2001:db8:abcd:0012::0/128", "2001:0DB8:ABCD:0012:0000:0000:0000:0000")]
        public void IPv6SubnetMaskMatchesValidIPAddress(string netMask, string ipAddress)
        {
            Assert.True(NetworkUtils.TryParseToSubnet(netMask, out var subnet) && subnet.Contains(IPAddress.Parse(ipAddress)));
        }

        [Theory]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0011:FFFF:FFFF:FFFF:FFFF")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0013:0000:0000:0000:0000")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0013:0001:0000:0000:0000")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0011:FFFF:FFFF:FFFF:FFF0")]
        [InlineData("2001:db8:abcd:0012::0/128", "2001:0DB8:ABCD:0012:0000:0000:0000:0001")]
        public void IPv6SubnetMaskDoesNotMatchInvalidIPAddress(string netMask, string ipAddress)
        {
            Assert.False(NetworkUtils.TryParseToSubnet(netMask, out var subnet) && subnet.Contains(IPAddress.Parse(ipAddress)));
        }

        [Theory]
        // Testing bind interfaces.
        // On my system eth16 is internal, eth11 external (Windows defines the indexes).
        //
        // This test is to replicate how DLNA requests work throughout the system.

        // User on internal network, we're bound internal and external - so result is internal.
        [InlineData("192.168.1.1", "eth16,eth11", false, "eth16")]
        // User on external network, we're bound internal and external - so result is external.
        [InlineData("8.8.8.8", "eth16,eth11", false, "eth11")]
        // User on internal network, we're bound internal only - so result is internal.
        [InlineData("10.10.10.10", "eth16", false, "eth16")]
        // User on internal network, no binding specified - so result is the 1st internal.
        [InlineData("192.168.1.1", "", false, "eth16")]
        // User on external network, internal binding only - so result is the 1st internal.
        [InlineData("jellyfin.org", "eth16", false, "eth16")]
        // User on external network, no binding - so result is the 1st external.
        [InlineData("jellyfin.org", "", false, "eth11")]
        // Dns failure - should skip the test.
        // https://en.wikipedia.org/wiki/.test
        [InlineData("invalid.domain.test", "", false, "eth11")]
        // User assumed to be internal, no binding - so result is the 1st internal.
        [InlineData("", "", false, "eth16")]
        public void TestBindInterfaces(string source, string bindAddresses, bool ipv6enabled, string result)
        {
            ArgumentNullException.ThrowIfNull(source);

            ArgumentNullException.ThrowIfNull(bindAddresses);

            ArgumentNullException.ThrowIfNull(result);

            var conf = new NetworkConfiguration()
            {
                LocalNetworkAddresses = bindAddresses.Split(','),
                EnableIPv6 = ipv6enabled,
                EnableIPv4 = true
            };

            NetworkManager.MockNetworkSettings = "192.168.1.208/24,-16,eth16|200.200.200.200/24,11,eth11";
            var startupConf = new Mock<IConfiguration>();
            using var nm = new NetworkManager(NetworkParseTests.GetMockConfig(conf), startupConf.Object, new NullLogger<NetworkManager>());
            NetworkManager.MockNetworkSettings = string.Empty;

            // Check to see if DNS resolution is working. If not, skip test.
            if (!NetworkUtils.TryParseHost(source, out var host))
            {
                return;
            }

            if (nm.TryParseInterface(result, out var resultObj))
            {
                result = resultObj[0].Address.ToString();
                var intf = nm.GetBindAddress(source, out _);

                Assert.Equal(intf, result);
            }
        }

        [Theory]

        // Testing bind interfaces. These are set for my system so won't work elsewhere.
        // On my system eth16 is internal, eth11 external (Windows defines the indexes).
        //
        // This test is to replicate how subnet bound ServerPublisherUri work throughout the system.

        // User on internal network, we're bound internal and external - so result is internal override.
        [InlineData("192.168.1.1", "192.168.1.0/24", "eth16,eth11", false, "192.168.1.0/24=internal.jellyfin", "internal.jellyfin")]

        // User on external network, we're bound internal and external - so result is override.
        [InlineData("8.8.8.8", "192.168.1.0/24", "eth16,eth11", false, "all=http://helloworld.com", "http://helloworld.com")]

        // User on internal network, we're bound internal only, but the address isn't in the LAN - so return the override.
        [InlineData("10.10.10.10", "192.168.1.0/24", "eth16", false, "external=http://internalButNotDefinedAsLan.com", "http://internalButNotDefinedAsLan.com")]

        // User on internal network, no binding specified - so result is the 1st internal.
        [InlineData("192.168.1.1", "192.168.1.0/24", "", false, "external=http://helloworld.com", "eth16")]

        // User on external network, internal binding only - so assumption is a proxy forward, return external override.
        [InlineData("jellyfin.org", "192.168.1.0/24", "eth16", false, "external=http://helloworld.com", "http://helloworld.com")]

        // User on external network, no binding - so result is the 1st external which is overridden.
        [InlineData("jellyfin.org", "192.168.1.0/24", "", false, "external=http://helloworld.com", "http://helloworld.com")]

        // User assumed to be internal, no binding - so result is the 1st matching interface.
        [InlineData("", "192.168.1.0/24", "", false, "all=http://helloworld.com", "eth16")]

        // User is internal, no binding - so result is the 1st internal interface, which is then overridden.
        [InlineData("192.168.1.1", "192.168.1.0/24", "", false, "eth16=http://helloworld.com", "http://helloworld.com")]
        public void TestBindInterfaceOverrides(string source, string lan, string bindAddresses, bool ipv6enabled, string publishedServers, string result)
        {
            ArgumentNullException.ThrowIfNull(lan);

            ArgumentNullException.ThrowIfNull(bindAddresses);

            var conf = new NetworkConfiguration()
            {
                LocalNetworkSubnets = lan.Split(','),
                LocalNetworkAddresses = bindAddresses.Split(','),
                EnableIPv6 = ipv6enabled,
                EnableIPv4 = true,
                PublishedServerUriBySubnet = new string[] { publishedServers }
            };

            NetworkManager.MockNetworkSettings = "192.168.1.208/24,-16,eth16|200.200.200.200/24,11,eth11";
            var startupConf = new Mock<IConfiguration>();
            using var nm = new NetworkManager(NetworkParseTests.GetMockConfig(conf), startupConf.Object, new NullLogger<NetworkManager>());
            NetworkManager.MockNetworkSettings = string.Empty;

            if (nm.TryParseInterface(result, out IReadOnlyList<IPData>? resultObj) && resultObj is not null)
            {
                // Parse out IPAddresses so we can do a string comparison (ignore subnet masks).
                result = resultObj[0].Address.ToString();
            }

            var intf = nm.GetBindAddress(source, out int? _);

            Assert.Equal(result, intf);
        }

        [Theory]
        [InlineData("185.10.10.10,200.200.200.200", "79.2.3.4", true)]
        [InlineData("185.10.10.10", "185.10.10.10", false)]
        [InlineData("", "100.100.100.100", false)]

        public void HasRemoteAccess_GivenWhitelist_AllowsOnlyIPsInWhitelist(string addresses, string remoteIP, bool denied)
        {
            // Comma separated list of IP addresses or IP/netmask entries for networks that will be allowed to connect remotely.
            // If left blank, all remote addresses will be allowed.
            var conf = new NetworkConfiguration()
            {
                EnableIPv4 = true,
                RemoteIPFilter = addresses.Split(','),
                IsRemoteIPFilterBlacklist = false
            };

            var startupConf = new Mock<IConfiguration>();
            using var nm = new NetworkManager(NetworkParseTests.GetMockConfig(conf), startupConf.Object, new NullLogger<NetworkManager>());

            Assert.NotEqual(nm.HasRemoteAccess(IPAddress.Parse(remoteIP)), denied);
        }

        [Theory]
        [InlineData("185.10.10.10", "79.2.3.4", false)]
        [InlineData("185.10.10.10", "185.10.10.10", true)]
        [InlineData("", "100.100.100.100", false)]

        public void HasRemoteAccess_GivenBlacklist_BlacklistTheIPs(string addresses, string remoteIP, bool denied)
        {
            // Comma separated list of IP addresses or IP/netmask entries for networks that will be allowed to connect remotely.
            // If left blank, all remote addresses will be allowed.
            var conf = new NetworkConfiguration()
            {
                EnableIPv4 = true,
                RemoteIPFilter = addresses.Split(','),
                IsRemoteIPFilterBlacklist = true
            };

            var startupConf = new Mock<IConfiguration>();
            using var nm = new NetworkManager(NetworkParseTests.GetMockConfig(conf), startupConf.Object, new NullLogger<NetworkManager>());

            Assert.NotEqual(nm.HasRemoteAccess(IPAddress.Parse(remoteIP)), denied);
        }

        [Theory]
        [InlineData("192.168.1.209/24,-16,eth16", "192.168.1.0/24", "", "192.168.1.209")] // Only 1 address so use it.
        [InlineData("192.168.1.208/24,-16,eth16|10.0.0.1/24,10,eth7", "192.168.1.0/24", "", "192.168.1.208")] // LAN address is specified by default.
        [InlineData("192.168.1.208/24,-16,eth16|10.0.0.1/24,10,eth7", "192.168.1.0/24", "10.0.0.1", "10.0.0.1")] // return bind address

        public void GetBindInterface_NoSourceGiven_Success(string interfaces, string lan, string bind, string result)
        {
            var conf = new NetworkConfiguration
            {
                EnableIPv4 = true,
                LocalNetworkSubnets = lan.Split(','),
                LocalNetworkAddresses = bind.Split(',')
            };

            NetworkManager.MockNetworkSettings = interfaces;
            var startupConf = new Mock<IConfiguration>();
            using var nm = new NetworkManager(NetworkParseTests.GetMockConfig(conf), startupConf.Object, new NullLogger<NetworkManager>());

            var interfaceToUse = nm.GetBindAddress(string.Empty, out _);

            Assert.Equal(result, interfaceToUse);
        }

        [Theory]
        [InlineData("192.168.1.209/24,-16,eth16", "192.168.1.0/24", "", "192.168.1.210", "192.168.1.209")] // Source on LAN
        [InlineData("192.168.1.208/24,-16,eth16|10.0.0.1/24,10,eth7", "192.168.1.0/24", "", "192.168.1.209", "192.168.1.208")] // Source on LAN
        [InlineData("192.168.1.208/24,-16,eth16|10.0.0.1/24,10,eth7", "192.168.1.0/24", "", "8.8.8.8", "10.0.0.1")] // Source external.
        [InlineData("192.168.1.208/24,-16,eth16|10.0.0.1/24,10,eth7", "192.168.1.0/24", "10.0.0.1", "192.168.1.209", "10.0.0.1")] // LAN not bound, so return external.
        [InlineData("192.168.1.208/24,-16,eth16|10.0.0.1/24,10,eth7", "192.168.1.0/24", "192.168.1.208,10.0.0.1", "8.8.8.8", "10.0.0.1")] // return external bind address
        [InlineData("192.168.1.208/24,-16,eth16|10.0.0.1/24,10,eth7", "192.168.1.0/24", "192.168.1.208,10.0.0.1", "192.168.1.210", "192.168.1.208")] // return LAN bind address
        public void GetBindInterface_ValidSourceGiven_Success(string interfaces, string lan, string bind, string source, string result)
        {
            var conf = new NetworkConfiguration
            {
                EnableIPv4 = true,
                LocalNetworkSubnets = lan.Split(','),
                LocalNetworkAddresses = bind.Split(',')
            };

            NetworkManager.MockNetworkSettings = interfaces;
            var startupConf = new Mock<IConfiguration>();
            using var nm = new NetworkManager(NetworkParseTests.GetMockConfig(conf), startupConf.Object, new NullLogger<NetworkManager>());

            var interfaceToUse = nm.GetBindAddress(source, out _);

            Assert.Equal(result, interfaceToUse);
        }
    }
}
