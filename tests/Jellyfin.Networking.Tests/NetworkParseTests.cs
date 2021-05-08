using System;
using System.Collections.Generic;
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
    /// <summary>
    ///  Defines the <seealso cref="NetworkParseTests"/>.
    /// </summary>
    /// <remarks>
    /// Mock network settings permit the testing of other physical setups via the NetworkManager.MockNetworkSettings property.
    ///
    /// The network is defined by a series of values, separated by the character |
    ///
    /// The format is [ip address],[interface index],[interface name].
    /// </remarks>
    public class NetworkParseTests
    {
        /// <summary>
        /// Compares two IEnumerable{IPNetAddress} objects. The order is ignored.
        /// </summary>
        /// <param name="source">The <see cref="Collection{IPNetAddress}"/>.</param>
        /// <param name="dest">Item to compare to.</param>
        /// <returns>True if both are equal.</returns>
        internal static bool Compare(IEnumerable<IPNetAddress> source, IEnumerable<IPNetAddress> dest)
        {
            if (dest == null)
            {
                return false;
            }

            foreach (var sourceItem in source)
            {
                bool found = false;
                foreach (var destItem in dest)
                {
                    if (sourceItem.Equals(destItem))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        internal static IConfigurationManager GetMockConfig(NetworkConfiguration conf)
        {
            var configManager = new Mock<IConfigurationManager>
            {
                CallBase = true
            };

            configManager.Setup(x => x.GetConfiguration(It.IsAny<string>())).Returns(conf);
            return (IConfigurationManager)configManager.Object;
        }

        /// <summary>
        /// Tests the ability of the system to ignore interfaces specified, returning local interfaces.
        /// This functionality is used to filter out virtual interfaces. <see cref="NetworkConfiguration.VirtualInterfaceNames"/>.
        /// </summary>
        /// <param name="interfaces">Mock network setup (see remarks above).</param>
        /// <param name="value">Interface addresses after filter.</param>
        [Theory]
        // All valid
        [InlineData("192.168.1.208/24,-16,eth16|200.200.200.200/24,11,eth11", "[192.168.1.208/24,200.200.200.200/24]")]
        // vEthernet1 and vEthernet212 should be excluded.
        [InlineData("192.168.1.200/24,-20,vEthernet1|192.168.2.208/24,-16,vEthernet212|200.200.200.200/24,11,eth11", "[200.200.200.200/24]")]
        // All invalid
        [InlineData("192.168.1.200/24,-20,vEthernet1|192.168.2.208/24,-16,vEthernet212", "[]")]
        public void Interfaces_Ignore_Virtual(string interfaces, string value)
        {
            var conf = new NetworkConfiguration()
            {
                EnableIPV6 = true,
                EnableIPV4 = true,
                LocalNetworkSubnets = new string[] { "0.0.0.0/0" }
            };

            NetworkManager.MockNetworkSettings = interfaces;
            using var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());
            NetworkManager.MockNetworkSettings = string.Empty;

            Assert.Equal(value, nm.GetInternalBindAddresses().AsString());
        }

        /// <summary>
        /// Tests the ability of the system to ignore interfaces specified, returning local interfaces.
        /// This functionality is used to filter out virtual interfaces. <see cref="NetworkConfiguration.VirtualInterfaceNames"/>.
        /// </summary>
        /// <param name="interfaces">Mock network setup (see remarks above).</param>
        /// <param name="lan">LAN address range.</param>
        /// <param name="value">Interface addresses after filter..</param>
        [Theory]
        // All valid
        [InlineData("192.168.1.208/24,-16,eth16|200.200.200.200/24,11,eth11", "192.168.1.0/24;200.200.200.0/24", "[192.168.1.208/24,200.200.200.200/24]")]
        // eth16 only
        [InlineData("192.168.1.208/24,-16,eth16|200.200.200.200/24,11,eth11", "192.168.1.0/24", "[192.168.1.208/24]")]
        // All interfaces excluded. (including loopbacks)
        [InlineData("192.168.1.208/24,-16,vEthernet1|192.168.2.208/24,-16,vEthernet212|200.200.200.200/24,11,eth11", "192.168.1.0/24", "[]")]
        // vEthernet1 and vEthernet212 should be excluded.
        [InlineData("192.168.1.200/24,-20,vEthernet1|192.168.2.208/24,-16,vEthernet212|200.200.200.200/24,11,eth11", "192.168.1.0/24;200.200.200.200/24", "[200.200.200.200/24]")]
        // Overlapping interface,
        [InlineData("192.168.1.110/24,-20,br0|192.168.1.10/24,-16,br1|200.200.200.200/24,11,eth11", "192.168.1.0/24", "[192.168.1.110/24,192.168.1.10/24]")]
        public void IgnoreVirtualInterfaces(string interfaces, string lan, string value)
        {
            var conf = new NetworkConfiguration()
            {
                EnableIPV6 = true,
                EnableIPV4 = true,
                LocalNetworkSubnets = lan?.Split(';') ?? throw new ArgumentNullException(nameof(lan))
            };

            NetworkManager.MockNetworkSettings = interfaces;
            using var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());
            NetworkManager.MockNetworkSettings = string.Empty;

            Assert.Equal(value, nm.GetInternalBindAddresses().AsString());
        }

        /// <summary>
        /// Validates that the address is a valid host string.
        /// </summary>
        /// <param name="address">IP Address.</param>
        /// <param name="valid">Expected outcome.</param>
        /// <param name="expected">Expected value.</param>
        [Theory]
        [InlineData("127.0.0.1", true, "127.0.0.1 [127.0.0.1/32]")]
        [InlineData("127.0.0.1:123", true, "127.0.0.1 [127.0.0.1/32]")]
        [InlineData("localhost", true, "localhost [127.0.0.1/32,::1/128]")]
        [InlineData("localhost:1345", true, "localhost [127.0.0.1/32,::1/128]")]
        [InlineData("www.google.co.uk", true, "")]
        [InlineData("www.google.co.uk:1273", true, "")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517", true, "fd23:184f:2029:0:3139:7386:67d7:d517 [fd23:184f:2029:0:3139:7386:67d7:d517/128]")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517/56", true, "")]
        [InlineData("[fd23:184f:2029:0:3139:7386:67d7:d517]:124", true, "fd23:184f:2029:0:3139:7386:67d7:d517 [fd23:184f:2029:0:3139:7386:67d7:d517/128]")]
        [InlineData("fe80::7add:12ff:febb:c67b%16", true, "fe80::7add:12ff:febb:c67b [fe80::7add:12ff:febb:c67b%16/128]")]
        [InlineData("[fe80::7add:12ff:febb:c67b%16]:123", true, "fe80::7add:12ff:febb:c67b [fe80::7add:12ff:febb:c67b%16/128]")]
        [InlineData("fe80::7add:12ff:febb:c67b%16:123", true, "fe80::7add:12ff:febb:c67b [fe80::7add:12ff:febb:c67b/128]")]
        [InlineData("[fe80::7add:12ff:febb:c67b%16]", true, "fe80::7add:12ff:febb:c67b [fe80::7add:12ff:febb:c67b%16/128]")]
        [InlineData("192.168.1.2/255.255.255.0", false, "")]
        [InlineData("192.168.1.2/24", false, "")]

        public void Check_Host_Is_Valid_String(string address, bool valid, string expected)
        {
            if (IPHost.TryParse(address, out var result, IpClassType.IpBoth))
            {
                // Cannot safely check dns names as the ip responses change.
                if (!string.IsNullOrEmpty(expected))
                {
                    Assert.Equal(expected, result.ToString());
                }

                Assert.True(valid);
                return;
            }

            Assert.False(valid);
        }

        /// <summary>
        /// Checks IP address formats.
        /// </summary>
        /// <param name="address">IP Address.</param>
        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517/56")]
        [InlineData("[fd23:184f:2029:0:3139:7386:67d7:d517]")]
        [InlineData("fe80::7add:12ff:febb:c67b%16")]
        [InlineData("[fe80::7add:12ff:febb:c67b%16]:123")]
        [InlineData("fe80::7add:12ff:febb:c67b%16:123")]
        [InlineData("[fe80::7add:12ff:febb:c67b%16]")]
        [InlineData("192.168.1.2/255.255.255.0")]
        [InlineData("192.168.1.2/24")]
        [InlineData("0.0.0.0/0")]
        [InlineData("2001:db8::/33")]
        [InlineData("2001:db8::/52")]
        [InlineData("2001:db8::/122")]
        public void ValidIPStrings(string address)
        {
            Assert.True(IPNetAddress.TryParse(address, out _, IpClassType.IpBoth));
        }

        /// <summary>
        /// Checks against overflow errors in Network address calculations.
        /// </summary>
        /// <param name="address">IP address.</param>
        [Theory]
        [InlineData("2001:db8::/33")]
        [InlineData("2001:db8::/52")]
        [InlineData("2001:db8::/122")]
        public void NetworkAddress(string address)
        {
            var ip = IPNetAddress.Parse(address);
            var netAddress = IPNetAddress.NetworkAddressOf(ip.Address, ip.PrefixLength);
            Assert.True(ip.Equals(netAddress));
        }

        /// <summary>
        /// All should be invalid address strings.
        /// </summary>
        /// <param name="address">Invalid address strings.</param>
        [Theory]
        [InlineData("127.0.0.1/0")]
        [InlineData("10.10.10.1/255")]
        [InlineData("10.10.10.1/65535")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517:1231/0")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517:1231/129")]
        [InlineData("256.128.0.0.0.1")]
        [InlineData("127.0.0.1#")]
        [InlineData("localhost!")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517:1231")]
        [InlineData("[fd23:184f:2029:0:3139:7386:67d7:d517:1231]")]
        [InlineData("129.10.20.30/0")]
        [InlineData("fe80::7add:12ff:febb:c67b%16/0")]
        public void Check_Invalid_Addresses(string address)
        {
            Assert.False(IPNetAddress.TryParse(address, out _, IpClassType.IpBoth));
            Assert.False(IPHost.TryParse(address, out _, IpClassType.IpBoth));
        }

        /// <summary>
        /// Test collection parsing.
        /// </summary>
        /// <param name="settings">Collection to parse.</param>
        /// <param name="includedItems">Included addresses from the collection.</param>
        /// <param name="includedIp4Items">Included IP4 addresses from the collection.</param>
        /// <param name="excludedItems">Excluded addresses from the collection.</param>
        /// <param name="excludedIp4Items">Excluded IP4 addresses from the collection.</param>
        /// <param name="networkAddress">Network addresses of the collection.</param>
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
            "[192.158.1.2/16,localhost [127.0.0.1/32,::1/128],fd23:184f:2029:0:3139:7386:67d7:d517/128]",
            "[192.158.1.2/16,localhost [127.0.0.1/32]]",
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
        public void Test_Collection_Parsing(string settings, string includedItems, string includedIp4Items, string excludedItems, string excludedIp4Items, string networkAddress)
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
            var nc = nm.CreateIPCollection(settings.Split(","), false, false);
            Assert.Equal(includedItems, nc.AsString());

            // Test excluded.
            nc = nm.CreateIPCollection(settings.Split(','), true, false);
            Assert.Equal(excludedItems, nc.AsString());

            conf.EnableIPV6 = false;
            nm.UpdateSettings(conf);

            // Test IP4 included.
            nc = nm.CreateIPCollection(settings.Split(','), false, false);
            Assert.Equal(includedIp4Items, nc.AsString());

            // Test IP4 excluded.
            nc = nm.CreateIPCollection(settings.Split(','), true, false);
            Assert.Equal(excludedIp4Items, nc.AsString());

            conf.EnableIPV6 = true;
            nm.UpdateSettings(conf);

            // Test network addresses of collection.
            nc = nm.CreateIPCollection(settings.Split(','), false, false);
            var nca = nc.AsNetworkAddresses();
            Assert.Equal(networkAddress, nca.AsString());
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

            var nc1 = nm.CreateIPCollection(settings.Split(','), false, false);
            var nc2 = nm.CreateIPCollection(compare.Split(','), false, false);

            Assert.Equal(result, nc1.ThatAreContainedInNetworks(nc2).AsString());
        }

        [Theory]
        [InlineData("192.168.5.85/24", "192.168.5.1")]
        [InlineData("192.168.5.85/24", "192.168.5.254")]
        [InlineData("10.128.240.50/30", "10.128.240.48")]
        [InlineData("10.128.240.50/30", "10.128.240.49")]
        [InlineData("10.128.240.50/30", "10.128.240.50")]
        [InlineData("10.128.240.50/30", "10.128.240.51")]
        [InlineData("127.0.0.1/8", "127.0.0.1")]
        [InlineData("0.0.0.0/0", "192.168.10.60")]
        public void IpV4SubnetMaskMatchesValidIpAddress(string netMask, string ipAddress)
        {
            var ipAddressObj = IPNetAddress.Parse(netMask);
            Assert.True(ipAddressObj.Contains(IPAddress.Parse(ipAddress)));
        }

        [Theory]
        [InlineData("192.168.5.85/24", "192.168.4.254")]
        [InlineData("192.168.5.85/24", "191.168.5.254")]
        [InlineData("10.128.240.50/30", "10.128.240.47")]
        [InlineData("10.128.240.50/30", "10.128.240.52")]
        [InlineData("10.128.240.50/30", "10.128.239.50")]
        [InlineData("10.128.240.50/30", "10.127.240.51")]
        [InlineData("0.0.0.0/32", "192.168.10.60")]
        public void IpV4SubnetMaskDoesNotMatchInvalidIpAddress(string netMask, string ipAddress)
        {
            var ipAddressObj = IPNetAddress.Parse(netMask);
            Assert.False(ipAddressObj.Contains(IPAddress.Parse(ipAddress)));
        }

        [Theory]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:0000:0000:0000:0000")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:FFFF:FFFF:FFFF:FFFF")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:0001:0000:0000:0000")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:FFFF:FFFF:FFFF:FFF0")]
        [InlineData("2001:db8:abcd:0012::0/128", "2001:0DB8:ABCD:0012:0000:0000:0000:0000")]
        public void IpV6SubnetMaskMatchesValidIpAddress(string netMask, string ipAddress)
        {
            var ipAddressObj = IPNetAddress.Parse(netMask);
            Assert.True(ipAddressObj.Contains(IPAddress.Parse(ipAddress)));
        }

        [Theory]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0011:FFFF:FFFF:FFFF:FFFF")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0013:0000:0000:0000:0000")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0013:0001:0000:0000:0000")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0011:FFFF:FFFF:FFFF:FFF0")]
        [InlineData("2001:db8:abcd:0012::0/128", "2001:0DB8:ABCD:0012:0000:0000:0000:0001")]
        public void IpV6SubnetMaskDoesNotMatchInvalidIpAddress(string netMask, string ipAddress)
        {
            var ipAddressObj = IPNetAddress.Parse(netMask);
            Assert.False(ipAddressObj.Contains(IPAddress.Parse(ipAddress)));
        }

        [Theory]
        [InlineData("10.0.0.0/255.0.0.0", "10.10.10.1/32")]
        [InlineData("10.0.0.0/8", "10.10.10.1/32")]
        [InlineData("10.0.0.0/255.0.0.0", "10.10.10.1")]

        [InlineData("10.10.0.0/255.255.0.0", "10.10.10.1/32")]
        [InlineData("10.10.0.0/16", "10.10.10.1/32")]
        [InlineData("10.10.0.0/255.255.0.0", "10.10.10.1")]

        [InlineData("10.10.10.0/255.255.255.0", "10.10.10.1/32")]
        [InlineData("10.10.10.0/24", "10.10.10.1/32")]
        [InlineData("10.10.10.0/255.255.255.0", "10.10.10.1")]

        public void TestSubnetContains(string network, string ip)
        {
            Assert.True(IPNetAddress.TryParse(network, out var networkObj, IpClassType.IpBoth));
            Assert.True(IPNetAddress.TryParse(ip, out IPNetAddress? ipObj, IpClassType.IpBoth));
            Assert.True(networkObj?.Contains(ipObj!));
        }

        [Theory]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "172.168.1.2/24", "172.168.1.2/24")]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "172.168.1.2/24, 10.10.10.1", "172.168.1.2/24,10.10.10.1/24")]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "192.168.1.2/255.255.255.0, 10.10.10.1", "192.168.1.2/24,10.10.10.1/24")]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "192.168.1.2/24, 100.10.10.1", "192.168.1.2/24")]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "194.168.1.2/24, 100.10.10.1", "")]

        public void TestCollectionEquality(string source, string dest, string result)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (dest == null)
            {
                throw new ArgumentNullException(nameof(dest));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var conf = new NetworkConfiguration()
            {
                EnableIPV6 = true,
                EnableIPV4 = true
            };

            using var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());

            // Test included, IP6.
            var ncSource = nm.CreateIPCollection(source.Split(','), false, false);
            var ncDest = nm.CreateIPCollection(dest.Split(','), false, false);
            var ncResult = ncSource.ThatAreContainedInNetworks(ncDest);
            var resultCollection = nm.CreateIPCollection(result.Split(','), false, false);
            Assert.True(Compare(ncResult, resultCollection));
        }

        [Theory]
        [InlineData("10.1.1.1/32", "10.1.1.1")]
        [InlineData("192.168.1.254/32", "192.168.1.254/255.255.255.255")]

        public void TestEquals(string source, string dest)
        {
            Assert.True(IPNetAddress.Parse(source).Equals(IPNetAddress.Parse(dest)));
            Assert.True(IPNetAddress.Parse(dest).Equals(IPNetAddress.Parse(source)));
        }

        [Theory]

        // Testing bind interfaces.
        // On my system eth16 is internal, eth11 external (Windows defines the indexes).
        //
        // This test is to replicate how DNLA requests work throughout the system.

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
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (bindAddresses == null)
            {
                throw new ArgumentNullException(nameof(bindAddresses));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var conf = new NetworkConfiguration()
            {
                LocalNetworkAddresses = bindAddresses.Split(','),
                EnableIPV6 = ipv6enabled,
                EnableIPV4 = true
            };

            NetworkManager.MockNetworkSettings = "192.168.1.208/24,-16,eth16|200.200.200.200/24,11,eth11";
            using var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());
            NetworkManager.MockNetworkSettings = string.Empty;

            _ = nm.TryParseInterface(result, out var resultObj);

            // Check to see if dns resolution is working. If not, skip test.
            _ = IPHost.TryParse(source, out var host);

            if (resultObj != null && host?.HasAddress == true)
            {
                result = ((IPNetAddress)resultObj[0]).ToString(true);
                var intf = nm.GetBindInterface(source, out _);

                Assert.Equal(result, intf);
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
        [InlineData("8.8.8.8", "192.168.1.0/24", "eth16,eth11", false, "0.0.0.0=http://helloworld.com", "http://helloworld.com")]

        // User on internal network, we're bound internal only, but the address isn't in the LAN - so return the override.
        [InlineData("10.10.10.10", "192.168.1.0/24", "eth16", false, "0.0.0.0=http://internalButNotDefinedAsLan.com", "http://internalButNotDefinedAsLan.com")]

        // User on internal network, no binding specified - so result is the 1st internal.
        [InlineData("192.168.1.1", "192.168.1.0/24", "", false, "0.0.0.0=http://helloworld.com", "eth16")]

        // User on external network, internal binding only - so assumption is a proxy forward, return external override.
        [InlineData("jellyfin.org", "192.168.1.0/24", "eth16", false, "0.0.0.0=http://helloworld.com", "http://helloworld.com")]

        // User on external network, no binding - so result is the 1st external which is overriden.
        [InlineData("jellyfin.org", "192.168.1.0/24", "", false, "0.0.0.0 = http://helloworld.com", "http://helloworld.com")]

        // User assumed to be internal, no binding - so result is the 1st internal.
        [InlineData("", "192.168.1.0/24", "", false, "0.0.0.0=http://helloworld.com", "eth16")]

        // User is internal, no binding - so result is the 1st internal, which is then overridden.
        [InlineData("192.168.1.1", "192.168.1.0/24", "", false, "eth16=http://helloworld.com", "http://helloworld.com")]
        public void TestBindInterfaceOverrides(string source, string lan, string bindAddresses, bool ipv6enabled, string publishedServers, string result)
        {
            if (lan == null)
            {
                throw new ArgumentNullException(nameof(lan));
            }

            if (bindAddresses == null)
            {
                throw new ArgumentNullException(nameof(bindAddresses));
            }

            var conf = new NetworkConfiguration()
            {
                LocalNetworkSubnets = lan.Split(','),
                LocalNetworkAddresses = bindAddresses.Split(','),
                EnableIPV6 = ipv6enabled,
                EnableIPV4 = true,
                PublishedServerUriBySubnet = new string[] { publishedServers }
            };

            NetworkManager.MockNetworkSettings = "192.168.1.208/24,-16,eth16|200.200.200.200/24,11,eth11";
            using var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());
            NetworkManager.MockNetworkSettings = string.Empty;

            if (nm.TryParseInterface(result, out var resultObj) && resultObj != null)
            {
                // Parse out IPAddresses so we can do a string comparison. (Ignore subnet masks).
                result = ((IPNetAddress)resultObj[0]).ToString(true);
            }

            var intf = nm.GetBindInterface(source, out int? _);

            Assert.Equal(result, intf);
        }

        [Theory]
        [InlineData("185.10.10.10,200.200.200.200", "79.2.3.4", true)]
        [InlineData("185.10.10.10", "185.10.10.10", false)]
        [InlineData("", "100.100.100.100", false)]

        public void HasRemoteAccess_GivenWhitelist_AllowsOnlyIpsInWhitelist(string addresses, string remoteIp, bool denied)
        {
            // Comma separated list of IP addresses or IP/netmask entries for networks that will be allowed to connect remotely.
            // If left blank, all remote addresses will be allowed.
            var conf = new NetworkConfiguration()
            {
                EnableIPV4 = true,
                RemoteIPFilter = addresses.Split(','),
                IsRemoteIPFilterBlacklist = false
            };
            using var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());

            Assert.NotEqual(denied, nm.HasRemoteAccess(IPAddress.Parse(remoteIp)));
        }

        [Theory]
        [InlineData("185.10.10.10", "79.2.3.4", false)]
        [InlineData("185.10.10.10", "185.10.10.10", true)]
        [InlineData("", "100.100.100.100", false)]
        public void HasRemoteAccess_GivenBlacklist_BlacklistTheIps(string addresses, string remoteIp, bool denied)
        {
            // Comma separated list of IP addresses or IP/netmask entries for networks that will be allowed to connect remotely.
            // If left blank, all remote addresses will be allowed.
            var conf = new NetworkConfiguration()
            {
                EnableIPV4 = true,
                RemoteIPFilter = addresses.Split(','),
                IsRemoteIPFilterBlacklist = true
            };

            using var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());

            Assert.NotEqual(denied, nm.HasRemoteAccess(IPAddress.Parse(remoteIp)));
        }

        [Theory]
        [InlineData("192.168.1.209/24,-16,eth16", "192.168.1.0/24", "", "192.168.1.209")] // Only 1 address so use it.
        [InlineData("192.168.1.208/24,-16,eth16|10.0.0.1/24,10,eth7", "192.168.1.0/24", "", "192.168.1.208")] // LAN address is specified by default.
        [InlineData("192.168.1.208/24,-16,eth16|10.0.0.1/24,10,eth7", "192.168.1.0/24", "10.0.0.1", "10.0.0.1")] // return bind address

        public void Get_Appropriate_Interface_NoSource(string interfaces, string lan, string bind, string result)
        {
            var conf = new NetworkConfiguration()
            {
                EnableIPV4 = true,
                LocalNetworkSubnets = lan.Split(','),
                LocalNetworkAddresses = bind.Split(',')
            };

            NetworkManager.MockNetworkSettings = interfaces;
            using var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());

            var interfaceToUse = nm.GetBindInterface(string.Empty, out _);

            Assert.Equal(result, interfaceToUse);
        }

        [Theory]
        [InlineData("192.168.1.209/24,-16,eth16", "192.168.1.0/24", "", "192.168.1.210", "192.168.1.209")] // Source on LAN
        [InlineData("192.168.1.208/24,-16,eth16|10.0.0.1/24,10,eth7", "192.168.1.0/24", "", "192.168.1.209", "192.168.1.208")] // Source on LAN
        [InlineData("192.168.1.208/24,-16,eth16|10.0.0.1/24,10,eth7", "192.168.1.0/24", "", "8.8.8.8", "10.0.0.1")] // Source external.
        [InlineData("192.168.1.208/24,-16,eth16|10.0.0.1/24,10,eth7", "192.168.1.0/24", "10.0.0.1", "192.168.1.209", "10.0.0.1")] // LAN not bound, so return external.
        [InlineData("192.168.1.208/24,-16,eth16|10.0.0.1/24,10,eth7", "192.168.1.0/24", "192.168.1.208,10.0.0.1", "8.8.8.8", "10.0.0.1")] // return external bind address
        [InlineData("192.168.1.208/24,-16,eth16|10.0.0.1/24,10,eth7", "192.168.1.0/24", "192.168.1.208,10.0.0.1", "192.168.1.210", "192.168.1.208")] // return LAN bind address
        public void Get_Appropriate_Interface_ForSource(string interfaces, string lan, string bind, string source, string result)
        {
            var conf = new NetworkConfiguration()
            {
                EnableIPV4 = true,
                LocalNetworkSubnets = lan.Split(','),
                LocalNetworkAddresses = bind.Split(',')
            };

            NetworkManager.MockNetworkSettings = interfaces;
            using var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());

            var interfaceToUse = nm.GetBindInterface(source, out _);

            Assert.Equal(result, interfaceToUse);
        }
    }
}
