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

    /// <summary>
    /// Defines the <see cref="Mapping" />.
    /// </summary>
    public sealed class Mapping : IEquatable<Mapping>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Mapping"/> class.
        /// </summary>
        /// <param name="protocol">The protocol used by the port mapping.</param>
        /// <param name="privatePort">The internal/LAN port which will receive traffic sent to the <paramref name="publicPort"/> on the WAN device.</param>
        /// <param name="publicPort">Traffic sent to this external/WAN port is forwarded to the <paramref name="privatePort"/> on the LAN device.</param>
        public Mapping(Protocol protocol, int privatePort, int publicPort)
            : this(protocol, privatePort, publicPort, 0, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mapping"/> class.
        /// </summary>
        /// <param name="protocol">The protocol used by the port mapping.</param>
        /// <param name="privatePort">The internal/LAN port which will receive traffic sent to the <paramref name="publicPort"/> on the WAN device.</param>
        /// <param name="publicPort">Traffic sent to this public/WAN port is forwarded to the <paramref name="privatePort"/> on the LAN device.</param>
        /// <param name="lifetime">The lifetime of the port mapping in seconds. If a lifetime of '0' is specified then the protocol default lifetime is used. uPnP defaults to 'indefinite' whereas NAT-PMP defaults to 7,200 seconds.</param>
        /// <param name="description">The text description for the port mapping.</param>
        public Mapping(Protocol protocol, int privatePort, int publicPort, int lifetime, string description)
        {
            Protocol = protocol;
            PrivatePort = privatePort;
            PublicPort = publicPort;
            Lifetime = lifetime;
            Description = description;

            if (lifetime == int.MaxValue)
            {
                Expiration = DateTime.MaxValue;
            }
            else if (lifetime == 0)
            {
                Expiration = DateTime.Now;
            }
            else
            {
                Expiration = DateTime.Now.AddSeconds(lifetime);
            }
        }

        /// <summary>
        /// Gets the Description
        /// The text description for the port mapping..
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the Expiration
        /// The time the port mapping should expire at..
        /// </summary>
        public DateTime Expiration { get; }

        /// <summary>
        /// Gets the Lifetime
        /// The lifetime of the port mapping in seconds. If a lifetime of '0' is specified then the
        /// protocol default lifetime is used. uPnP defaults to 'indefinite' whereas NAT-PMP defaults
        /// to 7,200 seconds..
        /// </summary>
        public int Lifetime { get; private set; }

        /// <summary>
        /// Gets the Protocol
        /// The protocol used for the port mapping..
        /// </summary>
        public Protocol Protocol { get; }

        /// <summary>
        /// Gets the PrivatePort
        /// The internal/LAN port which will receive traffic sent to the <see cref="PublicPort"/> on the WAN device..
        /// </summary>
        public int PrivatePort { get; }

        /// <summary>
        /// Gets the PublicPort
        /// Traffic sent to this external/WAN port is forwarded to the <see cref="PrivatePort"/> on the LAN device..
        /// </summary>
        public int PublicPort { get; }

        /// <summary>
        /// The IsExpired.
        /// </summary>
        /// <returns>The <see cref="bool"/>.</returns>
        public bool IsExpired()
            => Expiration < DateTime.Now;

        /// <summary>
        /// Mappings are considered equal if they have the same PrivatePort, PublicPort and Protocol.
        /// </summary>
        /// <param name="obj">The other object to compare with.</param>
        /// <returns>.</returns>
        public override bool Equals(object obj)
            => Equals(obj as Mapping);

        /// <summary>
        /// Mappings are considered equal if they have the same PrivatePort, PublicPort and Protocol.
        /// </summary>
        /// <param name="other">The other mapping to compare with.</param>
        /// <returns>.</returns>
        public bool Equals(Mapping other)
        {
            return other != null
                && Protocol == other.Protocol
                && PrivatePort == other.PrivatePort
                && PublicPort == other.PublicPort;
        }

        /// <summary>
        /// The GetHashCode.
        /// </summary>
        /// <returns>The <see cref="int"/>.</returns>
        public override int GetHashCode()
            => Protocol.GetHashCode() ^ PrivatePort.GetHashCode() ^ PublicPort.GetHashCode();

        /// <summary>
        /// The ToString.
        /// </summary>
        /// <returns>The <see cref="string"/>.</returns>
        public override string ToString()
        {
            return $"(Protocol: {Protocol}, Public Port: {PublicPort}, Private Port: {PrivatePort}, Description: {Description}, Expiration: {Expiration}, Lifetime: {Lifetime}";
        }
    }
}
