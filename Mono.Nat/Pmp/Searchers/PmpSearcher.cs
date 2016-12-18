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
using System.Text;
using System.Net;
using Mono.Nat.Pmp;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Linq;
using System.Threading.Tasks;

namespace Mono.Nat
{
    internal class PmpSearcher : ISearcher
    {
		static PmpSearcher instance = new PmpSearcher();
        
		
		public static PmpSearcher Instance
		{
			get { return instance; }
		}

        private int timeout;
        private DateTime nextSearch;
        public event EventHandler<DeviceEventArgs> DeviceFound;
        public event EventHandler<DeviceEventArgs> DeviceLost;

        static PmpSearcher()
        {
            CreateSocketsAndAddGateways();
        }

        public static List<UdpClient> sockets;
        protected static Dictionary<UdpClient, List<IPEndPoint>> gatewayLists;

        internal static void CreateSocketsAndAddGateways()
        {
            sockets = new List<UdpClient>();
            gatewayLists = new Dictionary<UdpClient, List<IPEndPoint>>();

            try
            {
                foreach (NetworkInterface n in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (n.OperationalStatus != OperationalStatus.Up && n.OperationalStatus != OperationalStatus.Unknown)
                        continue;
                    IPInterfaceProperties properties = n.GetIPProperties();
                    List<IPEndPoint> gatewayList = new List<IPEndPoint>();

                    foreach (GatewayIPAddressInformation gateway in properties.GatewayAddresses)
                    {
                        if (gateway.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            gatewayList.Add(new IPEndPoint(gateway.Address, PmpConstants.ServerPort));
                        }
                    }
                    if (gatewayList.Count == 0)
                    {
                        /* Mono on OSX doesn't give any gateway addresses, so check DNS entries */
                        foreach (var gw2 in properties.DnsAddresses)
                        {
                            if (gw2.AddressFamily == AddressFamily.InterNetwork)
                            {
                                gatewayList.Add(new IPEndPoint(gw2, PmpConstants.ServerPort));
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
                                gatewayList.Add(new IPEndPoint(new IPAddress(bytes), PmpConstants.ServerPort));
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

                                gatewayLists.Add(client, gatewayList);
                                sockets.Add(client);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // NAT-PMP does not use multicast, so there isn't really a good fallback.
            }
        }

        PmpSearcher()
        {
            timeout = 250;
        }

        public async void Search()
		{
			foreach (UdpClient s in sockets)
			{
				try
				{
					await Search(s).ConfigureAwait(false);
				}
				catch
				{
					// Ignore any search errors
				}
			}
		}

		async Task Search (UdpClient client)
        {
            // Sort out the time for the next search first. The spec says the 
            // timeout should double after each attempt. Once it reaches 64 seconds
            // (and that attempt fails), assume no devices available
            nextSearch = DateTime.Now.AddMilliseconds(timeout);
            timeout *= 2;

            // We've tried 9 times as per spec, try searching again in 5 minutes
            if (timeout == 128 * 1000)
            {
                timeout = 250;
                nextSearch = DateTime.Now.AddMinutes(10);
                return;
            }

            // The nat-pmp search message. Must be sent to GatewayIP:53531
            byte[] buffer = new byte[] { PmpConstants.Version, PmpConstants.OperationCode };
		    foreach (IPEndPoint gatewayEndpoint in gatewayLists[client])
		    {
		        await client.SendAsync(buffer, buffer.Length, gatewayEndpoint).ConfigureAwait(false);
		    }
        }

        bool IsSearchAddress(IPAddress address)
        {
            foreach (List<IPEndPoint> gatewayList in gatewayLists.Values)
                foreach (IPEndPoint gatewayEndpoint in gatewayList)
                    if (gatewayEndpoint.Address.Equals(address))
                        return true;
            return false;
        }

        public void Handle(IPAddress localAddress, byte[] response, IPEndPoint endpoint)
        {
            if (!IsSearchAddress(endpoint.Address))
                return;
            if (response.Length != 12)
                return;
            if (response[0] != PmpConstants.Version)
                return;
            if (response[1] != PmpConstants.ServerNoop)
                return;
            int errorcode = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(response, 2));
            if (errorcode != 0)
                NatUtility.Log("Non zero error: {0}", errorcode);

            IPAddress publicIp = new IPAddress(new byte[] { response[8], response[9], response[10], response[11] });
            nextSearch = DateTime.Now.AddMinutes(5);
            timeout = 250;
            OnDeviceFound(new DeviceEventArgs(new PmpNatDevice(endpoint.Address, publicIp)));
        }

        public DateTime NextSearch
        {
            get { return nextSearch; }
        }
        private void OnDeviceFound(DeviceEventArgs args)
        {
            if (DeviceFound != null)
                DeviceFound(this, args);
        }

        public NatProtocol Protocol
        {
            get { return NatProtocol.Pmp; }
        }
    }
}
