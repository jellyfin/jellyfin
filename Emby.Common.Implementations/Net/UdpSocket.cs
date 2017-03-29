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

        public Task<SocketReceiveResult> ReceiveAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            var tcs = new TaskCompletionSource<SocketReceiveResult>();
            EndPoint receivedFromEndPoint = new IPEndPoint(IPAddress.Any, 0);

            var state = new AsyncReceiveState(_Socket, receivedFromEndPoint);
            state.TaskCompletionSource = tcs;

            cancellationToken.Register(() => tcs.TrySetCanceled());

            _receiveSocketAsyncEventArgs.RemoteEndPoint = receivedFromEndPoint;
            _currentReceiveTaskCompletionSource = tcs;

            try
            {
                var willRaiseEvent = _Socket.ReceiveFromAsync(_receiveSocketAsyncEventArgs);

                if (!willRaiseEvent)
                {
                    _receiveSocketAsyncEventArgs_Completed(this, _receiveSocketAsyncEventArgs);
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        public Task SendAsync(byte[] buffer, int size, IpEndPointInfo endPoint, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (buffer == null) throw new ArgumentNullException("messageData");
            if (endPoint == null) throw new ArgumentNullException("endPoint");

            var ipEndPoint = NetworkManager.ToIPEndPoint(endPoint);

#if NETSTANDARD1_6

            if (size != buffer.Length)
            {
                byte[] copy = new byte[size];
                Buffer.BlockCopy(buffer, 0, copy, 0, size);
                buffer = copy;
            }

            cancellationToken.ThrowIfCancellationRequested();

            _Socket.SendTo(buffer, ipEndPoint);
            return Task.FromResult(true);
#else
            var taskSource = new TaskCompletionSource<bool>();

            try
            {
                _Socket.BeginSendTo(buffer, 0, size, SocketFlags.None, ipEndPoint, result =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        taskSource.TrySetCanceled();
                        return;
                    }
                    try
                    {
                        _Socket.EndSend(result);
                        taskSource.TrySetResult(true);
                    }
                    catch (Exception ex)
                    {
                        taskSource.TrySetException(ex);
                    }

                }, null);
            }
            catch (Exception ex)
            {
                taskSource.TrySetException(ex);
            }

            return taskSource.Task;
#endif
            //ThrowIfDisposed();

            //if (buffer == null) throw new ArgumentNullException("messageData");
            //if (endPoint == null) throw new ArgumentNullException("endPoint");

            //cancellationToken.ThrowIfCancellationRequested();

            //var tcs = new TaskCompletionSource<int>();

            //cancellationToken.Register(() => tcs.TrySetCanceled());

            //_sendSocketAsyncEventArgs.SetBuffer(buffer, 0, size);
            //_sendSocketAsyncEventArgs.RemoteEndPoint = NetworkManager.ToIPEndPoint(endPoint);
            //_currentSendTaskCompletionSource = tcs;

            //var willRaiseEvent = _Socket.SendAsync(_sendSocketAsyncEventArgs);

            //if (!willRaiseEvent)
            //{
            //    _sendSocketAsyncEventArgs_Completed(this, _sendSocketAsyncEventArgs);
            //}

            //return tcs.Task;
        }

        public async Task SendWithLockAsync(byte[] buffer, int size, IpEndPointInfo endPoint, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            //await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await SendAsync(buffer, size, endPoint, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                //_sendLock.Release();
            }
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

        private void ProcessResponse(IAsyncResult asyncResult)
        {
#if NET46
            var state = asyncResult.AsyncState as AsyncReceiveState;
            try
            {
                var bytesRead = state.Socket.EndReceiveFrom(asyncResult, ref state.RemoteEndPoint);

                var ipEndPoint = state.RemoteEndPoint as IPEndPoint;
                state.TaskCompletionSource.SetResult(
                    new SocketReceiveResult
                    {
                        Buffer = state.Buffer,
                        ReceivedBytes = bytesRead,
                        RemoteEndPoint = ToIpEndPointInfo(ipEndPoint),
                        LocalIPAddress = LocalIPAddress
                    }
                );
            }
            catch (ObjectDisposedException)
            {
                state.TaskCompletionSource.SetCanceled();
            }
            catch (Exception ex)
            {
                state.TaskCompletionSource.SetException(ex);
            }
#endif
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
