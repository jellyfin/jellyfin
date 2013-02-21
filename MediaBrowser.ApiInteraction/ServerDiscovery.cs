using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.ApiInteraction
{
    public static class ServerDiscovery
    {
        /// <summary>
        /// Attemps to discover the server within a local network
        /// </summary>
        public static async Task<IPEndPoint> DiscoverServer()
        {
            // Create a udp client
            var client = new UdpClient(new IPEndPoint(IPAddress.Any, GetRandomUnusedPort()));

            // Construct the message the server is expecting
            var bytes = Encoding.UTF8.GetBytes("who is MediaBrowserServer?");

            // Send it - must be IPAddress.Broadcast, 7359
            var targetEndPoint = new IPEndPoint(IPAddress.Broadcast, 7359);

            // Send it
            await client.SendAsync(bytes, bytes.Length, targetEndPoint).ConfigureAwait(false);

            // Get a result back
            var result = await client.ReceiveAsync().ConfigureAwait(false);

            if (result.RemoteEndPoint.Port == targetEndPoint.Port)
            {
                // Convert bytes to text
                var text = Encoding.UTF8.GetString(result.Buffer);

                // Expected response : MediaBrowserServer|192.168.1.1:1234
                // If the response is what we're expecting, proceed
                if (text.StartsWith("mediabrowserserver", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Split('|')[1];

                    var vals = text.Split(':');

                    return new IPEndPoint(IPAddress.Parse(vals[0]), int.Parse(vals[1]));
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a random port number that is currently available
        /// </summary>
        private static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
