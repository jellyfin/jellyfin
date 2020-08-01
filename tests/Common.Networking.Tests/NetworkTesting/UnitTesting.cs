using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Networking;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Common.Networking;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace NetworkTesting
{
    public class NetTesting
    {
        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("127.0.0.1:123")]
        [InlineData("localhost")]
        [InlineData("localhost:1345")]
        [InlineData("www.google.co.uk")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517")]
        [InlineData("[fd23:184f:2029:0:3139:7386:67d7:d517]:124")]
        [InlineData("fe80::7add:12ff:febb:c67b%16")]
        [InlineData("[fe80::7add:12ff:febb:c67b%16]:123")]
        [InlineData("192.168.1.2/255.255.255.0")]
        [InlineData("192.168.1.2/24")]

        public void TestCollectionCreation(string address)
        {
            Assert.True(NetCollection.TryParse(address, out _));
        }

        [Theory]
        [InlineData("256.128.0.0.0.1")]
        [InlineData("127.0.0.1#")]
        [InlineData("localhost!")]
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517:1231")]
        public void TestInvalidCollectionCreation(string address)
        {
            Assert.False(NetCollection.TryParse(address, out _));
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
            "[192.158.1.2/16,127.0.0.1/32,fd23:184f:2029:0:3139:7386:67d7:d517]",
            "[192.158.1.2/16,127.0.0.1/32]",
            "[10.10.10.10/32]",
            "[10.10.10.10/32]",
            "[192.158.0.0/16,127.0.0.1/32,fd23:184f:2029::]")]
        public void TestCollections(string settings, string result1, string result2, string result3, string result4, string result5)
        {
            var conf = new ServerConfiguration()
            {
                EnableIPV6 = true
            };

            var confManagerMock = Mock.Of<IServerConfigurationManager>(x => x.Configuration == conf);

            var nm = new NetworkManager(confManagerMock, new NullLogger<NetworkManager>());

            // Test included, IP6.
            NetCollection nc = nm.CreateIPCollection(settings.Split(","), false);
            Assert.True(string.Equals(nc.ToString(), result1, System.StringComparison.OrdinalIgnoreCase));

            // Text excluded, non IP6.
            nc = nm.CreateIPCollection(settings.Split(","), true);
            Assert.True(string.Equals(nc?.ToString(), result3, System.StringComparison.OrdinalIgnoreCase));

            conf.EnableIPV6 = false;
            // Test included, non IP6.
            nc = nm.CreateIPCollection(settings.Split(","), false);
            Assert.True(string.Equals(nc.ToString(), result2, System.StringComparison.OrdinalIgnoreCase));

            // Test excluded, including IPv6.
            nc = nm.CreateIPCollection(settings.Split(","), true);
            Assert.True(string.Equals(nc.ToString(), result4, System.StringComparison.OrdinalIgnoreCase));

            conf.EnableIPV6 = true;
            // Test network addresses of collection.
            nc = nm.CreateIPCollection(settings.Split(","), false);
            nc = NetCollection.AsNetworks(nc);
            Assert.True(string.Equals(nc.ToString(), result5, System.StringComparison.OrdinalIgnoreCase));
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
            Assert.True(NetCollection.TryParse(network, out IPObject? networkObj));
            Assert.True(NetCollection.TryParse(ip, out IPObject? ipObj));

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
            Assert.True(networkObj.Contains(ipObj));
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        [Theory]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "172.168.1.2/24", "[172.168.1.2/24]")]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "172.168.1.2/24, 10.10.10.1", "[172.168.1.2/24,10.10.10.1/32]")]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "192.168.1.2/255.255.255.0, 10.10.10.1", "[192.168.1.2/24,10.10.10.1/32]")]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "192.168.1.2/24, 100.10.10.1", "[192.168.1.2/24]")]
        [InlineData("192.168.1.2/24,10.10.10.1/24,172.168.1.2/24", "194.168.1.2/24, 100.10.10.1", "[]")]

        public void TestMatches(string source, string dest, string result)
        {
            var conf = new ServerConfiguration()
            {
                EnableIPV6 = true
            };

            var confManagerMock = Mock.Of<IServerConfigurationManager>(x => x.Configuration == conf);

            var nm = new NetworkManager(confManagerMock, new NullLogger<NetworkManager>());

            // Test included, IP6.
            NetCollection ncSource = nm.CreateIPCollection(source.Split(","));
            NetCollection ncDest = nm.CreateIPCollection(dest.Split(","));
            string ncResult = ncSource.Union(ncDest).ToString();

            Assert.True(string.Equals(ncResult, result, System.StringComparison.OrdinalIgnoreCase));
        }


        [Theory]
        [InlineData("10.1.1.1/32", "10.1.1.1")]
        [InlineData("192.168.1.254/32", "192.168.1.254/255.255.255.255")]

        public void TestEquals(string source, string dest)
        {
            Assert.True(IPNetAddress.Parse(source).Equals(IPNetAddress.Parse(dest)));
            Assert.True(IPNetAddress.Parse(dest).Equals(IPNetAddress.Parse(source)));
        }

        private async Task<bool> TestAsync(IPObject address, CancellationToken cancellationToken)
        {
            await Task.Delay(5000-(1000*address.Tag));
            return address.Equals(IPAddress.Loopback);
        }

        [Theory]

        // Testing multi-task launching, and resolution. Returning after the 1st success.
        [InlineData("www.google.co.uk;www.helloworld.com;www.123.com;127.0.0.1")]
        public void TestCallback(string source)
        {

            var conf = new ServerConfiguration()
            {
                EnableIPV6 = true
            };

            var confManagerMock = Mock.Of<IServerConfigurationManager>(x => x.Configuration == conf);

            var nm = new NetworkManager(confManagerMock, new NullLogger<NetworkManager>());
            // Test included, IP6.
            NetCollection ncSource = nm.CreateIPCollection(source.Split(";"));

            // Mark each one so we know which is which
            ncSource[0].Tag = 1;
            ncSource[1].Tag = 2;
            ncSource[2].Tag = 3;
            ncSource[3].Tag = 4;

            // Last one should return first.
            NetCollection first = ncSource.Callback(TestAsync, default, 1);
            // test that we only have one response.
            Assert.True(first.Count == 1 && first[0].Tag == 4);
        }

        [Theory]

        // Testing bind interfaces. These are set for my system so won't work elsewhere.
        // On my system eth16 is internal, eth11 external (Windows defines the indexes).

        // User on internal network, we're bound internal and external - so result is internal.
        [InlineData("192.168.1.1", "eth16,eth11", false, "eth16")]
        // User on internal network, we're bound internal and external - so result is external.
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
            var conf = new ServerConfiguration()
            {
                InternalBindInterface = "192.168.1.207",
                ExternalBindInterface = "eth11",
                LocalNetworkAddresses = bindAddresses.Split(','),
                EnableIPV6 = ipv6enabled
            };

            var confManagerMock = Mock.Of<IServerConfigurationManager>(x => x.Configuration == conf);

            var nm = new NetworkManager(confManagerMock, new NullLogger<NetworkManager>());

            _ = nm.TryParseInterface(result, out IPNetAddress resultObj);

            Assert.True(nm.GetBindInterface(source).Equals(resultObj));
            
        }
    }
}
