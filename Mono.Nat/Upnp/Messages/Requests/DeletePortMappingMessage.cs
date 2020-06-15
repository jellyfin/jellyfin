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
    using System.Xml;

    /// <summary>
    /// Defines the <see cref="DeletePortMappingMessage" />.
    /// </summary>
    internal sealed class DeletePortMappingMessage : RequestMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeletePortMappingMessage"/> class.
        /// </summary>
        /// <param name="mapping">The mapping<see cref="Mapping"/>.</param>
        /// <param name="device">The device<see cref="UpnpNatDevice"/>.</param>
        public DeletePortMappingMessage(Mapping mapping, UpnpNatDevice device)
            : base(device, "DeletePortMapping")
        {
            Mapping = mapping;
        }

        /// <summary>
        /// Gets the Mapping.
        /// </summary>
        internal Mapping Mapping { get; }

        /// <summary>
        /// The Encode.
        /// </summary>
        /// <param name="writer">The writer<see cref="XmlWriter"/>.</param>
        public override void Encode(XmlWriter writer)
        {
            WriteFullElement(writer, "NewRemoteHost", string.Empty);
            WriteFullElement(writer, "NewExternalPort", Mapping.PublicPort);
            WriteFullElement(writer, "NewProtocol", Mapping.Protocol);
        }
    }
}
