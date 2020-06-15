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

namespace Mono.Nat
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="NatDevice" />.
    /// </summary>
    internal abstract class NatDevice : INatDevice
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NatDevice"/> class.
        /// </summary>
        /// <param name="deviceEndpoint">The deviceEndpoint<see cref="IPEndPoint"/>.</param>
        /// <param name="natProtocol">The natProtocol<see cref="NatProtocol"/>.</param>
        protected NatDevice(IPEndPoint deviceEndpoint, NatProtocol natProtocol)
        {
            LastSeen = DateTime.UtcNow;
            DeviceEndpoint = deviceEndpoint;
            NatProtocol = natProtocol;
        }

        /// <summary>
        /// Gets or sets the LastSeen.
        /// </summary>
        public DateTime LastSeen { get; internal set; }

        /// <summary>
        /// Gets the DeviceEndpoint.
        /// </summary>
        public IPEndPoint DeviceEndpoint { get; }

        /// <summary>
        /// Gets the NatProtocol.
        /// </summary>
        public NatProtocol NatProtocol { get; }

        /// <summary>
        /// The CreatePortMapAsync.
        /// </summary>
        /// <param name="mapping">The mapping<see cref="Mapping"/>.</param>
        /// <returns>The <see cref="Task{Mapping}"/>.</returns>
        public abstract Task<Mapping> CreatePortMapAsync(Mapping mapping);

        /// <summary>
        /// The DeletePortMapAsync.
        /// </summary>
        /// <param name="mapping">The mapping<see cref="Mapping"/>.</param>
        /// <returns>The <see cref="Task{Mapping}"/>.</returns>
        public abstract Task<Mapping> DeletePortMapAsync(Mapping mapping);

        /// <summary>
        /// The GetAllMappingsAsync.
        /// </summary>
        /// <returns>The Task.</returns>
        public abstract Task<Mapping[]> GetAllMappingsAsync();

        /// <summary>
        /// The GetExternalIPAsync.
        /// </summary>
        /// <returns>The <see cref="Task{IPAddress}"/>.</returns>
        public abstract Task<IPAddress> GetExternalIPAsync();

        /// <summary>
        /// The GetSpecificMappingAsync.
        /// </summary>
        /// <param name="protocol">The protocol<see cref="Protocol"/>.</param>
        /// <param name="publicPort">The publicPort<see cref="int"/>.</param>
        /// <returns>The <see cref="Task{Mapping}"/>.</returns>
        public abstract Task<Mapping> GetSpecificMappingAsync(Protocol protocol, int publicPort);
    }
}
