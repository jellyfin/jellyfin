using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using MediaBrowser.Common.Threading;
using Open.Nat;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.EntryPoints
{
    public class ExternalPortForwarding : IServerEntryPoint
    {
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly ISsdpHandler _ssdp;
        private CancellationTokenSource _currentCancellationTokenSource;
        private TimeSpan _interval = TimeSpan.FromHours(1);

        public ExternalPortForwarding(ILogManager logmanager, IServerApplicationHost appHost, IServerConfigurationManager config, ISsdpHandler ssdp)
        {
            _logger = logmanager.GetLogger("PortMapper");
            _appHost = appHost;
            _config = config;
            _ssdp = ssdp;
        }

        public void Run()
        {
            //NatUtility.Logger = new LogWriter(_logger);

            if (_config.Configuration.EnableUPnP)
            {
                Discover();
            }
        }

        private async void Discover()
        {
            var discoverer = new NatDiscoverer();

            var cancellationTokenSource = new CancellationTokenSource(10000);
            _currentCancellationTokenSource = cancellationTokenSource;

            try
            {
                var device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cancellationTokenSource).ConfigureAwait(false);

                await CreateRules(device).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error discovering NAT devices", ex);
            }
            finally
            {
                _currentCancellationTokenSource = null;
            }

            if (_config.Configuration.EnableUPnP)
            {
                await Task.Delay(_interval).ConfigureAwait(false);
                Discover();
            }
        }

        private async Task CreateRules(NatDevice device)
        {
            // On some systems the device discovered event seems to fire repeatedly
            // This check will help ensure we're not trying to port map the same device over and over

            await CreatePortMap(device, _appHost.HttpPort, _config.Configuration.PublicPort).ConfigureAwait(false);
            await CreatePortMap(device, _appHost.HttpsPort, _config.Configuration.PublicHttpsPort).ConfigureAwait(false);
        }

        private async Task CreatePortMap(NatDevice device, int privatePort, int publicPort)
        {
            _logger.Debug("Creating port map on port {0}", privatePort);

            try
            {
                await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, privatePort, publicPort, _appHost.Name)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error creating port map", ex);
            }
        }

        public void Dispose()
        {
            DisposeNat();
        }

        private void DisposeNat()
        {
            if (_currentCancellationTokenSource != null)
            {
                try
                {
                    _currentCancellationTokenSource.Cancel();
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error calling _currentCancellationTokenSource.Cancel", ex);
                }
            }
        }
    }
}
