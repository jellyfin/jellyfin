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

using System.Net;
using System.IO;
using System.Text;
using System.Xml;

namespace Mono.Nat.Upnp
{
	internal class DeletePortMappingMessage : MessageBase
	{
		private Mapping mapping;

		public DeletePortMappingMessage(Mapping mapping, UpnpNatDevice device)
			: base(device)
		{
			this.mapping = mapping;
		}

		public override WebRequest Encode(out byte[] body)
		{
			StringBuilder builder = new StringBuilder(256);
			XmlWriter writer = CreateWriter(builder);

			WriteFullElement(writer, "NewRemoteHost", string.Empty);
			WriteFullElement(writer, "NewExternalPort", mapping.PublicPort.ToString(MessageBase.Culture));
			WriteFullElement(writer, "NewProtocol", mapping.Protocol == Protocol.Tcp ? "TCP" : "UDP");

			writer.Flush();
			return CreateRequest("DeletePortMapping", builder.ToString(), out body);
		}
	}
}
