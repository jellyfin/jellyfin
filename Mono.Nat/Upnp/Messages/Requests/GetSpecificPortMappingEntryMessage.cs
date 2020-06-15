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
    using System.Globalization;
    using System.Xml;

    /// <summary>
    /// Defines the <see cref="GetSpecificPortMappingEntryMessage" />.
    /// </summary>
    internal sealed class GetSpecificPortMappingEntryMessage : RequestMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetSpecificPortMappingEntryMessage"/> class.
        /// </summary>
        /// <param name="protocol">The protocol<see cref="Protocol"/>.</param>
        /// <param name="externalPort">The externalPort<see cref="int"/>.</param>
        /// <param name="device">The device<see cref="UpnpNatDevice"/>.</param>
        public GetSpecificPortMappingEntryMessage(Protocol protocol, int externalPort, UpnpNatDevice device)
            : base(device, "GetSpecificPortMappingEntry")
        {
            Protocol = protocol;
            ExternalPort = externalPort;
        }

        /// <summary>
        /// Gets the ExternalPort.
        /// </summary>
        internal int ExternalPort { get; }

        /// <summary>
        /// Gets the Protocol.
        /// </summary>
        internal Protocol Protocol { get; }

        /// <summary>
        /// The Encode.
        /// </summary>
        /// <param name="writer">The writer<see cref="XmlWriter"/>.</param>
        public override void Encode(XmlWriter writer)
        {
            WriteFullElement(writer, "NewRemoteHost", string.Empty);
            WriteFullElement(writer, "NewExternalPort", ExternalPort.ToString(CultureInfo.InvariantCulture));
            WriteFullElement(writer, "NewProtocol", Protocol);
        }
    }
}
