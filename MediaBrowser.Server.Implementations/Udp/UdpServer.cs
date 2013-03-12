using MediaBrowser.Common.Implementations.NetworkManagement;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Udp
{
    /// <summary>
    /// Provides a Udp Server
    /// </summary>
    public class UdpServer : IUdpServer
    {
        /// <summary>
        /// Occurs when [message received].
        /// </summary>
        public event EventHandler<UdpMessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpServer" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public UdpServer(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Raises the <see cref="E:MessageReceived" /> event.
        /// </summary>
        /// <param name="e">The <see cref="UdpMessageReceivedEventArgs" /> instance containing the event data.</param>
        protected virtual void OnMessageReceived(UdpMessageReceivedEventArgs e)
        {
            EventHandler<UdpMessageReceivedEventArgs> handler = MessageReceived;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// The _udp client
        /// </summary>
        private UdpClient _udpClient;

        /// <summary>
        /// Starts the specified port.
        /// </summary>
        /// <param name="port">The port.</param>
        public void Start(int port)
        {
            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));

            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            CreateObservable().Subscribe(OnMessageReceived);
        }

        /// <summary>
        /// Creates the observable.
        /// </summary>
        /// <returns>IObservable{UdpReceiveResult}.</returns>
        private IObservable<UdpReceiveResult> CreateObservable()
        {
            return Observable.Create<UdpReceiveResult>(obs =>
                                Observable.FromAsync(() =>
                                {
                                    try
                                    {
                                        return _udpClient.ReceiveAsync();
                                    }
                                    catch (ObjectDisposedException)
                                    {
                                        return Task.FromResult(new UdpReceiveResult(new byte[]{}, new IPEndPoint(IPAddress.Any, 0)));
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.ErrorException("Error receiving udp message", ex);
                                        return Task.FromResult(new UdpReceiveResult(new byte[] { }, new IPEndPoint(IPAddress.Any, 0)));
                                    }
                                })

                             .Subscribe(obs))
                             .Repeat()
                             .Retry()
                             .Publish()
                             .RefCount();
        }

        /// <summary>
        /// Called when [message received].
        /// </summary>
        /// <param name="message">The message.</param>
        private void OnMessageReceived(UdpReceiveResult message)
        {
            if (message.RemoteEndPoint.Port == 0)
            {
                return;
            }
            var bytes = message.Buffer;

            OnMessageReceived(new UdpMessageReceivedEventArgs
            {
                Bytes = bytes,
                RemoteEndPoint = message.RemoteEndPoint.ToString()
            });
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
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            if (_udpClient != null)
            {
                _udpClient.Close();
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                Stop();
            }
        }

        /// <summary>
        /// Sends the async.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="port">The port.</param>
        /// <returns>Task{System.Int32}.</returns>
        /// <exception cref="System.ArgumentNullException">data</exception>
        public Task SendAsync(string data, string ipAddress, int port)
        {
            return SendAsync(Encoding.UTF8.GetBytes(data), ipAddress, port);
        }

        /// <summary>
        /// Sends the async.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="port">The port.</param>
        /// <returns>Task{System.Int32}.</returns>
        /// <exception cref="System.ArgumentNullException">bytes</exception>
        public Task SendAsync(byte[] bytes, string ipAddress, int port)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (string.IsNullOrEmpty(ipAddress))
            {
                throw new ArgumentNullException("ipAddress");
            }

            return _udpClient.SendAsync(bytes, bytes.Length, ipAddress, port);
        }

        /// <summary>
        /// Sends the async.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <returns>Task.</returns>
        public async Task SendAsync(byte[] bytes, string remoteEndPoint)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (string.IsNullOrEmpty(remoteEndPoint))
            {
                throw new ArgumentNullException("remoteEndPoint");
            }

            await _udpClient.SendAsync(bytes, bytes.Length, new NetworkManager().Parse(remoteEndPoint)).ConfigureAwait(false);

            Logger.Info("Udp message sent to {0}", remoteEndPoint);
        }
    }

}
