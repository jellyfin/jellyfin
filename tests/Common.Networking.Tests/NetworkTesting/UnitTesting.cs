using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Networking;
using Emby.Server.Implementations.Networking;
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
            Assert.True(NetCollection.TryParse(address, out IPObject? result));
        }

        [Theory]
        [InlineData("256.128.0.0.0.1")]
        [InlineData("127.0.0.1#")]
        [InlineData("localhost!")]        
        [InlineData("fd23:184f:2029:0:3139:7386:67d7:d517:1231")]
        public void TestInvalidCollectionCreation(string address)
        {
            Assert.False(NetCollection.TryParse(address, out IPObject? result));
        }

        public static bool DisableIP6()
        {
            return false;
        }

        public static bool EnableIP6()
        {
            return true;
        }

        public static string[] NoParams()
        {
            return Array.Empty<string>();
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
            "[192.158.0.0/16,fd23:184f:2029::]")]
        public void TestCollections(string settings, string result1, string result2, string result3, string result4, string result5)
        {
            var nm = new NetworkManager(null, EnableIP6, NoParams, NoParams);
            // Test included, IP6.            
            NetCollection nc = nm.CreateIPCollection(settings.Split(","), false);           
            Assert.True(string.Equals(nc.ToString(), result1, System.StringComparison.OrdinalIgnoreCase));

            // Text excluded, non IP6.
            nc = nm.CreateIPCollection(settings.Split(","), true);
            Assert.True(string.Equals(nc?.ToString(), result3, System.StringComparison.OrdinalIgnoreCase));

            nm = new NetworkManager(null, DisableIP6, NoParams, NoParams);

            // Test included, non IP6.
            nc = nm.CreateIPCollection(settings.Split(","), false);
            Assert.True(string.Equals(nc.ToString(), result2, System.StringComparison.OrdinalIgnoreCase));
            
            // Test excluded, including IPv6.
            nc = nm.CreateIPCollection(settings.Split(","), true);
            Assert.True(string.Equals(nc.ToString(), result4, System.StringComparison.OrdinalIgnoreCase));

            nm = new NetworkManager(null, EnableIP6, NoParams, NoParams);
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
            var nm = new NetworkManager(null, EnableIP6, NoParams, NoParams);

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
            return address.Equals(IPAddress.Broadcast);
        }

        [Theory]

        [InlineData("www.google.co.uk;www.helloworld.com;www.123.com;255.255.255.255")]
        public void TestCallback(string source)
        {

            var nm = new NetworkManager(null, EnableIP6, NoParams, NoParams);

            // Test included, IP6.
            NetCollection ncSource = nm.CreateIPCollection(source.Split(";"));
            ncSource.Items[0].Tag = 1;
            ncSource.Items[1].Tag = 2;
            ncSource.Items[2].Tag = 3;
            NetCollection first = ncSource.Callback(TestAsync, new CancellationToken(), 1);
            Assert.True(first.Count == 1);
        }
    }
}
