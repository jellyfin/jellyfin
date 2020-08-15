//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//   Ben Motmans <ben.motmans@gmail.com>
//   Nicholas Terry <nick.i.terry@gmail.com>
//
// Copyright (C) 2006 Alan McGovern
// Copyright (C) 2007 Ben Motmans
// Copyright (C) 2014 Nicholas Terry
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
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Mono.Nat.Logging;

namespace Mono.Nat.Pmp
{
	class PmpSearcher : Searcher
	{
		static Logger Log { get; } = Logger.Create();

		internal static PmpSearcher Create()
		{
			var clients = new Dictionary<UdpClient, List<IPAddress>>();

			try
			{
				foreach (NetworkInterface n in NetworkInterface.GetAllNetworkInterfaces())
				{
					if (n.OperationalStatus != OperationalStatus.Up && n.OperationalStatus != OperationalStatus.Unknown)
						continue;
					IPInterfaceProperties properties = n.GetIPProperties();
					var gatewayList = new List<IPAddress>();

					foreach (GatewayIPAddressInformation gateway in properties.GatewayAddresses)
					{
						if (gateway.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							gatewayList.Add(gateway.Address);
						}
					}
					if (gatewayList.Count == 0)
					{
						/* Mono on OSX doesn't give any gateway addresses, so check DNS entries */
						foreach (var gw2 in properties.DnsAddresses)
						{
							if (gw2.AddressFamily == AddressFamily.InterNetwork)
							{
								gatewayList.Add(gw2);
							}
						}
						foreach (var unicast in properties.UnicastAddresses)
						{
							if (/*unicast.DuplicateAddressDetectionState == DuplicateAddressDetectionState.Preferred
							    && unicast.AddressPreferredLifetime != UInt32.MaxValue
							    && */unicast.Address.AddressFamily == AddressFamily.InterNetwork)
							{
								var bytes = unicast.Address.GetAddressBytes();
								bytes[3] = 1;
								gatewayList.Add(new IPAddress(bytes));
							}
						}
					}

					if (gatewayList.Count > 0)
					{
						foreach (UnicastIPAddressInformation address in properties.UnicastAddresses)
						{
							if (address.Address.AddressFamily == AddressFamily.InterNetwork)
							{
								UdpClient client;

								try
								{
									client = new UdpClient(new IPEndPoint(address.Address, 0));
								}
								catch (SocketException)
								{
									continue; // Move on to the next address.
								}

								clients.Add(client, gatewayList);
							}
						}
					}
				}
			}
			catch (Exception)
			{
				// NAT-PMP does not use multicast, so there isn't really a good fallback.
			}

			return new PmpSearcher(new SocketGroup(clients, PmpConstants.ServerPort));
		}

		public override NatProtocol Protocol => NatProtocol.Pmp;

		PmpSearcher (SocketGroup sockets)
			: base (sockets)
		{

		}

		protected override async Task SearchAsync (IPAddress gatewayAddress, TimeSpan? repeatInterval, CancellationToken token)
		{
            if (token == CancellationToken.None)
            {
                token = Cancellation.Token;
            }

            do
            {
				var currentSearch = CancellationTokenSource.CreateLinkedTokenSource (token);
				Interlocked.Exchange (ref CurrentSearchCancellation, currentSearch)?.Cancel ();

				try {
					await SearchOnce (gatewayAddress, currentSearch.Token);
				} catch (OperationCanceledException) {
					token.ThrowIfCancellationRequested ();
				}
				if (!repeatInterval.HasValue)
					break;
				await Task.Delay (repeatInterval.Value, token);
			} while (true);
		}

		async Task SearchOnce (IPAddress gatewayAddress, CancellationToken token)
		{
			var buffer = new [] { PmpConstants.Version, PmpConstants.OperationCode };
			var delay = PmpConstants.RetryDelay;

			for (int i = 0; i < PmpConstants.RetryAttempts; i ++) {
				await Clients.SendAsync (buffer, gatewayAddress, token);
				await Task.Delay (delay, token);
				delay = TimeSpan.FromTicks (delay.Ticks * 2);
			}
		}

        public override Task HandleMessageReceived(IPAddress localAddress, byte[] response, IPEndPoint endpoint, CancellationToken token)
        { 
			if (response.Length != 12)
				return Task.CompletedTask;
			if (response [0] != PmpConstants.Version)
				return Task.CompletedTask;
			if (response [1] != PmpConstants.ServerNoop)
				return Task.CompletedTask;

			int errorcode = IPAddress.NetworkToHostOrder (BitConverter.ToInt16 (response, 2));
			if (errorcode != 0)
				Log.InfoFormatted("Non zero error: {0}", errorcode);

			var publicIp = new IPAddress (new byte [] { response [8], response [9], response [10], response [11] });

			RaiseDeviceFound (new PmpNatDevice (endpoint, publicIp));
			return Task.CompletedTask;
		}
	}
}
