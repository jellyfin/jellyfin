using System;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using Emby.Server.Implementations.Udp;
using MediaBrowser.Model.Net;

namespace Emby.Server.Implementations.EntryPoints
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
        private readonly ISocketFactory _socketFactory;
        private readonly IServerApplicationHost _appHost;
        private readonly IJsonSerializer _json;

        public const int PortNumber = 7359;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpServerEntryPoint" /> class.
        /// </summary>
        public UdpServerEntryPoint(ILogger logger, IServerApplicationHost appHost, IJsonSerializer json, ISocketFactory socketFactory)
        {
            _logger = logger;
            _appHost = appHost;
            _json = json;
            _socketFactory = socketFactory;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public void Run()
        {
            var udpServer = new UdpServer(_logger, _appHost, _json, _socketFactory);

            try
            {
                udpServer.Start(PortNumber);

                UdpServer = udpServer;
            }
            catch (Exception ex)
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
