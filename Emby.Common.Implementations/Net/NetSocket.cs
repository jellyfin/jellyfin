using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Emby.Common.Implementations.Networking;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Logging;

namespace Emby.Common.Implementations.Net
{
    public class NetSocket : ISocket
    {
        public Socket Socket { get; private set; }
        private readonly ILogger _logger;

        public NetSocket(Socket socket, ILogger logger)
        {
            if (socket == null)
            {
                throw new ArgumentNullException("socket");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            Socket = socket;
            _logger = logger;
        }

        public IpEndPointInfo LocalEndPoint
        {
            get
            {
                return NetworkManager.ToIpEndPointInfo((IPEndPoint)Socket.LocalEndPoint);
            }
        }

        public IpEndPointInfo RemoteEndPoint
        {
            get
            {
                return NetworkManager.ToIpEndPointInfo((IPEndPoint)Socket.RemoteEndPoint);
            }
        }

        public void Close()
        {
#if NET46
            Socket.Close();
#else
                        Socket.Dispose();
#endif
        }

        public void Shutdown(bool both)
        {
            if (both)
            {
                Socket.Shutdown(SocketShutdown.Both);
            }
            else
            {
                // Change interface if ever needed
                throw new NotImplementedException();
            }
        }

        public void Listen(int backlog)
        {
            Socket.Listen(backlog);
        }

        public void Bind(IpEndPointInfo endpoint)
        {
            var nativeEndpoint = NetworkManager.ToIPEndPoint(endpoint);

            Socket.Bind(nativeEndpoint);
        }

        private SocketAcceptor _acceptor;
        public void StartAccept(Action<ISocket> onAccept, Func<bool> isClosed)
        {
            _acceptor = new SocketAcceptor(_logger, Socket, onAccept, isClosed);

            _acceptor.StartAccept();
        }

        public void Dispose()
        {
            Socket.Dispose();
        }
    }
}
