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

        /// <summary>
        /// The number of times to send the message
        /// </summary>
        public int TotalSendCount { get; private set; }
        public bool IgnoreBindFailure { get; private set; }

        /// <summary>
        /// The number of times the message has been sent
        /// </summary>
        public int SendCount { get; private set; }

        private readonly ILogger _logger;

        public Datagram(EndPoint toEndPoint, EndPoint fromEndPoint, ILogger logger, string message, int totalSendCount, bool ignoreBindFailure)
        {
            Message = message;
            _logger = logger;
            IgnoreBindFailure = ignoreBindFailure;
            TotalSendCount = totalSendCount;
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
                    catch
                    {
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
                        if (!IgnoreBindFailure)
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
                        catch (Exception)
                        {
                        }
                    }
                }, null);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending Datagram to {0} from {1}: " + Message, ex, ToEndPoint, FromEndPoint == null ? "" : FromEndPoint.ToString());
            }
            ++SendCount;
        }

        private Socket CreateSocket()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            return socket;
        }
    }
}
