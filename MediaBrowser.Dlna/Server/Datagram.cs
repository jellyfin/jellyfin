using MediaBrowser.Model.Logging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MediaBrowser.Dlna.Server
{
    public class Datagram
    {
        public IPEndPoint EndPoint { get; private set; }
        public IPAddress LocalAddress { get; private set; }
        public string Message { get; private set; }
        public bool Sticky { get; private set; }

        public int SendCount { get; private set; }

        private readonly ILogger _logger;

        public Datagram(IPEndPoint endPoint, IPAddress localAddress, ILogger logger, string message, bool sticky)
        {
            Message = message;
            _logger = logger;
            Sticky = sticky;
            LocalAddress = localAddress;
            EndPoint = endPoint;
        }

        public void Send()
        {
            var msg = Encoding.ASCII.GetBytes(Message);
            try
            {
                var client = new UdpClient();
                client.Client.Bind(new IPEndPoint(LocalAddress, 0));
                client.BeginSend(msg, msg.Length, EndPoint, result =>
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
    }
}
