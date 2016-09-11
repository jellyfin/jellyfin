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

namespace Mono.Nat
{
    internal class PmpSearcher : Pmp.Pmp, ISearcher
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

        PmpSearcher()
        {
            timeout = 250;
        }

        public void Search()
		{
			foreach (UdpClient s in sockets)
			{
				try
				{
					Search(s);
				}
				catch
				{
					// Ignore any search errors
				}
			}
		}

		void Search (UdpClient client)
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
                client.Send(buffer, buffer.Length, gatewayEndpoint);
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
