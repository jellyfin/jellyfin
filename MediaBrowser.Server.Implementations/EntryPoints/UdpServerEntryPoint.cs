using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations.Udp;
using System.Net.Sockets;

namespace MediaBrowser.Server.Implementations.EntryPoints
{
    /// <summary>
    /// Class UdpServerEntryPoint
    /// </summary>
    public class UdpServerEntryPoint : IServerEntryPoint
    {
        /// <summary>
        /// Gets or sets the UDP server.
        /// </summary>
        /// <value>The UDP server.</value>
        private UdpServer UdpServer { get; set; }

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;
        /// <summary>
        /// The _network manager
        /// </summary>
        private readonly INetworkManager _networkManager;
        /// <summary>
        /// The _server configuration manager
        /// </summary>
        private readonly IServerConfigurationManager _serverConfigurationManager;
        /// <summary>
        /// The _HTTP server
        /// </summary>
        private readonly IHttpServer _httpServer;
        private readonly IServerApplicationHost _appHost;

        public const int PortNumber = 7359;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpServerEntryPoint"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="networkManager">The network manager.</param>
        /// <param name="serverConfigurationManager">The server configuration manager.</param>
        /// <param name="httpServer">The HTTP server.</param>
        public UdpServerEntryPoint(ILogger logger, INetworkManager networkManager, IServerConfigurationManager serverConfigurationManager, IHttpServer httpServer, IServerApplicationHost appHost)
        {
            _logger = logger;
            _networkManager = networkManager;
            _serverConfigurationManager = serverConfigurationManager;
            _httpServer = httpServer;
            _appHost = appHost;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public void Run()
        {
            var udpServer = new UdpServer(_logger, _networkManager, _serverConfigurationManager, _httpServer, _appHost);

            try
            {
                udpServer.Start(PortNumber);

                UdpServer = udpServer;
            }
            catch (SocketException ex)
            {
                _logger.ErrorException("Failed to start UDP Server", ex);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                if (UdpServer != null)
                {
                    UdpServer.Dispose();
                }
            }
        }
    }
}
