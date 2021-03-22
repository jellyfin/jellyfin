using System;
using System.Collections.ObjectModel;
using System.Net;
using Jellyfin.Networking.Configuration;
using Jellyfin.Networking.Manager;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Networking.Tests
{
    public class NetworkParseTests
    {
        /// <summary>
        /// Checks if the specified string is a valid IP host (IP address or domain name).
        /// </summary>
        /// <param name="address">The host to check.</param>
        /// <param name="hostname">The hostname which should be parsed, or the ip address if not a host name.</param>
        [Theory]
        [InlineData("127.0.0.1", "127.0.0.1")]
        [InlineData("127.0.0.1:123", "127.0.0.1")]
        [InlineData("localhost", "localhost")]
        [InlineData("localhost:1345", "localhost")]
        [InlineData("www.google.co.uk", "www.google.co.uk")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517", "fd23:184f:2029:0:3139:7386:67d7:d517")]
        [InlineData("[fd23:184f:2029:0:3139:7386:67d7:d517]:124", "fd23:184f:2029:0:3139:7386:67d7:d517")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517/56", "fd23:184f:2029:0:3139:7386:67d7:d517")]
        [InlineData("fe80::7add:12ff:febb:c67b%16", "fe80::7add:12ff:febb:c67b")]
        [InlineData("[fe80::7add:12ff:febb:c67b%16]:123", "fe80::7add:12ff:febb:c67b")]
        [InlineData("fe80::7add:12ff:febb:c67b%16:123", "fe80::7add:12ff:febb:c67b")]
        [InlineData("[fe80::7add:12ff:febb:c67b%16]", "fe80::7add:12ff:febb:c67b")]
        [InlineData("192.168.1.2/255.255.255.0", "192.168.1.2")]
        [InlineData("192.168.1.2/24", "192.168.1.2")]
        public void Parse_ValidIPHost_Success(string address, string hostname)
        {
            Assert.True(IPHost.TryParse(address, out IPHost parsed));
            Assert.NotNull(parsed);
            Assert.Equal(hostname, parsed.HostName);
        }

        /// <summary>
        /// Checks if the specified string is a valid IP address.
        /// </summary>
        /// <param name="address">The IP to check.</param>
        /// <param name="verify">The IP address which should be parsed.</param>
        [Theory]
        [InlineData("127.0.0.1", "127.0.0.1")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517", "fd23:184f:2029:0:3139:7386:67d7:d517")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517/56", "fd23:184f:2029:0:3139:7386:67d7:d517")]
        [InlineData("[fd23:184f:2029:0:3139:7386:67d7:d517]", "fd23:184f:2029:0:3139:7386:67d7:d517")]
        [InlineData("fe80::7add:12ff:febb:c67b%16:123", "fe80::7add:12ff:febb:c67b")]
        [InlineData("192.168.1.2/255.255.255.0", "192.168.1.2")]
        [InlineData("192.168.1.2/24", "192.168.1.2")]
        // TODO: Tests failing
        // [InlineData("[fe80::7add:12ff:febb:c67b%16]", "fe80::7add:12ff:febb:c67b")]
        // [InlineData("fe80::7add:12ff:febb:c67b%16", "fe80::7add:12ff:febb:c67b")]
        // [InlineData("[fe80::7add:12ff:febb:c67b%16]:123", "fe80::7add:12ff:febb:c67b")]
        public void Parse_ValidIPNetAddress_Success(string address, string verify)
        {
            Assert.True(IPNetAddress.TryParse(address, out IPNetAddress parsed));
            Assert.NotNull(parsed);
            Assert.Equal(verify, parsed.Address.ToString());
        }

        /// <summary>
        /// Check that invalid IP addresses don't get parsed.
        /// </summary>
        /// <param name="address">The IP address to check.</param>
        [Theory]
        [InlineData("256.128.0.0.0.1")]
        [InlineData("127.0.0.1#")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517:1231")]
        [InlineData("[fd23:184f:2029:0:3139:7386:67d7:d517:1231]")]
        public void Parse_InvalidIPNetAddress_Fail(string address)
        {
            Assert.False(IPNetAddress.TryParse(address, out _));
        }

        /// <summary>
        /// Check that invalid IP hosts don't get parsed.
        /// </summary>
        /// <param name="host">The IP host to check.</param>
        [Theory]
        [InlineData("256.128.0.0.0.1")]
        [InlineData("127.0.0.1#")]
        [InlineData("localhost!")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517:1231")]
        [InlineData("[fd23:184f:2029:0:3139:7386:67d7:d517:1231]")]
        public void Parse_InvalidIPHost_Fail(string host)
        {
            Assert.False(IPHost.TryParse(host, out _));
        }

        /// <summary>
        /// Check that subnet contains IP address.
        /// </summary>
        /// <param name="subnet">The IP subnet.</param>
        /// <param name="ip">The IP address which should be in the subnet.</param>
        [Theory]
        [InlineData("192.168.5.85/24", "192.168.5.1")]
        [InlineData("192.168.5.85/24", "192.168.5.254")]
        [InlineData("10.128.240.50/30", "10.128.240.48")]
        [InlineData("10.128.240.50/30", "10.128.240.49")]
        [InlineData("10.128.240.50/30", "10.128.240.50")]
        [InlineData("10.128.240.50/30", "10.128.240.51")]
        [InlineData("127.0.0.1/8", "127.0.0.1")]
        [InlineData("10.0.0.0/255.0.0.0", "10.10.10.1")]
        [InlineData("10.10.0.0/255.255.0.0", "10.10.10.1")]
        [InlineData("10.10.10.0/255.255.255.0", "10.10.10.1")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:0000:0000:0000:0000")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:FFFF:FFFF:FFFF:FFFF")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:0001:0000:0000:0000")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:FFFF:FFFF:FFFF:FFF0")]
        [InlineData("2001:db8:abcd:0012::0/128", "2001:0DB8:ABCD:0012:0000:0000:0000:0000")]
        public void SubnetContainsIP_Yes_Success(string subnet, string ip)
        {
            var ipAddress = IPAddress.Parse(ip);
            var ipSubnet = IPNetAddress.Parse(subnet);
            Assert.True(ipSubnet.Contains(ipAddress));
        }

        /// <summary>
        /// Check that subnet doesn't contains IP address.
        /// </summary>
        /// <param name="subnet">The IP subnet.</param>
        /// <param name="ip">The IP address which shouldn't be in the subnet.</param>
        [Theory]
        [InlineData("192.168.5.85/24", "192.168.4.254")]
        [InlineData("192.168.5.85/24", "191.168.5.254")]
        [InlineData("10.128.240.50/30", "10.128.240.47")]
        [InlineData("10.128.240.50/30", "10.128.240.52")]
        [InlineData("10.128.240.50/30", "10.128.239.50")]
        [InlineData("10.128.240.50/30", "10.127.240.51")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0011:FFFF:FFFF:FFFF:FFFF")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0013:0000:0000:0000:0000")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0013:0001:0000:0000:0000")]
        [InlineData("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0011:FFFF:FFFF:FFFF:FFF0")]
        [InlineData("2001:db8:abcd:0012::0/128", "2001:0DB8:ABCD:0012:0000:0000:0000:0001")]
        public void SubnetContainsIP_No_Failure(string subnet, string ip)
        {
            var ipAddress = IPAddress.Parse(ip);
            var ipSubnet = IPNetAddress.Parse(subnet);
            Assert.False(ipSubnet.Contains(ipAddress));
        }

        /// <summary>
        /// Check that subnet contains submet.
        /// </summary>
        /// <param name="bigSubnet">The wider subnet.</param>
        /// <param name="smallSubnet">The narrower subnet.</param>
        [Theory]
        [InlineData("10.0.0.0/255.0.0.0", "10.10.10.1/32")]
        [InlineData("10.0.0.0/8", "10.10.10.1/32")]
        [InlineData("10.10.0.0/255.255.0.0", "10.10.10.1/32")]
        [InlineData("10.10.0.0/16", "10.10.10.1/32")]
        [InlineData("10.10.10.0/255.255.255.0", "10.10.10.1/32")]
        [InlineData("10.10.10.0/24", "10.10.10.1/32")]
        public void SubnetContainsSubnet_Yes_Success(string bigSubnet, string smallSubnet)
        {
            Assert.True(IPNetAddress.TryParse(bigSubnet, out IPNetAddress bigSubnetIp));
            Assert.True(IPNetAddress.TryParse(smallSubnet, out IPNetAddress smallSubnetIp));
            Assert.True(bigSubnetIp.Contains(smallSubnetIp));
        }

        /// <summary>
        /// Unite 2 Subnet lists check result.
        /// </summary>
        /// <param name="subnet1">The first subnet.</param>
        /// <param name="subnet2">The second subnet.</param>
        /// <param name="result">The resulting subnet.</param>
        [Theory]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "172.168.1.2/24", "172.168.1.2/24")]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "172.168.1.2/24, 10.10.10.1", "172.168.1.2/24,10.10.10.1/24")]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "192.168.1.2/255.255.255.0, 10.10.10.1", "192.168.1.2/24,10.10.10.1/24")]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "192.168.1.2/24, 100.10.10.1", "192.168.1.2/24")]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "194.168.1.2/24, 100.10.10.1", "")]
        public void UniteSubnets_Valid_Success(string subnet1, string subnet2, string result)
        {
            var conf = new NetworkConfiguration()
            {
                EnableIPV6 = true,
                EnableIPV4 = true
            };

            using var nm = new NetworkManager(NetworkManagerTests.GetMockConfig(conf), new NullLogger<NetworkManager>());

            Collection<IPObject> subnetList1 = nm.CreateIPCollection(subnet1.Split(","));
            Collection<IPObject> subnetList2 = nm.CreateIPCollection(subnet2.Split(","));
            Collection<IPObject> resultCollection = nm.CreateIPCollection(result.Split(","));

            Collection<IPObject> unitedSubnets = subnetList1.Union(subnetList2);

            Assert.True(unitedSubnets.Compare(resultCollection));
        }

        /// <summary>
        /// Check that multiple IP addresses or subnets containing one IP are equal.
        /// </summary>
        /// <param name="ip1">The first IP / subnet.</param>
        /// <param name="ip2">The second IP / subnet.</param>
        [Theory]
        [InlineData("10.1.1.1/32", "10.1.1.1")]
        [InlineData("192.168.1.254/32", "192.168.1.254/255.255.255.255")]
        public void TestEquals(string ip1, string ip2)
        {
            var ipAddress1 = IPNetAddress.Parse(ip1);
            var ipAddress2 = IPNetAddress.Parse(ip2);
            Assert.True(ipAddress1.Equals(ipAddress2));
            Assert.True(ipAddress2.Equals(ipAddress1));
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
            using var nm = new NetworkManager(NetworkManagerTests.GetMockConfig(conf), new NullLogger<NetworkManager>());
            NetworkManager.MockNetworkSettings = string.Empty;

            _ = nm.TryParseInterface(result, out Collection<IPObject>? resultObj);

            if (resultObj != null)
            {
                result = ((IPNetAddress)resultObj[0]).ToString(true);
                var intf = nm.GetBindInterface(source, out int? _);

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
            using var nm = new NetworkManager(NetworkManagerTests.GetMockConfig(conf), new NullLogger<NetworkManager>());
            NetworkManager.MockNetworkSettings = string.Empty;

            if (nm.TryParseInterface(result, out Collection<IPObject>? resultObj) && resultObj != null)
            {
                // Parse out IPAddresses so we can do a string comparison. (Ignore subnet masks).
                result = ((IPNetAddress)resultObj[0]).ToString(true);
            }

            var intf = nm.GetBindInterface(source, out int? _);

            Assert.Equal(result, intf);
        }
    }
}
