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
using System.Xml;

namespace Mono.Nat.Upnp
{
    sealed class CreatePortMappingMessage : RequestMessage
    {
        IPAddress LocalIpAddress { get; }
        Mapping Mapping { get; }

        public CreatePortMappingMessage (Mapping mapping, IPAddress localIpAddress, UpnpNatDevice device)
            : base (device, "AddPortMapping")
        {
            Mapping = mapping;
            LocalIpAddress = localIpAddress;
        }

        public override void Encode (XmlWriter writer)
        {
            WriteFullElement (writer, "NewRemoteHost", "");
            WriteFullElement (writer, "NewExternalPort", Mapping.PublicPort);
            WriteFullElement (writer, "NewProtocol", Mapping.Protocol);
            WriteFullElement (writer, "NewInternalPort", Mapping.PrivatePort);
            WriteFullElement (writer, "NewInternalClient", LocalIpAddress);
            WriteFullElement (writer, "NewEnabled", "1");
            WriteFullElement (writer, "NewPortMappingDescription", string.IsNullOrEmpty (Mapping.Description) ? CreateDefaultDescription (Mapping) : Mapping.Description);
            WriteFullElement (writer, "NewLeaseDuration", Mapping.Lifetime);
        }

        internal static string CreateDefaultDescription (Mapping mapping)
        {
            string executableName = null;
            try {
                executableName = System.Diagnostics.Process.GetCurrentProcess ().ProcessName;
            } catch {

            }

            // If we can get a process name - use it. Note: We shorten it to 12 characters arbitrarily. The
            // reason is that I am concerned that some routers will fail to create port mappings if the
            // description is too long, and about 28 characters total seems like it should be safe. 32 is
            // the next power of two and we're comfortably below that.
            if (string.IsNullOrEmpty (executableName))
                executableName = "Mono.Nat";
            else if (executableName.Length > 12)
                executableName = executableName.Substring (0, 12);

            // longest string is: "aaaaaaaaaaaa TCP 65535 65535"
            return string.Format ($"{executableName} {mapping.Protocol} {mapping.PublicPort} {mapping.PrivatePort}");
        }
    }
}
