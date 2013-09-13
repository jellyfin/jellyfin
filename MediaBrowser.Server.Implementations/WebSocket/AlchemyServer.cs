using Alchemy;
using Alchemy.Classes;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using System;
using System.Net;
using System.Net.Sockets;

namespace MediaBrowser.Server.Implementations.WebSocket
{
    /// <summary>
    /// Class AlchemyServer
    /// </summary>
    public class AlchemyServer : IWebSocketServer
    {
        /// <summary>
        /// Occurs when [web socket connected].
        /// </summary>
        public event EventHandler<WebSocketConnectEventArgs> WebSocketConnected;

        /// <summary>
        /// Gets or sets the web socket server.
        /// </summary>
        /// <value>The web socket server.</value>
        private WebSocketServer WebSocketServer { get; set; }

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlchemyServer" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentNullException">logger</exception>
        public AlchemyServer(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            _logger = logger;
        }

        /// <summary>
        /// Gets the port.
        /// </summary>
        /// <value>The port.</value>
        public int Port { get; private set; }

        /// <summary>
        /// Starts the specified port number.
        /// </summary>
        /// <param name="portNumber">The port number.</param>
        public void Start(int portNumber)
        {
            WebSocketServer = new WebSocketServer(portNumber, IPAddress.Any)
            {
                OnConnected = OnAlchemyWebSocketClientConnected,
                TimeOut = TimeSpan.FromHours(12)
            };

            try
            {
                WebSocketServer.Start();
            }
            catch (SocketException ex)
            {
                _logger.ErrorException("The web socket server is unable to start on port {0} due to a Socket error. This can occasionally happen when the operating system takes longer than usual to release the IP bindings from the previous session. This can take up to five minutes. Please try waiting or rebooting the system.", ex, portNumber);

                throw;
            }

            Port = portNumber;

            _logger.Info("Alchemy Web Socket Server started");
        }

        /// <summary>
        /// Called when [alchemy web socket client connected].
        /// </summary>
        /// <param name="context">The context.</param>
        private void OnAlchemyWebSocketClientConnected(UserContext context)
        {
            if (WebSocketConnected != null)
            {
                var socket = new AlchemyWebSocket(context, _logger);

                WebSocketConnected(this, new WebSocketConnectEventArgs
                {
                    WebSocket = socket,
                    Endpoint = context.ClientAddress.ToString()
                });
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            if (WebSocketServer != null)
            {
                WebSocketServer.Stop();
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
            if (WebSocketServer != null)
            {
                WebSocketServer.Dispose();
            }
        }
    }
}
