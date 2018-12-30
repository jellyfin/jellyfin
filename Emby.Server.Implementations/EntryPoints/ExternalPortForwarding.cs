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
using Mono.Nat;
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

        private ITimer _timer;
        private readonly ITimerFactory _timerFactory;

        private NatManager _natManager;

        public ExternalPortForwarding(ILoggerFactory loggerFactory, IServerApplicationHost appHost, IServerConfigurationManager config, IDeviceDiscovery deviceDiscovery, IHttpClient httpClient, ITimerFactory timerFactory)
        {
            _logger = loggerFactory.CreateLogger("PortMapper");
            _appHost = appHost;
            _config = config;
            _deviceDiscovery = deviceDiscovery;
            _httpClient = httpClient;
            _timerFactory = timerFactory;
            _config.ConfigurationUpdated += _config_ConfigurationUpdated1;
        }

        private void _config_ConfigurationUpdated1(object sender, EventArgs e)
        {
            _config_ConfigurationUpdated(sender, e);
        }

        private string _lastConfigIdentifier;
        private string GetConfigIdentifier()
        {
            var values = new List<string>();
            var config = _config.Configuration;

            values.Add(config.EnableUPnP.ToString());
            values.Add(config.PublicPort.ToString(CultureInfo.InvariantCulture));
            values.Add(_appHost.HttpPort.ToString(CultureInfo.InvariantCulture));
            values.Add(_appHost.HttpsPort.ToString(CultureInfo.InvariantCulture));
            values.Add(_appHost.EnableHttps.ToString());
            values.Add((config.EnableRemoteAccess).ToString());

            return string.Join("|", values.ToArray());
        }

        void _config_ConfigurationUpdated(object sender, EventArgs e)
        {
            if (!string.Equals(_lastConfigIdentifier, GetConfigIdentifier(), StringComparison.OrdinalIgnoreCase))
            {
                DisposeNat();

                Run();
            }
        }

        public void Run()
        {
            if (_config.Configuration.EnableUPnP && _config.Configuration.EnableRemoteAccess)
            {
                Start();
            }

            _config.ConfigurationUpdated -= _config_ConfigurationUpdated;
            _config.ConfigurationUpdated += _config_ConfigurationUpdated;
        }

        private void Start()
        {
            _logger.LogDebug("Starting NAT discovery");
            if (_natManager == null)
            {
                _natManager = new NatManager(_logger, _httpClient);
                _natManager.DeviceFound += NatUtility_DeviceFound;
                _natManager.StartDiscovery();
            }

            _timer = _timerFactory.Create(ClearCreatedRules, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

            _deviceDiscovery.DeviceDiscovered += _deviceDiscovery_DeviceDiscovered;

            _lastConfigIdentifier = GetConfigIdentifier();
        }

        private async void _deviceDiscovery_DeviceDiscovered(object sender, GenericEventArgs<UpnpDeviceInfo> e)
        {
            if (_disposed)
            {
                return;
            }

            var info = e.Argument;

            string usn;
            if (!info.Headers.TryGetValue("USN", out usn)) usn = string.Empty;

            string nt;
            if (!info.Headers.TryGetValue("NT", out nt)) nt = string.Empty;

            // Filter device type
            if (usn.IndexOf("WANIPConnection:", StringComparison.OrdinalIgnoreCase) == -1 &&
                     nt.IndexOf("WANIPConnection:", StringComparison.OrdinalIgnoreCase) == -1 &&
                     usn.IndexOf("WANPPPConnection:", StringComparison.OrdinalIgnoreCase) == -1 &&
                     nt.IndexOf("WANPPPConnection:", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return;
            }

            var identifier = string.IsNullOrWhiteSpace(usn) ? nt : usn;

            if (info.Location == null)
            {
                return;
            }

            lock (_usnsHandled)
            {
                if (_usnsHandled.Contains(identifier))
                {
                    return;
                }
                _usnsHandled.Add(identifier);
            }

            _logger.LogDebug("Found NAT device: " + identifier);

            IPAddress address;
            if (IPAddress.TryParse(info.Location.Host, out address))
            {
                // The Handle method doesn't need the port
                var endpoint = new IPEndPoint(address, info.Location.Port);

                IPAddress localAddress = null;

                try
                {
                    var localAddressString = await _appHost.GetLocalApiUrl(CancellationToken.None).ConfigureAwait(false);

                    Uri uri;
                    if (Uri.TryCreate(localAddressString, UriKind.Absolute, out uri))
                    {
                        localAddressString = uri.Host;

                        if (!IPAddress.TryParse(localAddressString, out localAddress))
                        {
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error");
                    return;
                }

                if (_disposed)
                {
                    return;
                }

                // This should never happen, but the Handle method will throw ArgumentNullException if it does
                if (localAddress == null)
                {
                    return;
                }

                var natManager = _natManager;
                if (natManager != null)
                {
                    await natManager.Handle(localAddress, info, endpoint, NatProtocol.Upnp).ConfigureAwait(false);
                }
            }
        }

        private void ClearCreatedRules(object state)
        {
            lock (_createdRules)
            {
                _createdRules.Clear();
            }
            lock (_usnsHandled)
            {
                _usnsHandled.Clear();
            }
        }

        void NatUtility_DeviceFound(object sender, DeviceEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                var device = e.Device;

                CreateRules(device);
            }
            catch
            {
                // Commenting out because users are reporting problems out of our control
                //_logger.LogError(ex, "Error creating port forwarding rules");
            }
        }

        private List<string> _createdRules = new List<string>();
        private List<string> _usnsHandled = new List<string>();
        private async void CreateRules(INatDevice device)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("PortMapper");
            }

            // On some systems the device discovered event seems to fire repeatedly
            // This check will help ensure we're not trying to port map the same device over and over
            var address = device.LocalAddress;

            var addressString = address.ToString();

            lock (_createdRules)
            {
                if (!_createdRules.Contains(addressString))
                {
                    _createdRules.Add(addressString);
                }
                else
                {
                    return;
                }
            }

            try
            {
                await CreatePortMap(device, _appHost.HttpPort, _config.Configuration.PublicPort).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating http port map");
                return;
            }

            try
            {
                await CreatePortMap(device, _appHost.HttpsPort, _config.Configuration.PublicHttpsPort).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating https port map");
            }
        }

        private Task CreatePortMap(INatDevice device, int privatePort, int publicPort)
        {
            _logger.LogDebug("Creating port map on local port {0} to public port {1} with device {2}", privatePort, publicPort, device.LocalAddress.ToString());

            return device.CreatePortMap(new Mapping(Protocol.Tcp, privatePort, publicPort)
            {
                Description = _appHost.Name
            });
        }

        private bool _disposed = false;
        public void Dispose()
        {
            _disposed = true;
            DisposeNat();
        }

        private void DisposeNat()
        {
            _logger.LogDebug("Stopping NAT discovery");

            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }

            _deviceDiscovery.DeviceDiscovered -= _deviceDiscovery_DeviceDiscovered;

            var natManager = _natManager;

            if (natManager != null)
            {
                _natManager = null;

                using (natManager)
                {
                    try
                    {
                        natManager.StopDiscovery();
                        natManager.DeviceFound -= NatUtility_DeviceFound;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error stopping NAT Discovery");
                    }
                }
            }
        }
    }
}
