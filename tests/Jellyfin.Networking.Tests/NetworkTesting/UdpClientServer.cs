
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Jellyfin.Networking.Udp;
using MediaBrowser.Common.Configuration;
using Moq;
using Xunit;

namespace Jellyfin.Networking.Tests
{
    /// <summary>
    /// Test for UDP sub-system.
    /// </summary>
    public static class UdpClientServer
    {
        private const string Message = "This is a messages to send to ourselves.";
        private static bool _received = false;

        [Fact]
        public static async Task SendMessage()
        {
            // select a port to listen to.
            int listeningPort = UdpHelper.GetRandomUnusedUdpPort();

#pragma warning disable CA1508 // Go home compiler - you are drunk. CreateUnicastClient CAN return null!

#pragma warning disable CA2201 // Don't see why i can't use NullReferenceException in tests.

            // Create the server
            using var server = UdpHelper.CreateUnicastClient(IPAddress.Loopback, listeningPort, ProcessMessage, failure: OnFailure);
            if (server == null)
            {
                throw new NullReferenceException("Unable to create server.");
            }

            // select a port to transmit from.
            var udpPortToUse = UdpHelper.GetUdpPortFromRange((UdpHelper.UDPLowerUserPort, UdpHelper.UDPMaxPort));


            // create the client
            using var client = UdpHelper.CreateUnicastClient(IPAddress.Loopback, udpPortToUse, failure: OnFailure);
            if (client == null)
            {
                throw new Exception("Unable to create client.");
            }

#pragma warning restore CA2201
#pragma warning restore CA1508
            // send ourselves a message.
            var destination = new IPEndPoint(IPAddress.Loopback, listeningPort);
            await client.SendUnicast(Message, destination).ConfigureAwait(false);

            // give ourselves time to receive it.
            await Task.Delay(1000);

            Assert.True(_received);
        }

        /// <summary>
        /// Processes any received messages.
        /// </summary>
        /// <param name="client">The <see cref="UdpProcess"/>.</param>
        /// <param name="data">The data received.</param>
        /// <param name="receivedFrom">The remote <see cref="IPEndPoint"/>.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        private static Task ProcessMessage(UdpProcess client, string data, IPEndPoint receivedFrom)
        {
            if (!string.IsNullOrEmpty(data))
            {
                Assert.True(string.Equals(data, Message, StringComparison.Ordinal));
                _received = true;
            }

            return Task.CompletedTask;
        }


        private static void OnFailure(UdpProcess client, Exception? ex = null, string? msg = null)
        {
            // Fail as something went wrong.
            Assert.True(false);
        }
    }
}
