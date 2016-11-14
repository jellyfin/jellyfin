using System;
using System.Net.Sockets;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;

namespace Emby.Common.Implementations.Net
{
    public class SocketAcceptor
    {
        private readonly ILogger _logger;
        private readonly Socket _originalSocket;
        private readonly Func<bool> _isClosed;
        private readonly Action<ISocket> _onAccept;

        public SocketAcceptor(ILogger logger, Socket originalSocket, Action<ISocket> onAccept, Func<bool> isClosed)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            if (originalSocket == null)
            {
                throw new ArgumentNullException("originalSocket");
            }
            if (onAccept == null)
            {
                throw new ArgumentNullException("onAccept");
            }
            if (isClosed == null)
            {
                throw new ArgumentNullException("isClosed");
            }

            _logger = logger;
            _originalSocket = originalSocket;
            _isClosed = isClosed;
            _onAccept = onAccept;
        }

        public void StartAccept()
        {
            Socket dummy = null;
            StartAccept(null, ref dummy);
        }

        public void StartAccept(SocketAsyncEventArgs acceptEventArg, ref Socket accepted)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }

            try
            {
                bool willRaiseEvent = _originalSocket.AcceptAsync(acceptEventArg);

                if (!willRaiseEvent)
                {
                    ProcessAccept(acceptEventArg);
                }
            }
            catch (Exception ex)
            {
                if (accepted != null)
                {
                    try
                    {
#if NET46
                        accepted.Close();
#else
                        accepted.Dispose();
#endif
                    }
                    catch
                    {
                    }
                    accepted = null;
                }
            }
        }

        // This method is the callback method associated with Socket.AcceptAsync  
        // operations and is invoked when an accept operation is complete 
        // 
        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (_isClosed())
            {
                return;
            }

            // http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.acceptasync%28v=vs.110%29.aspx
            // Under certain conditions ConnectionReset can occur
            // Need to attept to re-accept
            if (e.SocketError == SocketError.ConnectionReset)
            {
                _logger.Error("SocketError.ConnectionReset reported. Attempting to re-accept.");
                Socket dummy = null;
                StartAccept(e, ref dummy);
                return;
            }

            var acceptSocket = e.AcceptSocket;
            if (acceptSocket != null)
            {
                //ProcessAccept(acceptSocket);
                _onAccept(new NetSocket(acceptSocket, _logger));
            }

            // Accept the next connection request
            StartAccept(e, ref acceptSocket);
        }
    }
}
