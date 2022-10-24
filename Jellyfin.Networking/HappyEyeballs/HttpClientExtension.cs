using System.IO;
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
        /// <summary>
        /// Gets or sets a value indicating whether the client should use IPv6.
        /// </summary>
        public static bool UseIPv6 { get; set; } = true;

        // Implementation taken from https://github.com/rmkerr/corefx/blob/SocketsHttpHandler_Connect_HappyEyeballs/src/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/ConnectHelper.cs

        /// <summary>
        /// Implements the httpclient callback method.
        /// </summary>
        /// <param name="context">The <see cref="SocketsHttpConnectionContext"/> instance.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> instance.</param>
        /// <returns>The http steam.</returns>
        public static async ValueTask<Stream> OnConnect(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
        {
            if (!UseIPv6)
            {
                return await AttemptConnection(AddressFamily.InterNetwork, context, cancellationToken).ConfigureAwait(false);
            }

            using var cancelIPv6 = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var tryConnectAsyncIPv6 = AttemptConnection(AddressFamily.InterNetworkV6, context, cancelIPv6.Token);

            if (await Task.WhenAny(tryConnectAsyncIPv6, Task.Delay(200, cancelIPv6.Token)).ConfigureAwait(false) == tryConnectAsyncIPv6 && tryConnectAsyncIPv6.IsCompletedSuccessfully)
            {
                cancelIPv6.Cancel();
                return tryConnectAsyncIPv6.GetAwaiter().GetResult();
            }

            using var cancelIPv4 = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var tryConnectAsyncIPv4 = AttemptConnection(AddressFamily.InterNetwork, context, cancelIPv4.Token);

            if (await Task.WhenAny(tryConnectAsyncIPv6, tryConnectAsyncIPv4).ConfigureAwait(false) == tryConnectAsyncIPv6)
            {
                if (tryConnectAsyncIPv6.IsCompletedSuccessfully)
                {
                    cancelIPv4.Cancel();
                    return tryConnectAsyncIPv6.GetAwaiter().GetResult();
                }

                return tryConnectAsyncIPv4.GetAwaiter().GetResult();
            }
            else
            {
                if (tryConnectAsyncIPv4.IsCompletedSuccessfully)
                {
                    cancelIPv6.Cancel();
                    return tryConnectAsyncIPv4.GetAwaiter().GetResult();
                }

                return tryConnectAsyncIPv6.GetAwaiter().GetResult();
            }
        }

        private static async Task<Stream> AttemptConnection(AddressFamily addressFamily, SocketsHttpConnectionContext context, CancellationToken cancellationToken)
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
