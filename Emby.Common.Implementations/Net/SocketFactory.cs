using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Emby.Common.Implementations.Networking;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;

namespace Emby.Common.Implementations.Net
{
    public class SocketFactory : ISocketFactory
    {
        // THIS IS A LINKED FILE - SHARED AMONGST MULTIPLE PLATFORMS	
        // Be careful to check any changes compile and work for all platform projects it is shared in.

        // Not entirely happy with this. Would have liked to have done something more generic/reusable,
        // but that wasn't really the point so kept to YAGNI principal for now, even if the 
        // interfaces are a bit ugly, specific and make assumptions.

        private readonly ILogger _logger;

        public SocketFactory(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _logger = logger;
        }

        public IAcceptSocket CreateSocket(IpAddressFamily family, MediaBrowser.Model.Net.SocketType socketType, MediaBrowser.Model.Net.ProtocolType protocolType, bool dualMode)
        {
            try
            {
                var addressFamily = family == IpAddressFamily.InterNetwork
                    ? AddressFamily.InterNetwork
                    : AddressFamily.InterNetworkV6;

                var socket = new Socket(addressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);

                if (dualMode)
                {
                    socket.DualMode = true;
                }

                return new NetAcceptSocket(socket, _logger, dualMode);
            }
            catch (SocketException ex)
            {
                throw new SocketCreateException(ex.SocketErrorCode.ToString(), ex);
            }
            catch (ArgumentException ex)
            {
                if (dualMode)
                {
                    // Mono for BSD incorrectly throws ArgumentException instead of SocketException
                    throw new SocketCreateException("AddressFamilyNotSupported", ex);
                }
                else
                {
                    throw;
                }
            }
        }

        public ISocket CreateTcpSocket(IpAddressInfo remoteAddress, int remotePort)
        {
            if (remotePort < 0) throw new ArgumentException("remotePort cannot be less than zero.", "remotePort");

            var addressFamily = remoteAddress.AddressFamily == IpAddressFamily.InterNetwork
               ? AddressFamily.InterNetwork
               : AddressFamily.InterNetworkV6;

            var retVal = new Socket(addressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);

            try
            {
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
            catch (SocketException)
            {
                // This is not supported on all operating systems (qnap)
            }
            
            try
            {
                return new UdpSocket(retVal, new IpEndPointInfo(remoteAddress, remotePort));
            }
            catch
            {
                if (retVal != null)
                    retVal.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Creates a new UDP acceptSocket and binds it to the specified local port.
        /// </summary>
        /// <param name="localPort">An integer specifying the local port to bind the acceptSocket to.</param>
        public ISocket CreateUdpSocket(int localPort)
        {
            if (localPort < 0) throw new ArgumentException("localPort cannot be less than zero.", "localPort");

            var retVal = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            try
            {
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                return new UdpSocket(retVal, localPort, IPAddress.Any);
            }
            catch
            {
                if (retVal != null)
                    retVal.Dispose();

                throw;
            }
        }

        public ISocket CreateUdpBroadcastSocket(int localPort)
        {
            if (localPort < 0) throw new ArgumentException("localPort cannot be less than zero.", "localPort");

            var retVal = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            try
            {
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);

                return new UdpSocket(retVal, localPort, IPAddress.Any);
            }
            catch
            {
                if (retVal != null)
                    retVal.Dispose();

                throw;
            }
        }
        
        /// <summary>
              /// Creates a new UDP acceptSocket that is a member of the SSDP multicast local admin group and binds it to the specified local port.
              /// </summary>
              /// <returns>An implementation of the <see cref="ISocket"/> interface used by RSSDP components to perform acceptSocket operations.</returns>
        public ISocket CreateSsdpUdpSocket(IpAddressInfo localIpAddress, int localPort)
        {
            if (localPort < 0) throw new ArgumentException("localPort cannot be less than zero.", "localPort");

            var retVal = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            try
            {
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 4);

                var localIp = NetworkManager.ToIPAddress(localIpAddress);

                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse("239.255.255.250"), localIp));
                return new UdpSocket(retVal, localPort, localIp);
            }
            catch
            {
                if (retVal != null)
                    retVal.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Creates a new UDP acceptSocket that is a member of the specified multicast IP address, and binds it to the specified local port.
        /// </summary>
        /// <param name="ipAddress">The multicast IP address to make the acceptSocket a member of.</param>
        /// <param name="multicastTimeToLive">The multicast time to live value for the acceptSocket.</param>
        /// <param name="localPort">The number of the local port to bind to.</param>
        /// <returns></returns>
        public ISocket CreateUdpMulticastSocket(string ipAddress, int multicastTimeToLive, int localPort)
        {
            if (ipAddress == null) throw new ArgumentNullException("ipAddress");
            if (ipAddress.Length == 0) throw new ArgumentException("ipAddress cannot be an empty string.", "ipAddress");
            if (multicastTimeToLive <= 0) throw new ArgumentException("multicastTimeToLive cannot be zero or less.", "multicastTimeToLive");
            if (localPort < 0) throw new ArgumentException("localPort cannot be less than zero.", "localPort");

            var retVal = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);

            try
            {
                retVal.ExclusiveAddressUse = false;
                //retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                retVal.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, multicastTimeToLive);

                var localIp = IPAddress.Any;

                retVal.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse(ipAddress), localIp));
                retVal.MulticastLoopback = true;

                return new UdpSocket(retVal, localPort, localIp);
            }
            catch
            {
                if (retVal != null)
                    retVal.Dispose();

                throw;
            }
        }

        public Stream CreateNetworkStream(ISocket socket, bool ownsSocket)
        {
            var netSocket = (UdpSocket)socket;

            return new SocketStream(netSocket.Socket, ownsSocket);
        }
    }

    public class SocketStream : Stream
    {
        private readonly Socket _socket;

        public SocketStream(Socket socket, bool ownsSocket)
        {
            _socket = socket;
        }

        public override void Flush()
        {
        }

        public override bool CanRead
        {
            get { return true; }
        }
        public override bool CanSeek
        {
            get { return false; }
        }
        public override bool CanWrite
        {
            get { return true; }
        }
        public override long Length
        {
            get { throw new NotImplementedException(); }
        }
        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _socket.Send(buffer, offset, count, SocketFlags.None);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _socket.BeginSend(buffer, offset, count, SocketFlags.None, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _socket.EndSend(asyncResult);
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _socket.Receive(buffer, offset, count, SocketFlags.None);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _socket.BeginReceive(buffer, offset, count, SocketFlags.None, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _socket.EndReceive(asyncResult);
        }
    }

}
