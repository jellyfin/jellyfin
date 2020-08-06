//
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
//

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Mono.Nat
{
	abstract class Searcher : ISearcher
	{
		protected static readonly TimeSpan SearchPeriod = TimeSpan.FromMinutes (5);

		public event EventHandler<DeviceEventArgs> DeviceFound;
        public event EventHandler<DeviceEventUnknownArgs> DeviceUnknown;

		public bool Listening => ListeningTask != null;
		public abstract NatProtocol Protocol { get; }

		Dictionary<NatDevice, NatDevice> Devices { get; }
		Task ListeningTask { get; set; }
		protected SocketGroup Clients { get; }

		CancellationTokenSource Cancellation;
		protected CancellationTokenSource CurrentSearchCancellation;
		CancellationTokenSource OverallSearchCancellation;
		Task SearchTask { get; set; }

		protected Searcher (SocketGroup clients)
		{
			Clients = clients;
			Devices = new Dictionary<NatDevice, NatDevice> ();
		}

		protected void BeginListening ()
		{
			// Begin listening, if we are not already listening.
			if (!Listening) {
				Cancellation?.Cancel ();
				Cancellation = new CancellationTokenSource ();
				lock (Devices)
					Devices.Clear();
				ListeningTask = ListenAsync (Cancellation.Token);
			}
		}

		public void Dispose ()
		{
			Clients.Dispose();
		}

		async Task ListenAsync (CancellationToken token)
		{
			while (!token.IsCancellationRequested) {
				(var localAddress, var data) = await Clients.ReceiveAsync (token).ConfigureAwait (false);
				await HandleMessageReceived (localAddress, data, token).ConfigureAwait (false);
			}
		}

		protected abstract Task HandleMessageReceived (IPAddress localAddress, UdpReceiveResult result, CancellationToken token);

		public async Task SearchAsync ()
		{
			// Cancel any existing continuous search operation.
			OverallSearchCancellation?.Cancel ();
			if (SearchTask != null)
				await SearchTask.CatchExceptions ();

			// Create a CancellationTokenSource for the search we're about to perform.
			BeginListening ();
			OverallSearchCancellation = CancellationTokenSource.CreateLinkedTokenSource (Cancellation.Token);

			SearchTask = SearchAsync (null, SearchPeriod, OverallSearchCancellation.Token);
			await SearchTask;
		}

		public async Task SearchAsync (IPAddress gatewayAddress)
		{
			BeginListening ();
			await SearchAsync (gatewayAddress, null, Cancellation.Token).ConfigureAwait (false);
		}

		protected abstract Task SearchAsync (IPAddress gatewayAddress, TimeSpan? repeatInterval, CancellationToken token);

		public void Stop ()
		{
			Cancellation?.Cancel ();
			ListeningTask?.WaitAndForget ();
			SearchTask?.WaitAndForget ();

			lock (Devices)
				Devices.Clear();

			Cancellation = null;
			ListeningTask = null;
			SearchTask = null;
		}

        protected void RaiseDeviceUnknown(IPAddress address, EndPoint remote, string response)
        {
            DeviceUnknown?.Invoke(this, new DeviceEventUnknownArgs(address, remote, response));
        }

		protected void RaiseDeviceFound (NatDevice device)
		{
			CurrentSearchCancellation?.Cancel ();

			NatDevice actualDevice;
			lock (Devices) {
				if (Devices.TryGetValue (device, out actualDevice))
					actualDevice.LastSeen = DateTime.UtcNow;
				else
					Devices[device] = device;
			}
			// If we did not find the device in the dictionary, raise an event as it's the first time
			// we've encountered it!
			if (actualDevice == null)
                DeviceFound?.Invoke(this, new DeviceEventArgs(device));
        }
	}
}
