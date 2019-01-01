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
using Mono.Nat.Upnp;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Dlna;
using System.Threading.Tasks;

namespace Mono.Nat
{
    internal class UpnpSearcher : ISearcher
    {
        public event EventHandler<DeviceEventArgs> DeviceFound;

        private DateTime nextSearch;
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;

        public UpnpSearcher(ILogger logger, IHttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public void Search()
		{
		}

        public async Task Handle(IPAddress localAddress, UpnpDeviceInfo deviceInfo, IPEndPoint endpoint)
        {
            if (localAddress == null)
            {
                throw new ArgumentNullException("localAddress");
            }

            try
            {
                /* For UPnP Port Mapping we need ot find either WANPPPConnection or WANIPConnection. 
				 Any other device type is no good to us for this purpose. See the IGP overview paper 
				 page 5 for an overview of device types and their hierarchy.
				 http://upnp.org/specs/gw/UPnP-gw-InternetGatewayDevice-v1-Device.pdf */

                /* TODO: Currently we are assuming version 1 of the protocol. We should figure out which
				 version it is and apply the correct URN. */

                /* Some routers don't correctly implement the version ID on the URN, so we only search for the type
				 prefix. */

                // We have an internet gateway device now
                UpnpNatDevice d = new UpnpNatDevice(localAddress, deviceInfo, endpoint, string.Empty, _logger, _httpClient);

                await d.GetServicesList().ConfigureAwait(false);

                OnDeviceFound(new DeviceEventArgs(d));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decoding device response");
            }
        }

        public void Handle(IPAddress localAddress, byte[] response, IPEndPoint endpoint)
        {
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
            get { return NatProtocol.Upnp; }
        }
    }
}
