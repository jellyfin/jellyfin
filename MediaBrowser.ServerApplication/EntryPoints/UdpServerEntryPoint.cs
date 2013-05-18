using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations.Udp;
using System.Net.Sockets;

namespace MediaBrowser.ServerApplication.EntryPoints
{
    public class UdpServerEntryPoint : IServerEntryPoint
    {
        /// <summary>
        /// Gets or sets the UDP server.
        /// </summary>
        /// <value>The UDP server.</value>
        private UdpServer UdpServer { get; set; }

        private readonly ILogger _logger;
        private readonly INetworkManager _networkManager;
        private readonly IServerConfigurationManager _serverConfigurationManager;

        public UdpServerEntryPoint(ILogger logger, INetworkManager networkManager, IServerConfigurationManager serverConfigurationManager)
        {
            _logger = logger;
            _networkManager = networkManager;
            _serverConfigurationManager = serverConfigurationManager;
        }
        
        public void Run()
        {
            var udpServer = new UdpServer(_logger, _networkManager, _serverConfigurationManager);

            try
            {
                udpServer.Start(ApplicationHost.UdpServerPort);

                UdpServer = udpServer;
            }
            catch (SocketException ex)
            {
                _logger.ErrorException("Failed to start UDP Server", ex);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

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
