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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;

namespace Mono.Nat.Upnp.Mappers
{
    internal class UpnpMapper : Upnp, IMapper
    {
        public event EventHandler<DeviceEventArgs> DeviceFound;

        public UdpClient Client { get; set; }

        public UpnpMapper(ILogger logger, IHttpClient httpClient)
            : base(logger,  httpClient)
        {
            //Bind to local port 1900 for ssdp responses
            Client = new UdpClient(1900);
        }

        public void Map(IPAddress gatewayAddress)
        {
            //Get the httpu request payload
            byte[] data = DiscoverDeviceMessage.EncodeUnicast(gatewayAddress);

            Client.Send(data, data.Length, new IPEndPoint(gatewayAddress, 1900));

            new Thread(Receive).Start(); 
        }

        public async void Receive()
        {
            while (true)
            {
                IPEndPoint received = new IPEndPoint(IPAddress.Parse("192.168.0.1"), 5351);
                if (Client.Available > 0)
                {
                    IPAddress localAddress = ((IPEndPoint)Client.Client.LocalEndPoint).Address;
                    byte[] data = Client.Receive(ref received);

                    await Handle(localAddress, data, received);
                }
            }
        }

        public void Handle(IPAddress localAddres, byte[] response)
        {
        }

        public override async Task<UpnpNatDevice> Handle(IPAddress localAddress, byte[] response, IPEndPoint endpoint)
        {
            // No matter what, this method should never throw an exception. If something goes wrong
            // we should still be in a position to handle the next reply correctly.
            try
            {
                var d = await base.Handle(localAddress, response, endpoint).ConfigureAwait(false);               
                var result = await d.GetServicesList().ConfigureAwait(false);

                if (result)
                {
                    DeviceSetupComplete(d);
                }

                return d;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error mapping port. Data string: {0}", ex, Encoding.UTF8.GetString(response));
                return null;
            }
        }

        private void DeviceSetupComplete(INatDevice device)
        {
            OnDeviceFound(new DeviceEventArgs(device));
        }

        private void OnDeviceFound(DeviceEventArgs args)
        {
            if (DeviceFound != null)
                DeviceFound(this, args);
        }
    }
}
