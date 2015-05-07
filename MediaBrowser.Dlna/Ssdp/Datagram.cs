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
        public bool IgnoreBindFailure { get; private set; }
        public bool EnableDebugLogging { get; private set; }

        private readonly ILogger _logger;

        public Datagram(EndPoint toEndPoint, EndPoint fromEndPoint, ILogger logger, string message, bool ignoreBindFailure, bool enableDebugLogging)
        {
            Message = message;
            _logger = logger;
            EnableDebugLogging = enableDebugLogging;
            IgnoreBindFailure = ignoreBindFailure;
            FromEndPoint = fromEndPoint;
            ToEndPoint = toEndPoint;
        }

        public void Send()
        {
            var msg = Encoding.ASCII.GetBytes(Message);
            try
            {
                var client = CreateSocket();

                if (FromEndPoint != null)
                {
                    try
                    {
                        client.Bind(FromEndPoint);
                    }
                    catch (Exception ex)
                    {
                        if (EnableDebugLogging)
                        {
                            _logger.ErrorException("Error binding datagram socket", ex);
                        }
                        
                        if (!IgnoreBindFailure) throw;
                    }
                }

                client.BeginSendTo(msg, 0, msg.Length, SocketFlags.None, ToEndPoint, result =>
                {
                    try
                    {
                        client.EndSend(result);
                    }
                    catch (Exception ex)
                    {
                        if (!IgnoreBindFailure || EnableDebugLogging)
                        {
                            _logger.ErrorException("Error sending Datagram to {0} from {1}: " + Message, ex, ToEndPoint, FromEndPoint == null ? "" : FromEndPoint.ToString());
                        }
                    }
                    finally
                    {
                        try
                        {
                            client.Close();
                        }
                        catch (Exception ex)
                        {
                            if (EnableDebugLogging)
                            {
                                _logger.ErrorException("Error closing datagram socket", ex);
                            }
                        }
                    }
                }, null);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending Datagram to {0} from {1}: " + Message, ex, ToEndPoint, FromEndPoint == null ? "" : FromEndPoint.ToString());
            }
        }

        private Socket CreateSocket()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            return socket;
        }
    }
}
