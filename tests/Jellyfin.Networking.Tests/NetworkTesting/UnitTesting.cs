using System;
using System.Net;
using Jellyfin.Networking.Manager;
using MediaBrowser.Common.Configuration;
using NetworkCollection;
using NetworkCollection.Udp;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Jellyfin.Networking.Configuration;

namespace NetworkTesting
{
    using XMLProperties = System.Collections.Generic.Dictionary<string, string>;

    public class NetTesting
    {
        /// <summary>
        /// Trys to identify the string and return an object of that class.
        /// </summary>
        /// <param name="addr">String to parse.</param>
        /// <param name="result">IPObject to return.</param>
        /// <returns>True if the value parsed successfully.</returns>
        private static bool TryParse(string addr, out IPObject result)
        {
            if (!string.IsNullOrEmpty(addr))
            {
                // Is it an IP address
                if (IPNetAddress.TryParse(addr, out IPNetAddress nw))
                {
                    result = nw;
                    return true;
                }

                if (IPHost.TryParse(addr, out IPHost h))
                {
                    result = h;
                    return true;
                }
            }

            result = IPNetAddress.None;
            return false;
        }


        private IConfigurationManager GetMockConfig(NetworkConfiguration conf)
        {
            var configManager = new Mock<IConfigurationManager>
            {
                CallBase = true
            };
            configManager.Setup(x => x.GetConfiguration(It.IsAny<string>())).Returns(conf);
            return (IConfigurationManager)configManager.Object;
        }

        [Theory]
        [InlineData("192.168.1.208/24,-16,eth16:200.200.200.200/24,11,eth11", "192.168.1.0/24;200.200.200.0/24", "[192.168.1.208/24,200.200.200.200/24]")]
        [InlineData("192.168.1.208/24,-16,eth16:200.200.200.200/24,11,eth11", "192.168.1.0/24", "[192.168.1.208/24]")]
        [InlineData("192.168.1.208/24,-16,vEthernet1:192.168.1.208/24,-16,vEthernet212;200.200.200.200/24,11,eth11", "192.168.1.0/24", "[192.168.1.208/24]")]
        public void IgnoreVirtualInterfaces(string interfaces, string lan, string value)
        {
            var conf = new NetworkConfiguration()
            {
                EnableIPV6 = true,
                EnableIPV4 = true,
                LocalNetworkSubnets = lan.Split(';')
            };

            NetworkManager.MockNetworkSettings = interfaces;
            var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());
            NetworkManager.MockNetworkSettings = string.Empty;

            Assert.True(string.Equals(nm.GetInternalBindAddresses().ToString(), value, StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("192.168.10.0/24, !192.168.10.60/32", "192.168.10.60")]
        public void TextIsInNetwork(string network, string value)
        {
            var conf = new NetworkConfiguration()
            {
                EnableIPV6 = true,
                EnableIPV4 = true,
                LocalNetworkSubnets = network.Split(',')
            };

            var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());

            Assert.True(!nm.IsInLocalNetwork(value));
        }

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
        [InlineData("192.168.1.2/255.255.255.0")]
        [InlineData("192.168.1.2/24")]

        public void TestCollectionCreation(string address)
        {
            Assert.True(TryParse(address, out _));
        }

        [Theory]
        [InlineData("256.128.0.0.0.1")]
        [InlineData("127.0.0.1#")]
        [InlineData("localhost!")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517:1231")]
        public void TestInvalidCollectionCreation(string address)
        {
            Assert.False(TryParse(address, out _));
        }

        [Theory]
        // Src, IncIP6, incIP4, exIP6, ecIP4, net
        [InlineData("127.0.0.1#",
            "[]",
            "[]",
            "[]",
            "[]",
            "[]")]
        [InlineData("[127.0.0.1]",
            "[]",
            "[]",
            "[127.0.0.1/32]",
            "[127.0.0.1/32]",
            "[]")]
        [InlineData("",
            "[]",
            "[]",
            "[]",
            "[]",
            "[]")]
        [InlineData("192.158.1.2/255.255.0.0,192.169.1.2/8",
            "[192.158.1.2/16,192.169.1.2/8]",
            "[192.158.1.2/16,192.169.1.2/8]",
            "[]",
            "[]",
            "[192.158.0.0/16,192.0.0.0/8]")]
        [InlineData("192.158.1.2/16, localhost, fd23:184f:2029:0:3139:7386:67d7:d517,    [10.10.10.10]",
            "[192.158.1.2/16,127.0.0.1/32,fd23:184f:2029:0:3139:7386:67d7:d517/128]",
            "[192.158.1.2/16,127.0.0.1/32]",
            "[10.10.10.10/32]",
            "[10.10.10.10/32]",
            "[192.158.0.0/16,127.0.0.1/32,fd23:184f:2029:0:3139:7386:67d7:d517/128]")]
        public void TestCollections(string settings, string result1, string result2, string result3, string result4, string result5)
        {
            var conf = new NetworkConfiguration()
            {
                EnableIPV6 = true,
                EnableIPV4 = true,
            };

            var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());

            // Test included, IP6.
            NetCollection nc = nm.CreateIPCollection(settings.Split(","), false);
            Assert.True(string.Equals(nc.ToString(), result1, System.StringComparison.OrdinalIgnoreCase));

            // Text excluded, non IP6.
            nc = nm.CreateIPCollection(settings.Split(","), true);
            Assert.True(string.Equals(nc?.ToString(), result3, System.StringComparison.OrdinalIgnoreCase));

            conf.EnableIPV6 = false;
            nm.UpdateSettings(conf);

            // Test included, non IP6.
            nc = nm.CreateIPCollection(settings.Split(","), false);
            Assert.True(string.Equals(nc.ToString(), result2, System.StringComparison.OrdinalIgnoreCase));

            // Test excluded, including IPv6.
            nc = nm.CreateIPCollection(settings.Split(","), true);
            Assert.True(string.Equals(nc.ToString(), result4, System.StringComparison.OrdinalIgnoreCase));

            conf.EnableIPV6 = true;
            nm.UpdateSettings(conf);

            // Test network addresses of collection.
            nc = nm.CreateIPCollection(settings.Split(","), false);
            nc = nc.AsNetworks();
            Assert.True(string.Equals(nc.ToString(), result5, System.StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("127.0.0.1", "fd23:184f:2029:0:3139:7386:67d7:d517/64,fd23:184f:2029:0:c0f0:8a8a:7605:fffa/128,fe80::3139:7386:67d7:d517%16/64,192.168.1.208/24,::1/128,127.0.0.1/8", "[127.0.0.1/32]")]
        [InlineData("127.0.0.1", "127.0.0.1/8", "[127.0.0.1/32]")]
        public void UnionCheck(string settings, string compare, string result)
        {
            var conf = new NetworkConfiguration()
            {
                EnableIPV6 = true,
                EnableIPV4 = true,
            };

            var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());

            NetCollection nc1 = nm.CreateIPCollection(settings.Split(","), false);
            NetCollection nc2 = nm.CreateIPCollection(compare.Split(","), false);

            Assert.True(nc1.Union(nc2).ToString() == result);
        }

        [Theory]
        [InlineData("192.168.5.85/24", "192.168.5.1")]
        [InlineData("192.168.5.85/24", "192.168.5.254")]
        [InlineData("10.128.240.50/30", "10.128.240.48")]
        [InlineData("10.128.240.50/30", "10.128.240.49")]
        [InlineData("10.128.240.50/30", "10.128.240.50")]
        [InlineData("10.128.240.50/30", "10.128.240.51")]
        [InlineData("127.0.0.1/8", "127.0.0.1")]
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

        public void TestSubnets(string network, string ip)
        {
            Assert.True(TryParse(network, out IPObject? networkObj));
            Assert.True(TryParse(ip, out IPObject? ipObj));

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
            Assert.True(networkObj.Contains(ipObj));
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        [Theory]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "172.168.1.2/24", "172.168.1.2/24")]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "172.168.1.2/24, 10.10.10.1", "172.168.1.2/24,10.10.10.1/24")]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "192.168.1.2/255.255.255.0, 10.10.10.1", "192.168.1.2/24,10.10.10.1/24")]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "192.168.1.2/24, 100.10.10.1", "192.168.1.2/24")]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "194.168.1.2/24, 100.10.10.1", "")]

        public void TestMatches(string source, string dest, string result)
        {
            var conf = new NetworkConfiguration()
            {
                EnableIPV6 = true,
                EnableIPV4 = true
            };

            var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());

            // Test included, IP6.
            NetCollection ncSource = nm.CreateIPCollection(source.Split(","));
            NetCollection ncDest = nm.CreateIPCollection(dest.Split(","));
            NetCollection ncResult = ncSource.Union(ncDest);
            NetCollection resultCollection = nm.CreateIPCollection(result.Split(","));
            Assert.True(ncResult.Equals(resultCollection));
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

        // Testing bind interfaces. These are set for my system so won't work elsewhere.
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
        // User assumed to be internal, no binding - so result is the 1st internal.
        [InlineData("", "", false, "eth16")]
        public void TestBindInterfaces(string source, string bindAddresses, bool ipv6enabled, string result)
        {
            var conf = new NetworkConfiguration()
            {
                LocalNetworkAddresses = bindAddresses.Split(','),
                EnableIPV6 = ipv6enabled,
                EnableIPV4 = true
            };

            NetworkManager.MockNetworkSettings = "192.168.1.208/24,-16,eth16:200.200.200.200/24,11,eth11";
            var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());
            NetworkManager.MockNetworkSettings = string.Empty;

            _ = nm.TryParseInterface(result, out NetCollection? resultObj);

            if (resultObj != null)
            {
                result = ((IPNetAddress)resultObj[0]).ToString(true);
                var intf = nm.GetBindInterface(source, out int? _);

                Assert.True(string.Equals(intf, result, System.StringComparison.OrdinalIgnoreCase));
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

        // User on external network, internal binding only - so asumption is a proxy forward, return external override.
        [InlineData("jellyfin.org", "192.168.1.0/24", "eth16", false, "0.0.0.0=http://helloworld.com", "http://helloworld.com")]

        // User on external network, no binding - so result is the 1st external which is overriden.
        [InlineData("jellyfin.org", "192.168.1.0/24", "", false, "0.0.0.0 = http://helloworld.com", "http://helloworld.com")]

        // User assumed to be internal, no binding - so result is the 1st internal.
        [InlineData("", "192.168.1.0/24", "", false, "0.0.0.0=http://helloworld.com", "eth16")]

        // User is internal, no binding - so result is the 1st internal, which is then overridden.
        [InlineData("192.168.1.1", "192.168.1.0/24", "", false, "eth16=http://helloworld.com", "http://helloworld.com")]

        public void TestBindInterfaceOverrides(string source, string lan, string bindAddresses, bool ipv6enabled, string publishedServers, string result)
        {
            var conf = new NetworkConfiguration()
            {
                LocalNetworkSubnets = lan.Split(','),
                LocalNetworkAddresses = bindAddresses.Split(','),
                EnableIPV6 = ipv6enabled,
                EnableIPV4 = true,
                PublishedServerUriBySubnet = new string[] { publishedServers }
            };

            NetworkManager.MockNetworkSettings = "192.168.1.208/24,-16,eth16:200.200.200.200/24,11,eth11";
            var nm = new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());
            NetworkManager.MockNetworkSettings = string.Empty;

            if (nm.TryParseInterface(result, out NetCollection? resultObj) && resultObj != null)
            {
                // Parse out IPAddresses so we can do a string comparison. (Ignore subnet masks).
                result = ((IPNetAddress)resultObj[0]).ToString(true);
            }

            var intf = nm.GetBindInterface(source, out int? _);

            Assert.True(string.Equals(intf, result, System.StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        // range specified.
        [InlineData("10-12", 10, 12)]
        // range specified.
        [InlineData("12 - 14", 12, 14)]
        // range specified but in the wrong order.
        [InlineData("12 - 1", 1, 12)]
        // No starting value, so 1 assumed.
        [InlineData(" - 1", 1, 1)]
        // No starting value, so 1 assumed.
        [InlineData("-1", 1, 1)]
        // Range not defined = random port.
        [InlineData("", 1, 65535)]
        // Range invalid, but two numbers specified.
        [InlineData("-14-12", 1, 65535)]

        [InlineData("12      -", 12, 65535)]
        [InlineData("0      - 1202020", 1, 65535)]
        public void TestRange(string rangeStr, int min, int max)
        {
            rangeStr.TryParseRange(out (int Min, int Max) range);
            Assert.True((range.Min == min) && (range.Max == max));
        }
    }
}
