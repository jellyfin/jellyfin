//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2006 Alan McGovern
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
using System.Xml;

namespace Mono.Nat.Upnp
{
    internal class GetGenericPortMappingEntryResponseMessage : MessageBase
    {
        private string remoteHost;
        private int externalPort;
        private Protocol protocol;
        private int internalPort;
        private string internalClient;
        private bool enabled;
        private string portMappingDescription;
        private int leaseDuration;

        public string RemoteHost
        {
            get { return this.remoteHost; }
        }

        public int ExternalPort
        {
            get { return this.externalPort; }
        }

        public Protocol Protocol
        {
            get { return this.protocol; }
        }

        public int InternalPort
        {
            get { return this.internalPort; }
        }

        public string InternalClient
        {
            get { return this.internalClient; }
        }

        public bool Enabled
        {
            get { return this.enabled; }
        }

        public string PortMappingDescription
        {
            get { return this.portMappingDescription; }
        }

        public int LeaseDuration
        {
            get { return this.leaseDuration; }
        }


        public GetGenericPortMappingEntryResponseMessage(XmlNode data, bool genericMapping)
            : base(null)
        {
            remoteHost = (genericMapping) ? data["NewRemoteHost"].InnerText : string.Empty;
            externalPort = (genericMapping) ? Convert.ToInt32(data["NewExternalPort"].InnerText) : -1;
            if (genericMapping)
                protocol = data["NewProtocol"].InnerText.Equals("TCP", StringComparison.InvariantCultureIgnoreCase) ? Protocol.Tcp : Protocol.Udp;
            else
                protocol = Protocol.Udp;

            internalPort = Convert.ToInt32(data["NewInternalPort"].InnerText);
            internalClient = data["NewInternalClient"].InnerText;
            enabled = data["NewEnabled"].InnerText == "1" ? true : false;
            portMappingDescription = data["NewPortMappingDescription"].InnerText;
            leaseDuration = Convert.ToInt32(data["NewLeaseDuration"].InnerText);
        }

        public override System.Net.WebRequest Encode(out byte[] body)
        {
            throw new NotImplementedException();
        }
    }
}
