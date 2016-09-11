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
using System.Linq;
using System.Net;
using System.Text;

namespace Mono.Nat.Upnp
{
    internal class Upnp
    {
        public UpnpNatDevice Handle(IPAddress localAddress, byte[] response, IPEndPoint endpoint)
        {
            // Convert it to a string for easy parsing
            string dataString = null;


            string urn;
            dataString = Encoding.UTF8.GetString(response);

            if (NatUtility.Verbose)
                NatUtility.Log("UPnP Response: {0}", dataString);

            /* For UPnP Port Mapping we need ot find either WANPPPConnection or WANIPConnection. 
                Any other device type is no good to us for this purpose. See the IGP overview paper 
                page 5 for an overview of device types and their hierarchy.
                http://upnp.org/specs/gw/UPnP-gw-InternetGatewayDevice-v1-Device.pdf */

            /* TODO: Currently we are assuming version 1 of the protocol. We should figure out which
                version it is and apply the correct URN. */

            /* Some routers don't correctly implement the version ID on the URN, so we only search for the type
                prefix. */

            string log = "UPnP Response: Router advertised a '{0}' service";
            StringComparison c = StringComparison.OrdinalIgnoreCase;
            if (dataString.IndexOf("urn:schemas-upnp-org:service:WANIPConnection:", c) != -1)
            {
                urn = "urn:schemas-upnp-org:service:WANIPConnection:1";
                NatUtility.Log(log, "urn:schemas-upnp-org:service:WANIPConnection:1");
            }
            else if (dataString.IndexOf("urn:schemas-upnp-org:service:WANPPPConnection:", c) != -1)
            {
                urn = "urn:schemas-upnp-org:service:WANPPPConnection:1";
                NatUtility.Log(log, "urn:schemas-upnp-org:service:WANPPPConnection:");
            }
            else
                throw new NotSupportedException("Received non-supported device type");

            // We have an internet gateway device now
            return new UpnpNatDevice(localAddress, dataString, urn);
        }
    }
}
