using MediaBrowser.Model.Logging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MediaBrowser.Dlna.Ssdp
{
    public class Datagram
    {
        public IPEndPoint ToEndPoint { get; private set; }
        public IPAddress FromEndPoint { get; private set; }
        public string Message { get; private set; }

        /// <summary>
        /// The number of times to send the message
        /// </summary>
        public int TotalSendCount { get; private set; }

        /// <summary>
        /// The number of times the message has been sent
        /// </summary>
        public int SendCount { get; private set; }

        private readonly ILogger _logger;

        public Datagram(IPEndPoint toEndPoint, IPAddress fromEndPoint, ILogger logger, string message, int totalSendCount)
        {
            Message = message;
            _logger = logger;
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
                    client.Bind(new IPEndPoint(FromEndPoint, 0));
                }

                client.BeginSendTo(msg, 0, msg.Length, SocketFlags.None, ToEndPoint, result =>
                {
                    try
                    {
                        client.EndSend(result);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error sending Datagram", ex);
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
                _logger.ErrorException("Error sending Datagram", ex);
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
