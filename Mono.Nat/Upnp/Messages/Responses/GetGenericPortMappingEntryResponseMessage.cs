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

namespace Mono.Nat.Upnp
{
    using System;
    using System.Globalization;
    using System.Xml;

    /// <summary>
    /// Defines the <see cref="GetGenericPortMappingEntryResponseMessage" />.
    /// </summary>
    internal class GetGenericPortMappingEntryResponseMessage : ResponseMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetGenericPortMappingEntryResponseMessage"/> class.
        /// </summary>
        /// <param name="data">The data<see cref="XmlNode"/>.</param>
        public GetGenericPortMappingEntryResponseMessage(XmlNode data)
        {
            RemoteHost = data["NewRemoteHost"].InnerText;
            ExternalPort = Convert.ToInt32(data["NewExternalPort"].InnerText, CultureInfo.InvariantCulture);
            Protocol = data["NewProtocol"].InnerText == "TCP" ? Protocol.Tcp : Protocol.Udp;

            InternalPort = Convert.ToInt32(data["NewInternalPort"].InnerText, CultureInfo.InvariantCulture);
            InternalClient = data["NewInternalClient"].InnerText;
            Enabled = data["NewEnabled"].InnerText == "1";
            PortMappingDescription = data["NewPortMappingDescription"].InnerText;
            LeaseDuration = Convert.ToInt32(data["NewLeaseDuration"].InnerText, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets a value indicating whether Enabled.
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// Gets the ExternalPort.
        /// </summary>
        public int ExternalPort { get; }

        /// <summary>
        /// Gets the InternalClient.
        /// </summary>
        public string InternalClient { get; }

        /// <summary>
        /// Gets the InternalPort.
        /// </summary>
        public int InternalPort { get; }

        /// <summary>
        /// Gets the LeaseDuration.
        /// </summary>
        public int LeaseDuration { get; }

        /// <summary>
        /// Gets the PortMappingDescription.
        /// </summary>
        public string PortMappingDescription { get; }

        /// <summary>
        /// Gets the Protocol.
        /// </summary>
        public Protocol Protocol { get; }

        /// <summary>
        /// Gets the RemoteHost.
        /// </summary>
        public string RemoteHost { get; }
    }
}
