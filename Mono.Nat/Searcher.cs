// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2019 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace Mono.Nat
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Defines the <see cref="Searcher" />.
    /// </summary>
    internal abstract class Searcher : ISearcher, IDisposable
    {
        /// <summary>
        /// Defines the SearchPeriod.
        /// </summary>
        protected static readonly TimeSpan SearchPeriod = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets defines the currentSearchCancellation.
        /// </summary>
#pragma warning disable SA1401 // Fields should be private
        protected CancellationTokenSource _currentSearchCancellation = null!;
#pragma warning restore SA1401 // Fields should be private

        /// <summary>
        /// Defines the cancellation.
        /// </summary>
        private CancellationTokenSource _cancellation = null!;

        /// <summary>
        /// Defines the overallSearchCancellation.
        /// </summary>
        private CancellationTokenSource _overallSearchCancellation = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="Searcher"/> class.
        /// </summary>
        /// <param name="logger">ILogger object.</param>
        public Searcher(ILogger<ISearcher> logger)
        {
            Logger = logger;
            Clients = null!;
            Devices = new Dictionary<NatDevice, NatDevice>();
        }

        /// <summary>
        /// Defines the DeviceFound.
        /// </summary>
        public event EventHandler<DeviceEventArgs>? DeviceFound;

        /// <summary>
        /// Defines the DeviceLost.
        /// </summary>
        public event EventHandler<DeviceEventArgs>? DeviceLost;

        /// <summary>
        /// Gets the Logging object.
        /// </summary>
        public ILogger<ISearcher> Logger { get; }

        /// <summary>
        /// Gets a value indicating whether Listening.
        /// </summary>
        public bool Listening => ListeningTask != null;

        /// <summary>
        /// Gets the Protocol.
        /// </summary>
        public abstract NatProtocol Protocol { get; }

        /// <summary>
        /// Gets or sets the SearchTask.
        /// </summary>
        internal Task? SearchTask { get; set; }

        /// <summary>
        /// Gets or sets the Clients.
        /// </summary>
        internal SocketGroup Clients { get; set; }

        /// <summary>
        /// Gets the Devices.
        /// </summary>
        private Dictionary<NatDevice, NatDevice> Devices { get; }

        /// <summary>
        /// Gets or sets the ListeningTask.
        /// </summary>
        private Task? ListeningTask { get; set; }

        /// <summary>
        /// The SearchAsync.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task SearchAsync()
        {
            Begin();
            // Cancel any existing continuous search operation.
            _overallSearchCancellation?.Cancel();
            if (SearchTask != null)
            {
                await SearchTask.CatchExceptions(Logger).ConfigureAwait(false);
            }

            // Create a CancellationTokenSource for the search we're about to perform.
            BeginListening();
            _overallSearchCancellation = CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token);

            SearchTask = SearchAsync(null, SearchPeriod, _overallSearchCancellation.Token);
            await SearchTask.ConfigureAwait(false);
        }

        /// <summary>
        /// The SearchAsync.
        /// </summary>
        /// <param name="gatewayAddress">The gatewayAddress.</param>
        /// <returns>A Task.</returns>
        public async Task SearchAsync(IPAddress? gatewayAddress)
        {
            Begin();
            BeginListening();
            await SearchAsync(gatewayAddress, null, _cancellation.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// The Stop.
        /// </summary>
        public virtual void Stop()
        {
            _cancellation?.Cancel();
            ListeningTask?.WaitAndForget(Logger);
            SearchTask?.WaitAndForget(Logger);

            foreach (KeyValuePair<NatDevice, NatDevice> entry in Devices)
            {
                RaiseDeviceLost(entry.Key);
            }
        }

        /// <summary>
        /// The Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The ListenAsync.
        /// </summary>
        /// <param name="token">The token<see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        internal async Task ListenAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                (var localAddress, var data) = await Clients.ReceiveAsync(token).ConfigureAwait(false);
                if (!token.IsCancellationRequested)
                {
                    await HandleMessageReceived(localAddress, data, token).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// The SearchAsync.
        /// </summary>
        /// <param name="gatewayAddress">The gatewayAddress.</param>
        /// <param name="repeatInterval">The repeatInterval.</param>
        /// <param name="token">The token<see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        protected abstract Task SearchAsync(IPAddress? gatewayAddress, TimeSpan? repeatInterval, CancellationToken token);

        protected abstract void Begin();

        /// <summary>
        /// The RaiseDeviceFound.
        /// </summary>
        /// <param name="device">The device<see cref="NatDevice"/>.</param>
        protected void RaiseDeviceFound(NatDevice device)
        {
            _currentSearchCancellation?.Cancel();

            NatDevice actualDevice;
            lock (Devices)
            {
                if (Devices.TryGetValue(device, out actualDevice))
                {
                    actualDevice.LastSeen = DateTime.UtcNow;
                }
                else
                {
                    Devices[device] = device;
                }
            }

            // If we did not find the device in the dictionary, raise an event as it's the first time
            // we've encountered it!

            if (actualDevice == null)
            {
                DeviceFound?.Invoke(this, new DeviceEventArgs(device));
            }
        }

        /// <summary>
        /// The Dispose.
        /// </summary>
        /// <param name="disposing">The disposing<see cref="bool"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                Stop();
            }

            _currentSearchCancellation?.Dispose();
            _cancellation?.Dispose();
            _overallSearchCancellation?.Dispose();

            Clients.Dispose();
        }

        /// <summary>
        /// The RaiseDeviceLost.
        /// </summary>
        /// <param name="device">The device<see cref="NatDevice"/>.</param>
        protected void RaiseDeviceLost(NatDevice device)
        {
            NatDevice actualDevice;
            lock (Devices)
            {
                // If the device is not in the dictionary, bail out.
                if (!Devices.TryGetValue(device, out actualDevice))
                {
                    return;
                }

                Devices.Remove(actualDevice);
            }

            DeviceLost?.Invoke(this, new DeviceEventArgs(actualDevice));
        }

        /// <summary>
        /// The BeginListening.
        /// </summary>
        protected void BeginListening()
        {
            // Begin listening, if we are not already listening.
            if (!Listening)
            {
                _cancellation?.Cancel();
                _cancellation = new CancellationTokenSource();
                ListeningTask = ListenAsync(_cancellation.Token);
            }
        }

        /// <summary>
        /// The HandleMessageReceived.
        /// </summary>
        /// <param name="localAddress">The localAddress<see cref="IPAddress"/>.</param>
        /// <param name="result">The result<see cref="UdpReceiveResult"/>.</param>
        /// <param name="token">The token<see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        protected abstract Task HandleMessageReceived(IPAddress localAddress, UdpReceiveResult result, CancellationToken token);
    }
}
