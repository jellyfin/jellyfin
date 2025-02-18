using System;
using System.Linq;
using System.Net;
using Jellyfin.Networking.Manager;
using Jellyfin.Server.Extensions;
using MediaBrowser.Common.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using IConfigurationManager = MediaBrowser.Common.Configuration.IConfigurationManager;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace Jellyfin.Server.Tests;

public class ParseNetworkTests
{
    public static TheoryData<bool, bool, string[], IPAddress[], IPNetwork[]> TestNetworks_TestData()
    {
        var data = new TheoryData<bool, bool, string[], IPAddress[], IPNetwork[]>();
        data.Add(
            true,
            true,
            ["192.168.t", "127.0.0.1", "::1", "1234.1232.12.1234"],
            [IPAddress.Loopback],
            [new IPNetwork(IPAddress.IPv6Loopback, 128)]);

        data.Add(
            true,
            false,
            ["192.168.x", "127.0.0.1", "1234.1232.12.1234"],
            [IPAddress.Loopback],
            []);

        data.Add(
            true,
            true,
            ["::1"],
            [],
            [new IPNetwork(IPAddress.IPv6Loopback, 128)]);

        data.Add(
            false,
            false,
            ["localhost"],
            [],
            []);

        data.Add(
            true,
            false,
            ["localhost"],
            [IPAddress.Loopback],
            []);

        data.Add(
            false,
            true,
            ["localhost"],
            [],
            [new IPNetwork(IPAddress.IPv6Loopback, 128)]);

        data.Add(
            true,
            true,
            ["localhost"],
            [IPAddress.Loopback],
            [new IPNetwork(IPAddress.IPv6Loopback, 128)]);
        return data;
    }

    [Theory]
    [MemberData(nameof(TestNetworks_TestData))]
    public void TestNetworks(bool ip4, bool ip6, string[] hostList, IPAddress[] knownProxies, IPNetwork[] knownNetworks)
    {
        using var nm = CreateNetworkManager();

        var settings = new NetworkConfiguration
        {
            EnableIPv4 = ip4,
            EnableIPv6 = ip6
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
            EnableIPv6 = true,
            EnableIPv4 = true,
        };
        var startupConf = new Mock<IConfiguration>();
        return new NetworkManager(GetMockConfig(conf), startupConf.Object, new NullLogger<NetworkManager>());
    }
}
