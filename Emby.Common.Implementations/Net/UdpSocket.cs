using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Threading.Tasks;
using Emby.Common.Implementations.Networking;
using MediaBrowser.Model.Net;

namespace Emby.Common.Implementations.Net
{
    // THIS IS A LINKED FILE - SHARED AMONGST MULTIPLE PLATFORMS	
    // Be careful to check any changes compile and work for all platform projects it is shared in.

    internal sealed class UdpSocket : DisposableManagedObjectBase, IUdpSocket
    {

        #region Fields

        private Socket _Socket;
        private int _LocalPort;

        #endregion

        #region Constructors

        public UdpSocket(Socket socket, int localPort, IPAddress ip)
        {
            if (socket == null) throw new ArgumentNullException("socket");

            _Socket = socket;
            _LocalPort = localPort;

            _Socket.Bind(new IPEndPoint(ip, _LocalPort));
            if (_LocalPort == 0)
                _LocalPort = (_Socket.LocalEndPoint as IPEndPoint).Port;
        }

        #endregion

        #region IUdpSocket Members

        public Task<SocketReceiveResult> ReceiveAsync()
        {
            ThrowIfDisposed();

            var tcs = new TaskCompletionSource<SocketReceiveResult>();

            EndPoint receivedFromEndPoint = new IPEndPoint(IPAddress.Any, 0);
            var state = new AsyncReceiveState(_Socket, receivedFromEndPoint);
            state.TaskCompletionSource = tcs;

#if NETSTANDARD1_6
            _Socket.ReceiveFromAsync(new ArraySegment<Byte>(state.Buffer),SocketFlags.None, state.EndPoint)
                .ContinueWith((task, asyncState) =>
                {
                    if (task.Status != TaskStatus.Faulted)
                    {
                        var receiveState = asyncState as AsyncReceiveState;
                        receiveState.EndPoint = task.Result.RemoteEndPoint;
                        ProcessResponse(receiveState, () => task.Result.ReceivedBytes);
                    }
                }, state);
#else
            _Socket.BeginReceiveFrom(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, ref state.EndPoint, ProcessResponse, state);
#endif

            return tcs.Task;
        }

        public Task SendAsync(byte[] buffer, int size, IpEndPointInfo endPoint)
        {
            ThrowIfDisposed();

            if (buffer == null) throw new ArgumentNullException("messageData");
            if (endPoint == null) throw new ArgumentNullException("endPoint");

#if NETSTANDARD1_6

            if (size != buffer.Length)
            {
                byte[] copy = new byte[size];
                Buffer.BlockCopy(buffer, 0, copy, 0, size);
                buffer = copy;
            }

            _Socket.SendTo(buffer, new IPEndPoint(IPAddress.Parse(endPoint.IpAddress.ToString()), endPoint.Port));
            return Task.FromResult(true);
#else
            var taskSource = new TaskCompletionSource<bool>();

            try
            {
                _Socket.BeginSendTo(buffer, 0, size, SocketFlags.None, new System.Net.IPEndPoint(IPAddress.Parse(endPoint.IpAddress.ToString()), endPoint.Port), result =>
                {
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

            //_Socket.SendTo(messageData, new System.Net.IPEndPoint(IPAddress.Parse(endPoint.IPAddress), endPoint.Port));

            return taskSource.Task;
#endif
        }

        #endregion

        #region Overrides

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var socket = _Socket;
                if (socket != null)
                    socket.Dispose();
            }
        }

        #endregion

        #region Private Methods

        private static void ProcessResponse(AsyncReceiveState state, Func<int> receiveData)
        {
            try
            {
                var bytesRead = receiveData();

                var ipEndPoint = state.EndPoint as IPEndPoint;
                state.TaskCompletionSource.SetResult(
                    new SocketReceiveResult()
                    {
                        Buffer = state.Buffer,
                        ReceivedBytes = bytesRead,
                        RemoteEndPoint = ToIpEndPointInfo(ipEndPoint)
                    }
                );
            }
            catch (ObjectDisposedException)
            {
                state.TaskCompletionSource.SetCanceled();
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode != SocketError.Interrupted && se.SocketErrorCode != SocketError.OperationAborted && se.SocketErrorCode != SocketError.Shutdown)
                    state.TaskCompletionSource.SetException(se);
                else
                    state.TaskCompletionSource.SetCanceled();
            }
            catch (Exception ex)
            {
                state.TaskCompletionSource.SetException(ex);
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
                var bytesRead = state.Socket.EndReceiveFrom(asyncResult, ref state.EndPoint);

                var ipEndPoint = state.EndPoint as IPEndPoint;
                state.TaskCompletionSource.SetResult(
                    new SocketReceiveResult
                    {
                        Buffer = state.Buffer,
                        ReceivedBytes = bytesRead,
                        RemoteEndPoint = ToIpEndPointInfo(ipEndPoint)
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

        #endregion

        #region Private Classes

        private class AsyncReceiveState
        {
            public AsyncReceiveState(Socket socket, EndPoint endPoint)
            {
                this.Socket = socket;
                this.EndPoint = endPoint;
            }

            public EndPoint EndPoint;
            public byte[] Buffer = new byte[8192];

            public Socket Socket { get; private set; }

            public TaskCompletionSource<SocketReceiveResult> TaskCompletionSource { get; set; }

        }

        #endregion

    }
}
