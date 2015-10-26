using MediaBrowser.Model.Logging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MediaBrowser.Dlna.Ssdp
{
    public class Datagram
    {
        public EndPoint ToEndPoint { get; private set; }
        public EndPoint FromEndPoint { get; private set; }
        public string Message { get; private set; }
        public bool IsBroadcast { get; private set; }
        public bool EnableDebugLogging { get; private set; }

        private readonly ILogger _logger;

        public Datagram(EndPoint toEndPoint, EndPoint fromEndPoint, ILogger logger, string message, bool isBroadcast, bool enableDebugLogging)
        {
            Message = message;
            _logger = logger;
            EnableDebugLogging = enableDebugLogging;
            IsBroadcast = isBroadcast;
            FromEndPoint = fromEndPoint;
            ToEndPoint = toEndPoint;
        }

        public void Send()
        {
            var msg = Encoding.ASCII.GetBytes(Message);

            var socket = CreateSocket();

            if (socket == null)
            {
                return;
            }

            if (FromEndPoint != null)
            {
                try
                {
                    socket.Bind(FromEndPoint);
                }
                catch (Exception ex)
                {
                    if (EnableDebugLogging)
                    {
                        _logger.ErrorException("Error binding datagram socket", ex);
                    }

                    if (IsBroadcast)
                    {
                        CloseSocket(socket, false);
                        return;
                    }
                }
            }

            try
            {
                socket.BeginSendTo(msg, 0, msg.Length, SocketFlags.None, ToEndPoint, result =>
                {
                    try
                    {
                        socket.EndSend(result);
                    }
                    catch (Exception ex)
                    {
                        if (EnableDebugLogging)
                        {
                            _logger.ErrorException("Error sending Datagram to {0} from {1}: " + Message, ex, ToEndPoint, FromEndPoint == null ? "" : FromEndPoint.ToString());
                        }
                    }
                    finally
                    {
                        CloseSocket(socket, true);
                    }
                }, null);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending Datagram to {0} from {1}: " + Message, ex, ToEndPoint, FromEndPoint == null ? "" : FromEndPoint.ToString());
                CloseSocket(socket, false);
            }
        }

        private void CloseSocket(Socket socket, bool logError)
        {
            try
            {
                socket.Close();
            }
            catch (Exception ex)
            {
                if (logError && EnableDebugLogging)
                {
                    _logger.ErrorException("Error closing datagram socket", ex);
                }
            }
        }

        private Socket CreateSocket()
        {
            try
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 4);

                if (IsBroadcast)
                {
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                }

                return socket;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error creating socket", ex);
                return null;
            }
        }
    }
}
