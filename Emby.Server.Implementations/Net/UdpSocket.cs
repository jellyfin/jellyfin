#pragma warning disable CS1591

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Net;

namespace Emby.Server.Implementations.Net
{
    // THIS IS A LINKED FILE - SHARED AMONGST MULTIPLE PLATFORMS
    // Be careful to check any changes compile and work for all platform projects it is shared in.

    public sealed class UdpSocket : ISocket, IDisposable
    {
        private Socket _socket;
        private readonly int _localPort;
        private bool _disposed = false;

        public UdpSocket(Socket socket, int localPort, IPAddress ip)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            _socket = socket;
            _localPort = localPort;
            LocalIPAddress = ip;

            _socket.Bind(new IPEndPoint(ip, _localPort));
        }

        public UdpSocket(Socket socket, IPEndPoint endPoint)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            _socket = socket;
            _socket.Connect(endPoint);
        }

        public IPAddress LocalIPAddress { get; }

        public async Task<SocketReceiveResult> ReceiveAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            SocketReceiveFromResult r = await _socket.ReceiveFromAsync(new ArraySegment<byte>(buffer, offset, count), SocketFlags.None, new IPEndPoint(IPAddress.Any, 0)).ConfigureAwait(false);
            return new SocketReceiveResult()
            {
                ReceivedBytes = r.ReceivedBytes,
                RemoteEndPoint = (IPEndPoint)r.RemoteEndPoint,
                Buffer = buffer,
                LocalIPAddress = LocalIPAddress
            };
        }

        public Task SendToAsync(byte[] buffer, int offset, int count, IPEndPoint endPoint, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            return _socket.SendToAsync(new ArraySegment<byte>(buffer, offset, count), SocketFlags.None, endPoint);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UdpSocket));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _socket?.Dispose();

            _socket = null;

            _disposed = true;
        }
    }
}
