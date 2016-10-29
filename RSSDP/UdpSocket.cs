using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rssdp.Infrastructure;

namespace Rssdp
{
    // THIS IS A LINKED FILE - SHARED AMONGST MULTIPLE PLATFORMS	
    // Be careful to check any changes compile and work for all platform projects it is shared in.

    internal sealed class UdpSocket : DisposableManagedObjectBase, IUdpSocket
    {

        #region Fields

        private System.Net.Sockets.Socket _Socket;
        private int _LocalPort;

        #endregion

        #region Constructors

        public UdpSocket(System.Net.Sockets.Socket socket, int localPort, string ipAddress)
        {
            if (socket == null) throw new ArgumentNullException("socket");

            _Socket = socket;
            _LocalPort = localPort;

            IPAddress ip = null;
            if (String.IsNullOrEmpty(ipAddress))
                ip = IPAddress.Any;
            else
                ip = IPAddress.Parse(ipAddress);

            _Socket.Bind(new IPEndPoint(ip, _LocalPort));
            if (_LocalPort == 0)
                _LocalPort = (_Socket.LocalEndPoint as IPEndPoint).Port;
        }

        #endregion

        #region IUdpSocket Members

        public System.Threading.Tasks.Task<ReceivedUdpData> ReceiveAsync()
        {
            ThrowIfDisposed();

            var tcs = new TaskCompletionSource<ReceivedUdpData>();

            System.Net.EndPoint receivedFromEndPoint = new IPEndPoint(IPAddress.Any, 0);
            var state = new AsyncReceiveState(_Socket, receivedFromEndPoint);
            state.TaskCompletionSource = tcs;

#if NETSTANDARD1_6
            _Socket.ReceiveFromAsync(new System.ArraySegment<Byte>(state.Buffer), System.Net.Sockets.SocketFlags.None, state.EndPoint)
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
            _Socket.BeginReceiveFrom(state.Buffer, 0, state.Buffer.Length, System.Net.Sockets.SocketFlags.None, ref state.EndPoint, new AsyncCallback(this.ProcessResponse), state);
#endif

            return tcs.Task;
        }

        public Task SendTo(byte[] messageData, UdpEndPoint endPoint)
        {
            ThrowIfDisposed();

            if (messageData == null) throw new ArgumentNullException("messageData");
            if (endPoint == null) throw new ArgumentNullException("endPoint");

#if NETSTANDARD1_6
            _Socket.SendTo(messageData, new System.Net.IPEndPoint(IPAddress.Parse(endPoint.IPAddress), endPoint.Port));
            return Task.FromResult(true);
#else
            var taskSource = new TaskCompletionSource<bool>();

            try
            {
                _Socket.BeginSendTo(messageData, 0, messageData.Length, SocketFlags.None, new System.Net.IPEndPoint(IPAddress.Parse(endPoint.IPAddress), endPoint.Port), result =>
                {
                    try
                    {
                        _Socket.EndSend(result);
                        taskSource.TrySetResult(true);
                    }
                    catch (SocketException ex)
                    {
                        taskSource.TrySetException(ex);
                    }
                    catch (ObjectDisposedException ex)
                    {
                        taskSource.TrySetException(ex);
                    }
                    catch (InvalidOperationException ex)
                    {
                        taskSource.TrySetException(ex);
                    }
                    catch (SecurityException ex)
                    {
                        taskSource.TrySetException(ex);
                    }
                }, null);
            }
            catch (SocketException ex)
            {
                taskSource.TrySetException(ex);
            }
            catch (ObjectDisposedException ex)
            {
                taskSource.TrySetException(ex);
            }
            catch (SecurityException ex)
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions via task methods should be reported by task completion source, so this should be ok.")]
        private static void ProcessResponse(AsyncReceiveState state, Func<int> receiveData)
        {
            try
            {
                var bytesRead = receiveData();

                var ipEndPoint = state.EndPoint as IPEndPoint;
                state.TaskCompletionSource.SetResult(
                    new ReceivedUdpData()
                    {
                        Buffer = state.Buffer,
                        ReceivedBytes = bytesRead,
                        ReceivedFrom = new UdpEndPoint()
                        {
                            IPAddress = ipEndPoint.Address.ToString(),
                            Port = ipEndPoint.Port
                        }
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions via task methods should be reported by task completion source, so this should be ok.")]
        private void ProcessResponse(IAsyncResult asyncResult)
        {
#if NET46
            var state = asyncResult.AsyncState as AsyncReceiveState;
            try
            {
                var bytesRead = state.Socket.EndReceiveFrom(asyncResult, ref state.EndPoint);

                var ipEndPoint = state.EndPoint as IPEndPoint;
                state.TaskCompletionSource.SetResult(
                    new ReceivedUdpData()
                    {
                        Buffer = state.Buffer,
                        ReceivedBytes = bytesRead,
                        ReceivedFrom = new UdpEndPoint()
                        {
                            IPAddress = ipEndPoint.Address.ToString(),
                            Port = ipEndPoint.Port
                        }
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
#endif
        }

        #endregion

        #region Private Classes

        private class AsyncReceiveState
        {
            public AsyncReceiveState(System.Net.Sockets.Socket socket, EndPoint endPoint)
            {
                this.Socket = socket;
                this.EndPoint = endPoint;
            }

            public EndPoint EndPoint;
            public byte[] Buffer = new byte[SsdpConstants.DefaultUdpSocketBufferSize];

            public System.Net.Sockets.Socket Socket { get; private set; }

            public TaskCompletionSource<ReceivedUdpData> TaskCompletionSource { get; set; }

        }

        #endregion

    }
}