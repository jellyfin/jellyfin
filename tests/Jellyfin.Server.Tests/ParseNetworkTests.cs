using System;
using System.Linq;
using System.Net;
using Jellyfin.Networking.Configuration;
using Jellyfin.Networking.Manager;
using Jellyfin.Server.Extensions;
using MediaBrowser.Common.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests
{
    public class ParseNetworkTests
    {
        public static TheoryData<bool, bool, string[], IPAddress[], IPNetwork[]> TestNetworks_TestData()
        {
            var data = new TheoryData<bool, bool, string[], IPAddress[], IPNetwork[]>();
            data.Add(
                true,
                true,
                new string[] { "192.168.t", "127.0.0.1", "1234.1232.12.1234" },
                new IPAddress[] { IPAddress.Loopback.MapToIPv6() },
                Array.Empty<IPNetwork>());

            data.Add(
                true,
                false,
                new string[] { "192.168.x", "127.0.0.1", "1234.1232.12.1234" },
                new IPAddress[] { IPAddress.Loopback },
                Array.Empty<IPNetwork>());

            data.Add(
                true,
                true,
                new string[] { "::1" },
                Array.Empty<IPAddress>(),
                new IPNetwork[] { new IPNetwork(IPAddress.IPv6Loopback, 128) });

            data.Add(
                false,
                false,
                new string[] { "localhost" },
                Array.Empty<IPAddress>(),
                Array.Empty<IPNetwork>());

            data.Add(
                true,
                false,
                new string[] { "localhost" },
                new IPAddress[] { IPAddress.Loopback },
                Array.Empty<IPNetwork>());

            data.Add(
                false,
                true,
                new string[] { "localhost" },
                Array.Empty<IPAddress>(),
                new IPNetwork[] { new IPNetwork(IPAddress.IPv6Loopback, 128) });

            data.Add(
                true,
                true,
                new string[] { "localhost" },
                new IPAddress[] { IPAddress.Loopback.MapToIPv6() },
                new IPNetwork[] { new IPNetwork(IPAddress.IPv6Loopback, 128) });
            return data;
        }

        [Theory]
        [MemberData(nameof(TestNetworks_TestData))]
        public void TestNetworks(bool ip4, bool ip6, string[] hostList, IPAddress[] knownProxies, IPNetwork[] knownNetworks)
        {
            using var nm = CreateNetworkManager();

            var settings = new NetworkConfiguration
            {
                EnableIPV4 = ip4,
                EnableIPV6 = ip6
            };

            ForwardedHeadersOptions options = new ForwardedHeadersOptions();

            // Need this here as ::1 and 127.0.0.1 are in them by default.
            options.KnownProxies.Clear();
            options.KnownNetworks.Clear();

            ApiServiceCollectionExtensions.AddProxyAddresses(settings, hostList, options);

            Assert.Equal(knownProxies.Length, options.KnownProxies.Count);
            foreach (var item in knownProxies)
            {
                Assert.True(options.KnownProxies.Contains(item));
            }

            Assert.Equal(knownNetworks.Length, options.KnownNetworks.Count);
            foreach (var item in knownNetworks)
            {
                Assert.NotNull(options.KnownNetworks.FirstOrDefault(x => x.Prefix.Equals(item.Prefix) && x.PrefixLength == item.PrefixLength));
            }
        }

        private static IConfigurationManager GetMockConfig(NetworkConfiguration conf)
        {
            var configManager = new Mock<IConfigurationManager>
            {
                CallBase = true
            };
            configManager.Setup(x => x.GetConfiguration(It.IsAny<string>())).Returns(conf);
            return configManager.Object;
        }

        private static NetworkManager CreateNetworkManager()
        {
            var conf = new NetworkConfiguration()
            {
                EnableIPV6 = true,
                EnableIPV4 = true,
            };

            return new NetworkManager(GetMockConfig(conf), new NullLogger<NetworkManager>());
        }
    }
}
