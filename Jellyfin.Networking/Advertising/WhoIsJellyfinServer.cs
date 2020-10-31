using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Networking.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Udp;
using MediaBrowser.Controller;
using MediaBrowser.Model.ApiClient;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger _logger;
        private readonly List<UdpProcess>? _udpProcess;

        /// <summary>
        /// Initializes a new instance of the <see cref="WhoIsJellyfinServer"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance.</param>
        /// <param name="appHost">The <see cref="IServerApplicationHost"/> instance.</param>
        /// <param name="networkManager">The <see cref="INetworkManager"/> instace.</param>
        /// <param name="configurationManager">The <see cref="IConfigurationManager"/> instance.</param>
        public WhoIsJellyfinServer(
            ILogger logger,
            IServerApplicationHost appHost,
            INetworkManager networkManager,
            IConfigurationManager configurationManager)
        {
            _logger = logger ?? throw new NullReferenceException(nameof(logger));
            _appHost = appHost ?? throw new NullReferenceException(nameof(appHost));
            if (networkManager == null)
            {
                throw new NullReferenceException(nameof(networkManager));
            }

            var config = configurationManager?.GetNetworkConfiguration() ?? throw new NullReferenceException(nameof(configurationManager));
            if (config.AutoDiscovery)
            {
                _udpProcess = UdpHelper.CreateMulticastClients(
                    PortNumber,
                    networkManager.GetAllBindInterfaces(true),
                    ProcessMessage,
                    _logger,
                    enableTracing: config.AutoDiscoveryTracing);

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
