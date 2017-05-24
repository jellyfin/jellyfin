using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Emby.Common.Implementations.Networking;
using MediaBrowser.Model.Net;

namespace Emby.Common.Implementations.Net
{
    // THIS IS A LINKED FILE - SHARED AMONGST MULTIPLE PLATFORMS	
    // Be careful to check any changes compile and work for all platform projects it is shared in.

    internal sealed class UdpSocket : DisposableManagedObjectBase, ISocket
    {
        private Socket _Socket;
        private int _LocalPort;

        private readonly SocketAsyncEventArgs _receiveSocketAsyncEventArgs = new SocketAsyncEventArgs()
        {
            SocketFlags = SocketFlags.None
        };

        private readonly SocketAsyncEventArgs _sendSocketAsyncEventArgs = new SocketAsyncEventArgs()
        {
            SocketFlags = SocketFlags.None
        };

        private TaskCompletionSource<SocketReceiveResult> _currentReceiveTaskCompletionSource;
        private TaskCompletionSource<int> _currentSendTaskCompletionSource;

        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        public UdpSocket(Socket socket, int localPort, IPAddress ip)
        {
            if (socket == null) throw new ArgumentNullException("socket");

            _Socket = socket;
            _LocalPort = localPort;
            LocalIPAddress = NetworkManager.ToIpAddressInfo(ip);

            _Socket.Bind(new IPEndPoint(ip, _LocalPort));

            InitReceiveSocketAsyncEventArgs();
        }

        private void InitReceiveSocketAsyncEventArgs()
        {
            var receiveBuffer = new byte[8192];
            _receiveSocketAsyncEventArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
            _receiveSocketAsyncEventArgs.Completed += _receiveSocketAsyncEventArgs_Completed;

            var sendBuffer = new byte[8192];
            _sendSocketAsyncEventArgs.SetBuffer(sendBuffer, 0, sendBuffer.Length);
            _sendSocketAsyncEventArgs.Completed += _sendSocketAsyncEventArgs_Completed;
        }

        private void _receiveSocketAsyncEventArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            var tcs = _currentReceiveTaskCompletionSource;
            if (tcs != null)
            {
                _currentReceiveTaskCompletionSource = null;

                if (e.SocketError == SocketError.Success)
                {
                    tcs.TrySetResult(new SocketReceiveResult
                    {
                        Buffer = e.Buffer,
                        ReceivedBytes = e.BytesTransferred,
                        RemoteEndPoint = ToIpEndPointInfo(e.RemoteEndPoint as IPEndPoint),
                        LocalIPAddress = LocalIPAddress
                    });
                }
                else
                {
                    tcs.TrySetException(new Exception("SocketError: " + e.SocketError));
                }
            }
        }

        private void _sendSocketAsyncEventArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            var tcs = _currentSendTaskCompletionSource;
            if (tcs != null)
            {
                _currentSendTaskCompletionSource = null;

                if (e.SocketError == SocketError.Success)
                {
                    tcs.TrySetResult(e.BytesTransferred);
                }
                else
                {
                    tcs.TrySetException(new Exception("SocketError: " + e.SocketError));
                }
            }
        }

        public UdpSocket(Socket socket, IpEndPointInfo endPoint)
        {
            if (socket == null) throw new ArgumentNullException("socket");

            _Socket = socket;
            _Socket.Connect(NetworkManager.ToIPEndPoint(endPoint));

            InitReceiveSocketAsyncEventArgs();
        }

        public IpAddressInfo LocalIPAddress
        {
            get;
            private set;
        }

        private readonly AsyncCallback _defaultAsyncCallback = (i) => { };

        public IAsyncResult BeginReceive(byte[] buffer, int offset, int count, AsyncCallback callback)
        {
            EndPoint receivedFromEndPoint = new IPEndPoint(IPAddress.Any, 0);

            return _Socket.BeginReceiveFrom(buffer, offset, count, SocketFlags.None, ref receivedFromEndPoint, callback, buffer);
        }

        public SocketReceiveResult EndReceive(IAsyncResult result)
        {
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint remoteEndPoint = (EndPoint)sender;

            var receivedBytes = _Socket.EndReceiveFrom(result, ref remoteEndPoint);

            var buffer = (byte[]) result.AsyncState;

            return new SocketReceiveResult
            {
                ReceivedBytes = receivedBytes,
                RemoteEndPoint = ToIpEndPointInfo((IPEndPoint)remoteEndPoint),
                Buffer = buffer,
                LocalIPAddress = LocalIPAddress
            };
        }

        public Task<SocketReceiveResult> ReceiveAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var result = BeginReceive(buffer, offset, count, _defaultAsyncCallback);

            return Task.Factory.FromAsync(result, EndReceive);
        }

        public Task<SocketReceiveResult> ReceiveAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[8192];

            return ReceiveAsync(buffer, 0, buffer.Length, cancellationToken);
        }

        public Task SendToAsync(byte[] buffer, int offset, int size, IpEndPointInfo endPoint, CancellationToken cancellationToken)
        {
            var result = BeginSendTo(buffer, offset, size, endPoint, _defaultAsyncCallback, null);

            return Task.Factory.FromAsync(result, EndSendTo);
        }

        public IAsyncResult BeginSendTo(byte[] buffer, int offset, int size, IpEndPointInfo endPoint, AsyncCallback callback, object state)
        {
            var ipEndPoint = NetworkManager.ToIPEndPoint(endPoint);

            return _Socket.BeginSendTo(buffer, offset, size, SocketFlags.None, ipEndPoint, callback, state);
        }

        public int EndSendTo(IAsyncResult result)
        {
            return _Socket.EndSendTo(result);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var socket = _Socket;
                if (socket != null)
                    socket.Dispose();

                _sendLock.Dispose();

                var tcs = _currentReceiveTaskCompletionSource;
                if (tcs != null)
                {
                    tcs.TrySetCanceled();
                }
                var sendTcs = _currentSendTaskCompletionSource;
                if (sendTcs != null)
                {
                    sendTcs.TrySetCanceled();
                }
            }
        }

        private static IpEndPointInfo ToIpEndPointInfo(IPEndPoint endpoint)
        {
            if (endpoint == null)
            {
                return null;
            }

            return NetworkManager.ToIpEndPointInfo(endpoint);
        }

        private class AsyncReceiveState
        {
            public AsyncReceiveState(Socket socket, EndPoint remoteEndPoint)
            {
                this.Socket = socket;
                this.RemoteEndPoint = remoteEndPoint;
            }

            public EndPoint RemoteEndPoint;
            public byte[] Buffer = new byte[8192];

            public Socket Socket { get; private set; }

            public TaskCompletionSource<SocketReceiveResult> TaskCompletionSource { get; set; }

        }
    }
}
