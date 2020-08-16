//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//   Ben Motmans <ben.motmans@gmail.com>
//
// Copyright (C) 2006 Alan McGovern
// Copyright (C) 2007 Ben Motmans
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
using System.Net;
using System.Threading.Tasks;

namespace Mono.Nat
{
    public interface INatDevice
    {
        /// <summary>
        /// The endpoint to send messages to the WAN device
        /// </summary>
        IPEndPoint DeviceEndpoint { get; }

        /// <summary>
        /// The UTC time the last message was received from the WAN device.
        /// </summary>
        DateTime LastSeen { get; }

        /// <summary>
        /// The NAT protocol supported by the WAN device (e.g. NAT-PMP or uPnP)
        /// </summary>
        NatProtocol NatProtocol { get; }

        /// <summary>
        /// Creates a port map using the specified Mapping. If that exact mapping cannot be
        /// created, a best-effort mapping may be created which uses a different port. The
        /// return value is actual created mapping.
        /// </summary>
        /// <param name="mapping"></param>
        /// <returns></returns>
        Task<Mapping> CreatePortMapAsync (Mapping mapping);

        /// <summary>
        /// Deletes a port mapping from the WAN device.
        /// </summary>
        /// <param name="mapping"></param>
        /// <returns></returns>
        Task<Mapping> DeletePortMapAsync (Mapping mapping);

        /// <summary>
        /// Retrieves a list of all mappings on the WAN device.
        /// </summary>
        /// <returns></returns>
        Task<Mapping[]> GetAllMappingsAsync ();

        /// <summary>
        /// Gets the external IP address associated with the WAN device.
        /// </summary>
        /// <returns></returns>
        Task<IPAddress> GetExternalIPAsync ();

        /// <summary>
        /// Retrieves the mapping associated with this combination of public port and protocol. Throws a MappingException
        /// if there is no mapping matching the criteria.
        /// </summary>
        /// <param name="protocol">The protocol of the mapping</param>
        /// <param name="publicPort">The external/WAN port of the mapping</param>
        /// <returns></returns>
        Task<Mapping> GetSpecificMappingAsync (Protocol protocol, int publicPort);
    }
}
