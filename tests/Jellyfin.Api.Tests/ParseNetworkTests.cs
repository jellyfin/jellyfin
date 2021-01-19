using System;
using System.Globalization;
using System.Text;
using Jellyfin.Networking.Configuration;
using Jellyfin.Networking.Manager;
using Jellyfin.Server.Extensions;
using MediaBrowser.Common.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests
{
    public class ParseNetworkTests
    {
        /// <summary>
        /// Order of the result has always got to be hosts, then networks.
        /// </summary>
        /// <param name="ip4">IP4 enabled.</param>
        /// <param name="ip6">IP6 enabled.</param>
        /// <param name="hostList">List to parse.</param>
        /// <param name="match">What it should match.</param>
        [Theory]
        // [InlineData(true, true, "192.168.0.0/16,www.yahoo.co.uk", "::ffff:212.82.100.150,::ffff:192.168.0.0/16")]  <- fails on Max. www.yahoo.co.uk resolves to a different ip address.
        // [InlineData(true, false, "192.168.0.0/16,www.yahoo.co.uk", "212.82.100.150,192.168.0.0/16")]
        [InlineData(true, true, "192.168.t,127.0.0.1,1234.1232.12.1234", "::ffff:127.0.0.1")]
        [InlineData(true, false, "192.168.x,127.0.0.1,1234.1232.12.1234", "127.0.0.1")]
        [InlineData(true, true, "::1", "::1/128")]
        public void TestNetworks(bool ip4, bool ip6, string hostList, string match)
        {
            using var nm = CreateNetworkManager();

            var settings = new NetworkConfiguration
            {
                EnableIPV4 = ip4,
                EnableIPV6 = ip6
            };

            var result = match + ',';
            ForwardedHeadersOptions options = new ForwardedHeadersOptions();

            // Need this here as ::1 and 127.0.0.1 are in them by default.
            options.KnownProxies.Clear();
            options.KnownNetworks.Clear();

            ApiServiceCollectionExtensions.AddProxyAddresses(settings, hostList.Split(","), options);

            var sb = new StringBuilder();
            foreach (var item in options.KnownProxies)
            {
                sb.Append(item);
                sb.Append(',');
            }

            foreach (var item in options.KnownNetworks)
            {
                sb.Append(item.Prefix);
                sb.Append('/');
                sb.Append(item.PrefixLength.ToString(CultureInfo.InvariantCulture));
                sb.Append(',');
            }

            Assert.Equal(sb.ToString(), result);
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
