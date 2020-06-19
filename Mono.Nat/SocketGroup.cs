namespace Mono.Nat
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="SocketGroup" />.
    /// </summary>
    internal class SocketGroup : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SocketGroup"/> class.
        /// </summary>
        /// <param name="sockets">The sockets.</param>
        /// <param name="defaultPort">The defaultPort<see cref="int"/>.</param>
        public SocketGroup(Dictionary<UdpClient, List<IPAddress>> sockets, int defaultPort)
        {
            Sockets = sockets;
            DefaultPort = defaultPort;
            SocketSendLocker = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Gets the Sockets.
        /// </summary>
        internal Dictionary<UdpClient, List<IPAddress>> Sockets { get; }

        /// <summary>
        /// Gets the SocketSendLocker.
        /// </summary>
        internal SemaphoreSlim SocketSendLocker { get; }

        /// <summary>
        /// Gets the DefaultPort.
        /// </summary>
        internal int DefaultPort { get; }

        /// <summary>
        /// The ReceiveAsync.
        /// </summary>
        /// <param name="token">The token<see cref="CancellationToken"/>.</param>
        /// <returns>The Task>.</returns>
        public async Task<(IPAddress, UdpReceiveResult)> ReceiveAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var keypair in Sockets)
                {
                    try
                    {
                        if (keypair.Key.Available > 0)
                        {
                            var localAddress = ((IPEndPoint)keypair.Key.Client.LocalEndPoint).Address;
                            var data = await keypair.Key.ReceiveAsync().ConfigureAwait(false);
                            return (localAddress, data);
                        }
                    }
                    catch
                    {
                        // Ignore any errors
                    }
                }

                await Task.Delay(10, token).ConfigureAwait(false);
            }

            throw new TaskCanceledException("This should never be reached.");
        }

        /// <summary>
        /// The SendAsync.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="gatewayAddress">The gatewayAddress<see cref="IPAddress"/>.</param>
        /// <param name="token">The token<see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task SendAsync(byte[] buffer, IPAddress? gatewayAddress, CancellationToken token)
        {
            using (await SocketSendLocker.DisposableWaitAsync(token).ConfigureAwait(false))
            {
                foreach (var keypair in Sockets)
                {
                    try
                    {
                        if (gatewayAddress == null)
                        {
                            foreach (var defaultGateway in keypair.Value)
                            {
                                await keypair.Key.SendAsync(buffer, buffer.Length, new IPEndPoint(defaultGateway, DefaultPort)).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await keypair.Key.SendAsync(buffer, buffer.Length, new IPEndPoint(gatewayAddress, DefaultPort)).ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        // Ignore errors.
                    }
                }
            }
        }

        /// <summary>
        /// Rests this instance to empty.
        /// </summary>
        public void Reset()
        {
            foreach (var keypair in Sockets)
            {
                keypair.Key.Dispose();
            }

            Sockets.Clear();
        }

        public void Dispose()
        {
            Reset();
        }
    }
}
