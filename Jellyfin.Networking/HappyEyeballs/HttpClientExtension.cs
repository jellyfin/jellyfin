using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Networking.HappyEyeballs
{
    /// <summary>
    /// Defines the <see cref="HttpClientExtension"/> class.
    ///
    /// Implementation taken from https://github.com/ppy/osu-framework/pull/4191 .
    /// </summary>
    public static class HttpClientExtension
    {
        private const int ConnectionEstablishTimeout = 2000;

        /// <summary>
        /// Gets a value indicating whether the initial IPv6 check has been performed (to determine whether v6 is available or not).
        /// </summary>
        private static bool hasResolvedIPv6Availability;

        /// <summary>
        /// Gets or sets a value indicating whether IPv6 should be preferred. Value may change based on runtime failures.
        /// </summary>
        public static bool UseIPv6 { get; set; } = Socket.OSSupportsIPv6;

        /// <summary>
        /// Implements the httpclient callback method.
        /// </summary>
        /// <param name="context">The <see cref="SocketsHttpConnectionContext"/> instance.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> instance.</param>
        /// <returns>The http steam.</returns>
        public static async ValueTask<Stream> OnConnect(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
        {
            // Until .NET supports an implementation of Happy Eyeballs (https://tools.ietf.org/html/rfc8305#section-2),
            // let's make IPv4 fallback work in a simple way. This issue is being tracked at https://github.com/dotnet/runtime/issues/26177
            // and expected to be fixed in .NET 6.
            if (UseIPv6 == true)
            {
                try
                {
                    var localToken = cancellationToken;

                    if (!hasResolvedIPv6Availability)
                    {
                        // to make things move fast, use a very low timeout for the initial ipv6 attempt.
                        var quickFailCts = new CancellationTokenSource(ConnectionEstablishTimeout);
                        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, quickFailCts.Token);

                        localToken = linkedTokenSource.Token;
                    }

                    return await AttemptConnection(AddressFamily.InterNetworkV6, context, localToken).ConfigureAwait(false);
                }
                catch
                {
                    // very naively fallback to ipv4 permanently for this execution based on the response of the first connection attempt.
                    // note that this may cause users to eventually get switched to ipv4 (on a random failure when they are switching networks, for instance)
                    // but in the interest of keeping this implementation simple, this is acceptable.
                    UseIPv6 = false;
                }
                finally
                {
                    hasResolvedIPv6Availability = true;
                }
            }

            // fallback to IPv4.
            return await AttemptConnection(AddressFamily.InterNetwork, context, cancellationToken).ConfigureAwait(false);
        }

        private static async ValueTask<Stream> AttemptConnection(AddressFamily addressFamily, SocketsHttpConnectionContext context, CancellationToken cancellationToken)
        {
            // The following socket constructor will create a dual-mode socket on systems where IPV6 is available.
            var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                // Turn off Nagle's algorithm since it degrades performance in most HttpClient scenarios.
                NoDelay = true
            };

            try
            {
                await socket.ConnectAsync(context.DnsEndPoint, cancellationToken).ConfigureAwait(false);
                // The stream should take the ownership of the underlying socket,
                // closing it when it's disposed.
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    }
}
