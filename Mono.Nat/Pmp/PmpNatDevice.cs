//
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
//

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mono.Nat.Pmp
{
    internal sealed class PmpNatDevice : AbstractNatDevice, IEquatable<PmpNatDevice>
    {
        private IPAddress localAddress;
        private IPAddress publicAddress;

        internal PmpNatDevice(IPAddress localAddress, IPAddress publicAddress)
        {
            this.localAddress = localAddress;
            this.publicAddress = publicAddress;
        }

        public override IPAddress LocalAddress
        {
            get { return localAddress; }
        }

        public override Task CreatePortMap(Mapping mapping)
        {
            return InternalCreatePortMapAsync(mapping, true);
        }

        public override bool Equals(object obj)
        {
            PmpNatDevice device = obj as PmpNatDevice;
            return (device == null) ? false : this.Equals(device);
        }

        public override int GetHashCode()
        {
            return this.publicAddress.GetHashCode();
        }

        public bool Equals(PmpNatDevice other)
        {
            return (other == null) ? false : this.publicAddress.Equals(other.publicAddress);
        }

        private async Task<Mapping> InternalCreatePortMapAsync(Mapping mapping, bool create)
        {
            var package = new List<byte>();

            package.Add(PmpConstants.Version);
            package.Add(mapping.Protocol == Protocol.Tcp ? PmpConstants.OperationCodeTcp : PmpConstants.OperationCodeUdp);
            package.Add(0); //reserved
            package.Add(0); //reserved
            package.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)mapping.PrivatePort)));
            package.AddRange(
                BitConverter.GetBytes(create ? IPAddress.HostToNetworkOrder((short)mapping.PublicPort) : (short)0));
            package.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(mapping.Lifetime)));

            try
            {
                byte[] buffer = package.ToArray();
                int attempt = 0;
                int delay = PmpConstants.RetryDelay;

                using (var udpClient = new UdpClient())
                {
                    var cancellationTokenSource = new CancellationTokenSource();

                    while (attempt < PmpConstants.RetryAttempts)
                    {
                        await udpClient.SendAsync(buffer, buffer.Length,
                                new IPEndPoint(LocalAddress, PmpConstants.ServerPort));

                        if (attempt == 0)
                        {
                            Task.Run(() => CreatePortMapListen(udpClient, mapping, cancellationTokenSource.Token));
                        }

                        attempt++;
                        delay *= 2;
                        await Task.Delay(delay).ConfigureAwait(false);
                    }

                    cancellationTokenSource.Cancel();
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception e)
            {
                string type = create ? "create" : "delete";
                string message = String.Format("Failed to {0} portmap (protocol={1}, private port={2}) {3}",
                                               type,
                                               mapping.Protocol,
                                               mapping.PrivatePort,
                                               e.Message);
                NatUtility.Log(message);
                var pmpException = e as MappingException;
                throw new MappingException(message, pmpException);
            }

            return mapping;
        }

        private async void CreatePortMapListen(UdpClient udpClient, Mapping mapping, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await udpClient.ReceiveAsync().ConfigureAwait(false);
                    var endPoint = result.RemoteEndPoint;
                    byte[] data = data = result.Buffer;

                    if (data.Length < 16)
                        continue;

                    if (data[0] != PmpConstants.Version)
                        continue;

                    var opCode = (byte)(data[1] & 127);

                    var protocol = Protocol.Tcp;
                    if (opCode == PmpConstants.OperationCodeUdp)
                        protocol = Protocol.Udp;

                    short resultCode = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 2));
                    int epoch = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, 4));

                    short privatePort = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 8));
                    short publicPort = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 10));

                    var lifetime = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, 12));

                    if (privatePort < 0 || publicPort < 0 || resultCode != PmpConstants.ResultCodeSuccess)
                    {
                        var errors = new[]
                                         {
                                         "Success",
                                         "Unsupported Version",
                                         "Not Authorized/Refused (e.g. box supports mapping, but user has turned feature off)"
                                         ,
                                         "Network Failure (e.g. NAT box itself has not obtained a DHCP lease)",
                                         "Out of resources (NAT box cannot create any more mappings at this time)",
                                         "Unsupported opcode"
                                     };

                        var errorMsg = errors[resultCode];
                        NatUtility.Log("Error in CreatePortMapListen: " + errorMsg);
                        return;
                    }

                    if (lifetime == 0) return; //mapping was deleted

                    //mapping was created
                    //TODO: verify that the private port+protocol are a match
                    mapping.PublicPort = publicPort;
                    mapping.Protocol = protocol;
                    mapping.Expiration = DateTime.Now.AddSeconds(lifetime);
                    return;
                }
                catch (Exception ex)
                {
                    NatUtility.Logger.ErrorException("Error in CreatePortMapListen", ex);
                    return;
                }
            }
        }

        /// <summary>
        /// Overridden.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("PmpNatDevice - Local Address: {0}, Public IP: {1}, Last Seen: {2}",
                this.localAddress, this.publicAddress, this.LastSeen);
        }
    }
}