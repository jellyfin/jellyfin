//
// Authors:
//   Nicholas Terry <nick.i.terry@gmail.com>
//
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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Mono.Nat.Pmp;

namespace Mono.Nat.Pmp.Mappers
{
    internal class PmpMapper : Pmp, IMapper
    {
        public event EventHandler<DeviceEventArgs> DeviceFound;

        static PmpMapper()
        {
            CreateSocketsAndAddGateways();
        }

        public void Map(IPAddress gatewayAddress)
        {
            sockets.ForEach(x => Map(x, gatewayAddress));
        }

        void Map(UdpClient client, IPAddress gatewayAddress)
        {
            // The nat-pmp search message. Must be sent to GatewayIP:53531
            byte[] buffer = new byte[] { PmpConstants.Version, PmpConstants.OperationCode };

            client.Send(buffer, buffer.Length, new IPEndPoint(gatewayAddress, PmpConstants.ServerPort));
        }

        public void Handle(IPAddress localAddres, byte[] response)
        {
            //if (!IsSearchAddress(endpoint.Address))
            //    return;
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
            OnDeviceFound(new DeviceEventArgs(new PmpNatDevice(localAddres, publicIp)));
        }

        private void OnDeviceFound(DeviceEventArgs args)
        {
            if (DeviceFound != null)
                DeviceFound(this, args);
        }  
    }
}
