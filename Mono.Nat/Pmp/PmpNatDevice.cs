// Authors:
//   Ben Motmans <ben.motmans@gmail.com>
//
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

namespace Mono.Nat.Pmp
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="PmpNatDevice" />.
    /// </summary>
    internal sealed class PmpNatDevice : NatDevice, IEquatable<PmpNatDevice>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PmpNatDevice"/> class.
        /// </summary>
        /// <param name="deviceEndpoint">The deviceEndpoint<see cref="IPEndPoint"/>.</param>
        /// <param name="publicAddress">The publicAddress<see cref="IPAddress"/>.</param>
        internal PmpNatDevice(IPEndPoint deviceEndpoint, IPAddress publicAddress)
            : base(deviceEndpoint, NatProtocol.Pmp)
        {
            PublicAddress = publicAddress;
        }

        /// <summary>
        /// Gets the PublicAddress.
        /// </summary>
        internal IPAddress PublicAddress { get; }

        /// <summary>
        /// The CreatePortMapAsync.
        /// </summary>
        /// <param name="mapping">The mapping<see cref="Mapping"/>.</param>
        /// <returns>The <see cref="Task{Mapping}"/>.</returns>
        public override async Task<Mapping> CreatePortMapAsync(Mapping mapping)
        {
            var message = new CreatePortMappingMessage(mapping);
            var actualMapping = (MappingResponseMessage)await SendMessageAsync(DeviceEndpoint, message).ConfigureAwait(false);
            return actualMapping.Mapping;
        }

        /// <summary>
        /// The DeletePortMapAsync.
        /// </summary>
        /// <param name="mapping">The mapping<see cref="Mapping"/>.</param>
        /// <returns>The <see cref="Task{Mapping}"/>.</returns>
        public override async Task<Mapping> DeletePortMapAsync(Mapping mapping)
        {
            var message = new DeletePortMappingMessage(mapping);
            var actualMapping = (MappingResponseMessage)await SendMessageAsync(DeviceEndpoint, message).ConfigureAwait(false);
            return actualMapping.Mapping;
        }

        /// <summary>
        /// The GetAllMappingsAsync.
        /// </summary>
        /// <returns>The Mapping[].</returns>
        public override Task<Mapping[]> GetAllMappingsAsync()
            => throw new MappingException(ErrorCode.UnsupportedOperation, "The NAT-PMP protocol does not support listing all mappings");

        /// <summary>
        /// The GetExternalIPAsync.
        /// </summary>
        /// <returns>The <see cref="Task{IPAddress}"/>.</returns>
        public override Task<IPAddress> GetExternalIPAsync()
            => Task.FromResult(PublicAddress);

        /// <summary>
        /// The GetSpecificMappingAsync.
        /// </summary>
        /// <param name="protocol">The protocol<see cref="Protocol"/>.</param>
        /// <param name="publicPort">The publicPort<see cref="int"/>.</param>
        /// <returns>The <see cref="Task{Mapping}"/>.</returns>
        public override Task<Mapping> GetSpecificMappingAsync(Protocol protocol, int publicPort)
            => throw new MappingException(ErrorCode.UnsupportedOperation, "The NAT-PMP protocol does not support retrieving a specific mappings");

        /// <summary>
        /// The GetHashCode.
        /// </summary>
        /// <returns>The <see cref="int"/>.</returns>
        public override int GetHashCode()
            => PublicAddress.GetHashCode();

        /// <summary>
        /// The Equals.
        /// </summary>
        /// <param name="other">The other<see cref="PmpNatDevice"/>.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public bool Equals(PmpNatDevice other)
            => other != null && PublicAddress.Equals(other.PublicAddress);

        /// <summary>
        /// The Equals.
        /// </summary>
        /// <param name="obj">The obj<see cref="object"/>.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public override bool Equals(object obj)
            => Equals(obj as PmpNatDevice);

        /// <summary>
        /// The SendMessageAsync.
        /// </summary>
        /// <param name="deviceEndpoint">The deviceEndpoint<see cref="IPEndPoint"/>.</param>
        /// <param name="message">The message<see cref="PortMappingMessage"/>.</param>
        /// <returns>The <see cref="Task{ResponseMessage}"/>.</returns>
        internal static async Task<ResponseMessage> SendMessageAsync(IPEndPoint deviceEndpoint, PortMappingMessage message)
        {
            var udpClient = new UdpClient();
            var tcs = new CancellationTokenSource();
            tcs.Token.Register(() => udpClient.Dispose());

            var data = message.Encode();
            await udpClient.SendAsync(data, data.Length, deviceEndpoint).ConfigureAwait(false);
            var receiveTask = ReceiveMessageAsync(udpClient);

            var delay = PmpConstants.RetryDelay;
            for (int i = 0; i < PmpConstants.RetryAttempts && !receiveTask.IsCompleted; i++)
            {
                await Task.Delay(delay).ConfigureAwait(false);
                delay = TimeSpan.FromTicks(delay.Ticks * 2);
                await udpClient.SendAsync(data, data.Length, deviceEndpoint).ConfigureAwait(false);
            }

            tcs.Dispose();
            return await receiveTask.ConfigureAwait(false);
        }

        /// <summary>
        /// The ReceiveMessageAsync.
        /// </summary>
        /// <param name="udpClient">The udpClient<see cref="UdpClient"/>.</param>
        /// <returns>The <see cref="Task{ResponseMessage}"/>.</returns>
        internal static async Task<ResponseMessage> ReceiveMessageAsync(UdpClient udpClient)
        {
            var receiveResult = await udpClient.ReceiveAsync().ConfigureAwait(false);
            var message = ResponseMessage.Decode(receiveResult.Buffer);

            return message;
        }

        /// <summary>
        /// Overridden.
        /// </summary>
        /// <returns>.</returns>
        public override string ToString()
        {
            return $"PmpNatDevice - Local Address: {DeviceEndpoint}, Public IP: {PublicAddress}, Last Seen: {LastSeen}";
        }
    }
}
