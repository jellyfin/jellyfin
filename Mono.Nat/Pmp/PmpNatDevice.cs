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

namespace Mono.Nat.Pmp
{
	internal sealed class PmpNatDevice : AbstractNatDevice, IEquatable<PmpNatDevice> 
	{
        private AsyncResult externalIpResult;
        private bool pendingOp;
		private IPAddress localAddress;
		private IPAddress publicAddress;
		
		internal PmpNatDevice (IPAddress localAddress, IPAddress publicAddress)
		{
			this.localAddress = localAddress;
			this.publicAddress = publicAddress;
		}
		
		public override IPAddress LocalAddress
		{
			get { return localAddress; }
		}

		public override IPAddress GetExternalIP ()
		{
			return publicAddress;
		}

        public override IAsyncResult BeginCreatePortMap(Mapping mapping, AsyncCallback callback, object asyncState)
		{
			PortMapAsyncResult pmar = new PortMapAsyncResult (mapping.Protocol, mapping.PublicPort, PmpConstants.DefaultLeaseTime, callback, asyncState);
			ThreadPool.QueueUserWorkItem (delegate 
            {
				try 
                {
					CreatePortMap(pmar.Mapping, true);
					pmar.Complete();
				} 
                catch (Exception e) 
                {
					pmar.Complete(e);
				}
			});
			return pmar;
		}

		public override IAsyncResult BeginDeletePortMap (Mapping mapping, AsyncCallback callback, object asyncState)
		{
			PortMapAsyncResult pmar =  new PortMapAsyncResult (mapping, callback, asyncState);
			ThreadPool.QueueUserWorkItem (delegate {
				try {
					CreatePortMap(pmar.Mapping, false);
					pmar.Complete();
				} catch (Exception e) {
					pmar.Complete(e);
				}
			});
			return pmar;
		}

		public override void EndCreatePortMap (IAsyncResult result)
		{
			PortMapAsyncResult pmar = result as PortMapAsyncResult;
			pmar.AsyncWaitHandle.WaitOne ();
		}

		public override void EndDeletePortMap (IAsyncResult result)
		{
			PortMapAsyncResult pmar = result as PortMapAsyncResult;
			pmar.AsyncWaitHandle.WaitOne ();
		}
		
		public override IAsyncResult BeginGetAllMappings (AsyncCallback callback, object asyncState)
		{
			//NAT-PMP does not specify a way to get all port mappings
			throw new NotSupportedException ();
		}

		public override IAsyncResult BeginGetExternalIP (AsyncCallback callback, object asyncState)
		{
            StartOp(ref externalIpResult, callback, asyncState);
            AsyncResult result = externalIpResult;
            result.Complete();
            return result;
		}

		public override IAsyncResult BeginGetSpecificMapping (Protocol protocol, int port, AsyncCallback callback, object asyncState)
		{
			//NAT-PMP does not specify a way to get a specific port map
			throw new NotSupportedException ();
		}
		
		public override Mapping[] EndGetAllMappings (IAsyncResult result)
		{
			//NAT-PMP does not specify a way to get all port mappings
			throw new NotSupportedException ();
		}

		public override IPAddress EndGetExternalIP (IAsyncResult result)
		{
            EndOp(result, ref externalIpResult);
			return publicAddress;
		}

        private void StartOp(ref AsyncResult result, AsyncCallback callback, object asyncState)
        {
            if (pendingOp == true)
                throw new InvalidOperationException("Can only have one simultaenous async operation");

            pendingOp = true;
            result = new AsyncResult(callback, asyncState);
        }

        private void EndOp(IAsyncResult supplied, ref AsyncResult actual)
        {
            if (supplied == null)
                throw new ArgumentNullException("result");

            if (supplied != actual)
                throw new ArgumentException("Supplied IAsyncResult does not match the stored result");

            if (!supplied.IsCompleted)
                supplied.AsyncWaitHandle.WaitOne();

            if (actual.StoredException != null)
                throw actual.StoredException;

            pendingOp = false;
            actual = null;
        }

		public override Mapping EndGetSpecificMapping (IAsyncResult result)
		{
			//NAT-PMP does not specify a way to get a specific port map
			throw new NotSupportedException ();
		}
		
		public override bool Equals(object obj)
		{
			PmpNatDevice device = obj as PmpNatDevice;
			return (device == null) ? false : this.Equals(device);
		}
		
		public override int GetHashCode ()
		{
			return this.publicAddress.GetHashCode();
		}

		public bool Equals (PmpNatDevice other)
		{
			return (other == null) ? false : this.publicAddress.Equals(other.publicAddress);
		}

		private Mapping CreatePortMap (Mapping mapping, bool create)
		{
			List<byte> package = new List<byte> ();
			
			package.Add (PmpConstants.Version);
			package.Add (mapping.Protocol == Protocol.Tcp ? PmpConstants.OperationCodeTcp : PmpConstants.OperationCodeUdp);
			package.Add ((byte)0); //reserved
			package.Add ((byte)0); //reserved
			package.AddRange (BitConverter.GetBytes (IPAddress.HostToNetworkOrder((short)mapping.PrivatePort)));
			package.AddRange (BitConverter.GetBytes (create ? IPAddress.HostToNetworkOrder((short)mapping.PublicPort) : (short)0));
			package.AddRange (BitConverter.GetBytes (IPAddress.HostToNetworkOrder(mapping.Lifetime)));

			CreatePortMapAsyncState state = new CreatePortMapAsyncState ();
			state.Buffer = package.ToArray ();
			state.Mapping = mapping;

			ThreadPool.QueueUserWorkItem (new WaitCallback (CreatePortMapAsync), state);
			WaitHandle.WaitAll (new WaitHandle[] {state.ResetEvent});
			
			if (!state.Success) {
				string type = create ? "create" : "delete";
				throw new MappingException (String.Format ("Failed to {0} portmap (protocol={1}, private port={2}", type, mapping.Protocol, mapping.PrivatePort));
			}
			
			return state.Mapping;
		}
		
		private void CreatePortMapAsync (object obj)
		{
			CreatePortMapAsyncState state = obj as CreatePortMapAsyncState;
			
			UdpClient udpClient = new UdpClient ();
			CreatePortMapListenState listenState = new CreatePortMapListenState (state, udpClient);

			int attempt = 0;
			int delay = PmpConstants.RetryDelay;
			
			ThreadPool.QueueUserWorkItem (new WaitCallback (CreatePortMapListen), listenState);

			while (attempt < PmpConstants.RetryAttempts && !listenState.Success) {
				udpClient.Send (state.Buffer, state.Buffer.Length, new IPEndPoint (localAddress, PmpConstants.ServerPort));
                listenState.UdpClientReady.Set();

				attempt++;
				delay *= 2;
				Thread.Sleep (delay);
			}
			
			state.Success = listenState.Success;
			
			udpClient.Close ();
			state.ResetEvent.Set ();
		}
		
		private void CreatePortMapListen (object obj)
		{
			CreatePortMapListenState state = obj as CreatePortMapListenState;

            UdpClient udpClient = state.UdpClient;
            state.UdpClientReady.WaitOne(); // Evidently UdpClient has some lazy-init Send/Receive race?
			IPEndPoint endPoint = new IPEndPoint (localAddress, PmpConstants.ServerPort);
			
			while (!state.Success) 
            {
                byte[] data;
                try
                {
                    data = udpClient.Receive(ref endPoint);
                }
                catch (SocketException)
                {
                    state.Success = false;
                    return;
                }

                catch (ObjectDisposedException)
                {
                    state.Success = false;
                    return;
                }
			
				if (data.Length < 16)
					continue;

				if (data[0] != PmpConstants.Version)
					continue;
			
				byte opCode = (byte)(data[1] & (byte)127);
				
				Protocol protocol = Protocol.Tcp;
				if (opCode == PmpConstants.OperationCodeUdp)
					protocol = Protocol.Udp;

				short resultCode = IPAddress.NetworkToHostOrder (BitConverter.ToInt16 (data, 2));
				uint epoch = (uint)IPAddress.NetworkToHostOrder (BitConverter.ToInt32 (data, 4));

				int privatePort = IPAddress.NetworkToHostOrder (BitConverter.ToInt16 (data, 8));
				int publicPort = IPAddress.NetworkToHostOrder (BitConverter.ToInt16 (data, 10));

				uint lifetime = (uint)IPAddress.NetworkToHostOrder (BitConverter.ToInt32 (data, 12));

				if (publicPort < 0 || privatePort < 0 || resultCode != PmpConstants.ResultCodeSuccess)
                {
					state.Success = false;
					return;
				}
				
				if (lifetime == 0) 
                {
					//mapping was deleted
					state.Success = true;
					state.Mapping = null;
					return;
				} 
                else 
                {
					//mapping was created
					//TODO: verify that the private port+protocol are a match
					Mapping mapping = state.Mapping;
					mapping.PublicPort = publicPort;
                    mapping.Protocol = protocol;
					mapping.Expiration = DateTime.Now.AddSeconds (lifetime);

					state.Success = true;
				}
			}
		}


        /// <summary>
        /// Overridden.
        /// </summary>
        /// <returns></returns>
        public override string ToString( )
        {
            return String.Format( "PmpNatDevice - Local Address: {0}, Public IP: {1}, Last Seen: {2}",
                this.localAddress, this.publicAddress, this.LastSeen );
        }


		private class CreatePortMapAsyncState
		{
			internal byte[] Buffer;
			internal ManualResetEvent ResetEvent = new ManualResetEvent (false);
			internal Mapping Mapping;
			
			internal bool Success;
		}
		
		private class CreatePortMapListenState
		{
			internal volatile bool Success;
			internal Mapping Mapping;
            internal UdpClient UdpClient;
            internal ManualResetEvent UdpClientReady;
			
			internal CreatePortMapListenState (CreatePortMapAsyncState state, UdpClient client)
			{
                Mapping = state.Mapping;
                UdpClient = client; UdpClientReady = new ManualResetEvent(false);
			}
		}
	}
}