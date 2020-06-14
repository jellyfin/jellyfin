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
        private int _localPort;
        private bool _disposed = false;

        public Socket Socket => _socket;

        public IPAddress LocalIPAddress { get; }

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

        public UdpSocket(Socket socket, int localPort, IPAddress ip)
        {
            if (socket == null) throw new ArgumentNullException(nameof(socket));

            _socket = socket;
            _localPort = localPort;
            LocalIPAddress = ip;

            _socket.Bind(new IPEndPoint(ip, _localPort));

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
                        RemoteEndPoint = e.RemoteEndPoint as IPEndPoint,
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

        public UdpSocket(Socket socket, IPEndPoint endPoint)
        {
            if (socket == null) throw new ArgumentNullException(nameof(socket));

            _socket = socket;
            _socket.Connect(endPoint);

            InitReceiveSocketAsyncEventArgs();
        }

        public IAsyncResult BeginReceive(byte[] buffer, int offset, int count, AsyncCallback callback)
        {
            ThrowIfDisposed();

            EndPoint receivedFromEndPoint = new IPEndPoint(IPAddress.Any, 0);

            return _socket.BeginReceiveFrom(buffer, offset, count, SocketFlags.None, ref receivedFromEndPoint, callback, buffer);
        }

        public int Receive(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();

            return _socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
        }

        public SocketReceiveResult EndReceive(IAsyncResult result)
        {
            ThrowIfDisposed();

            var sender = new IPEndPoint(IPAddress.Any, 0);
            var remoteEndPoint = (EndPoint)sender;

            var receivedBytes = _socket.EndReceiveFrom(result, ref remoteEndPoint);

            var buffer = (byte[])result.AsyncState;

            return new SocketReceiveResult
            {
                ReceivedBytes = receivedBytes,
                RemoteEndPoint = (IPEndPoint)remoteEndPoint,
                Buffer = buffer,
                LocalIPAddress = LocalIPAddress
            };
        }

        public Task<SocketReceiveResult> ReceiveAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            var taskCompletion = new TaskCompletionSource<SocketReceiveResult>();
            bool isResultSet = false;

            Action<IAsyncResult> callback = callbackResult =>
            {
                try
                {
                    if (!isResultSet)
                    {
                        isResultSet = true;
                        taskCompletion.TrySetResult(EndReceive(callbackResult));
                    }
                }
                catch (Exception ex)
                {
                    taskCompletion.TrySetException(ex);
                }
            };

            var result = BeginReceive(buffer, offset, count, new AsyncCallback(callback));

            if (result.CompletedSynchronously)
            {
                callback(result);
                return taskCompletion.Task;
            }

            cancellationToken.Register(() => taskCompletion.TrySetCanceled());

            return taskCompletion.Task;
        }

        public Task SendToAsync(byte[] buffer, int offset, int size, IPEndPoint endPoint, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            var taskCompletion = new TaskCompletionSource<int>();
            bool isResultSet = false;

            Action<IAsyncResult> callback = callbackResult =>
            {
                try
                {
                    if (!isResultSet)
                    {
                        isResultSet = true;
                        taskCompletion.TrySetResult(EndSendTo(callbackResult));
                    }
                }
                catch (Exception ex)
                {
                    taskCompletion.TrySetException(ex);
                }
            };

            var result = BeginSendTo(buffer, offset, size, endPoint, new AsyncCallback(callback), null);

            if (result.CompletedSynchronously)
            {
                callback(result);
                return taskCompletion.Task;
            }

            cancellationToken.Register(() => taskCompletion.TrySetCanceled());

            return taskCompletion.Task;
        }

        public IAsyncResult BeginSendTo(byte[] buffer, int offset, int size, IPEndPoint endPoint, AsyncCallback callback, object state)
        {
            ThrowIfDisposed();

            return _socket.BeginSendTo(buffer, offset, size, SocketFlags.None, endPoint, callback, state);
        }

        public int EndSendTo(IAsyncResult result)
        {
            ThrowIfDisposed();

            return _socket.EndSendTo(result);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UdpSocket));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _socket?.Dispose();
            _currentReceiveTaskCompletionSource?.TrySetCanceled();
            _currentSendTaskCompletionSource?.TrySetCanceled();

            _socket = null;
            _currentReceiveTaskCompletionSource = null;
            _currentSendTaskCompletionSource = null;

            _disposed = true;
        }
    }
}
