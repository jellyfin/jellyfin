using MediaBrowser.Controller;
using MediaBrowser.Model.ApiClient;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Net;

namespace Emby.Server.Implementations.Udp
{
    /// <summary>
    /// Provides a Udp Server
    /// </summary>
    public class UdpServer : IDisposable
    {
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        private bool _isDisposed;

        private readonly List<Tuple<string, bool, Func<string, IpEndPointInfo, Encoding, Task>>> _responders = new List<Tuple<string, bool, Func<string, IpEndPointInfo, Encoding, Task>>>();

        private readonly IServerApplicationHost _appHost;
        private readonly IJsonSerializer _json;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpServer" /> class.
        /// </summary>
        public UdpServer(ILogger logger, IServerApplicationHost appHost, IJsonSerializer json, ISocketFactory socketFactory)
        {
            _logger = logger;
            _appHost = appHost;
            _json = json;
            _socketFactory = socketFactory;

            AddMessageResponder("who is EmbyServer?", true, RespondToV2Message);
            AddMessageResponder("who is MediaBrowserServer_v2?", false, RespondToV2Message);
        }

        private void AddMessageResponder(string message, bool isSubstring, Func<string, IpEndPointInfo, Encoding, Task> responder)
        {
            _responders.Add(new Tuple<string, bool, Func<string, IpEndPointInfo, Encoding, Task>>(message, isSubstring, responder));
        }

        /// <summary>
        /// Raises the <see cref="E:MessageReceived" /> event.
        /// </summary>
        private async void OnMessageReceived(GenericEventArgs<SocketReceiveResult> e)
        {
            var message = e.Argument;

            var encoding = Encoding.UTF8;
            var responder = GetResponder(message.Buffer, message.ReceivedBytes, encoding);

            if (responder == null)
            {
                encoding = Encoding.Unicode;
                responder = GetResponder(message.Buffer, message.ReceivedBytes, encoding);
            }

            if (responder != null)
            {
                try
                {
                    await responder.Item2.Item3(responder.Item1, message.RemoteEndPoint, encoding).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in OnMessageReceived", ex);
                }
            }
        }

        private Tuple<string, Tuple<string, bool, Func<string, IpEndPointInfo, Encoding, Task>>> GetResponder(byte[] buffer, int bytesReceived, Encoding encoding)
        {
            var text = encoding.GetString(buffer, 0, bytesReceived);
            var responder = _responders.FirstOrDefault(i =>
            {
                if (i.Item2)
                {
                    return text.IndexOf(i.Item1, StringComparison.OrdinalIgnoreCase) != -1;
                }
                return string.Equals(i.Item1, text, StringComparison.OrdinalIgnoreCase);
            });

            if (responder == null)
            {
                return null;
            }
            return new Tuple<string, Tuple<string, bool, Func<string, IpEndPointInfo, Encoding, Task>>>(text, responder);
        }

        private async Task RespondToV2Message(string messageText, IpEndPointInfo endpoint, Encoding encoding)
        {
            var parts = messageText.Split('|');

            var localUrl = await _appHost.GetLocalApiUrl().ConfigureAwait(false);

            if (!string.IsNullOrEmpty(localUrl))
            {
                var response = new ServerDiscoveryInfo
                {
                    Address = localUrl,
                    Id = _appHost.SystemId,
                    Name = _appHost.FriendlyName
                };

                await SendAsync(encoding.GetBytes(_json.SerializeToString(response)), endpoint).ConfigureAwait(false);

                if (parts.Length > 1)
                {
                    _appHost.EnableLoopback(parts[1]);
                }
            }
            else
            {
                _logger.Warn("Unable to respond to udp request because the local ip address could not be determined.");
            }
        }

        /// <summary>
        /// The _udp client
        /// </summary>
        private ISocket _udpClient;
        private readonly ISocketFactory _socketFactory;

        /// <summary>
        /// Starts the specified port.
        /// </summary>
        /// <param name="port">The port.</param>
        public void Start(int port)
        {
            _udpClient = _socketFactory.CreateUdpSocket(port);

            Task.Run(() => BeginReceive());
        }

        private readonly byte[] _receiveBuffer = new byte[8192];

        private void BeginReceive()
        {
            if (_isDisposed)
            {
                return;
            }

            try
            {
                var result = _udpClient.BeginReceive(_receiveBuffer, 0, _receiveBuffer.Length, OnReceiveResult);

                if (result.CompletedSynchronously)
                {
                    OnReceiveResult(result);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error receiving udp message", ex);
            }
        }

        private void OnReceiveResult(IAsyncResult result)
        {
            if (_isDisposed)
            {
                return;
            }
            
            try
            {
                var socketResult = _udpClient.EndReceive(result);

                OnMessageReceived(socketResult);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error receiving udp message", ex);
            }

            BeginReceive();
        }

        /// <summary>
        /// Called when [message received].
        /// </summary>
        /// <param name="message">The message.</param>
        private void OnMessageReceived(SocketReceiveResult message)
        {
            if (_isDisposed)
            {
                return;
            }

            if (message.RemoteEndPoint.Port == 0)
            {
                return;
            }

            try
            {
                OnMessageReceived(new GenericEventArgs<SocketReceiveResult>
                {
                    Argument = message
                });
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error handling UDP message", ex);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                _isDisposed = true;

                if (_udpClient != null)
                {
                    _udpClient.Dispose();
                }
            }
        }

        public async Task SendAsync(byte[] bytes, IpEndPointInfo remoteEndPoint)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (remoteEndPoint == null)
            {
                throw new ArgumentNullException("remoteEndPoint");
            }

            try
            {
                await _udpClient.SendToAsync(bytes, 0, bytes.Length, remoteEndPoint, CancellationToken.None).ConfigureAwait(false);

                _logger.Info("Udp message sent to {0}", remoteEndPoint);
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending message to {0}", ex, remoteEndPoint);
            }
        }
    }

}
