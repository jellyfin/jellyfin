using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.ApiClient;
using Microsoft.Extensions.Logging;
using NetworkCollection.Udp;

namespace Jellyfin.Networking.Advertising
{
    /// <summary>
    /// Class WhoIsJellyfinServer.
    /// </summary>
    public class WhoIsJellyfinServer
    {
        /// <summary>
        /// The port of the UDP server.
        /// </summary>
        public const int PortNumber = 7359;

        private readonly IServerApplicationHost _appHost;
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly List<UdpProcess>? _udpProcess;

        /// <summary>
        /// Initializes a new instance of the <see cref="WhoIsJellyfinServer"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance.</param>
        /// <param name="appHost">The <see cref="IServerApplicationHost"/> instance.</param>
        /// <param name="networkManager">The <see cref="INetworkManager"/> instace.</param>
        /// <param name="configurationManager">The <see cref="IServerConfigurationManager"/> instance.</param>
        public WhoIsJellyfinServer(
            ILogger logger,
            IServerApplicationHost appHost,
            INetworkManager networkManager,
            IServerConfigurationManager configurationManager)
        {
            _logger = logger ?? throw new NullReferenceException(nameof(logger));
            _appHost = appHost ?? throw new NullReferenceException(nameof(appHost));
            _config = configurationManager ?? throw new NullReferenceException(nameof(configurationManager));

            if (_config.Configuration.AutoDiscovery)
            {
                _udpProcess = UdpHelper.CreateMulticastClients(
                    PortNumber,
                    networkManager.GetAllBindInterfaces(true),
                    ProcessMessage,
                    _logger,
                    enableTracing: _config.Configuration.AutoDiscoveryTracing);

                if (_udpProcess.Count == 0)
                {
                    _logger.LogWarning("Unable to start AutoDiscovery listener on UDP port {PortNumber}", PortNumber);
                }
                else
                {
                    _logger.LogDebug("Starting auto discovery.");
                }
            }
        }

        private async Task ProcessMessage(UdpProcess client, string data, IPEndPoint receivedFrom)
        {
            if (data.Contains("who is JellyfinServer?", StringComparison.OrdinalIgnoreCase))
            {
                var response = new ServerDiscoveryInfo
                {
                    Address = _appHost.GetSmartApiUrl(receivedFrom.Address),
                    Id = _appHost.SystemId,
                    Name = _appHost.FriendlyName
                };
                string reply = JsonSerializer.Serialize(response);

                try
                {
                    await UdpHelper.SendUnicast(client, reply, receivedFrom).ConfigureAwait(false);
                }
                catch (SocketException ex)
                {
                    _logger.LogError(ex, "Error sending response to {0}->{1}", client.LocalEndPoint.Address, receivedFrom.Address);
                }
            }
        }
    }
}
