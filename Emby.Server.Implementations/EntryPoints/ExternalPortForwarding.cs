using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Events;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Threading;
using Open.Nat;
using System.Threading;

namespace Emby.Server.Implementations.EntryPoints
{
    public class ExternalPortForwarding : IServerEntryPoint
    {
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IServerConfigurationManager _config;
        private readonly IDeviceDiscovery _deviceDiscovery;
        private NatDiscoverer _natDiscoverer;
        private bool _disposed = false;

        public ExternalPortForwarding(ILoggerFactory logFactory, IServerApplicationHost appHost, IServerConfigurationManager config, IHttpClient httpClient, ITimerFactory timerFactory)
        {
            _appHost = appHost;
            _config = config;
            _httpClient = httpClient;
            _logger = logFactory.CreateLogger("PortMapper");
            _natDiscoverer = new NatDiscoverer();
        }

        public void Run()
        {
            if (_config.Configuration.EnableUPnP && _config.Configuration.EnableRemoteAccess)
            {
                var _ = Task.Run(async () => { await Start().ConfigureAwait(false); });
            }
        }

        private async Task Start()
        {
            _logger.LogDebug("Starting NAT discovery");

            var cts = new CancellationTokenSource(10 * 1000);
            IEnumerable<NatDevice> devices = await _natDiscoverer.DiscoverDevicesAsync(PortMapper.Upnp, cts);
            foreach (NatDevice device in devices)
            {
                IPAddress ip = await device.GetExternalIPAsync().ConfigureAwait(false);
                _logger.LogDebug("Found NAT device: {Ip}", ip);

                await device.CreatePortMapAsync(
                        new Mapping(Protocol.Tcp, _appHost.HttpPort, _config.Configuration.PublicPort, _appHost.Name)
                    ).ConfigureAwait(false);

                await device.CreatePortMapAsync(
                        new Mapping(Protocol.Tcp, _appHost.HttpsPort, _config.Configuration.PublicHttpsPort, _appHost.Name)
                    ).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
        }
    }
}
