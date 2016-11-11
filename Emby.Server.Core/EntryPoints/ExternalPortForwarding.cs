using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Threading;
using Mono.Nat;

namespace Emby.Server.Core.EntryPoints
{
    public class ExternalPortForwarding : IServerEntryPoint
    {
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IServerConfigurationManager _config;
        private readonly IDeviceDiscovery _deviceDiscovery;

        private ITimer _timer;
        private bool _isStarted;
        private readonly ITimerFactory _timerFactory;

        public ExternalPortForwarding(ILogManager logmanager, IServerApplicationHost appHost, IServerConfigurationManager config, IDeviceDiscovery deviceDiscovery, IHttpClient httpClient, ITimerFactory timerFactory)
        {
            _logger = logmanager.GetLogger("PortMapper");
            _appHost = appHost;
            _config = config;
            _deviceDiscovery = deviceDiscovery;
            _httpClient = httpClient;
            _timerFactory = timerFactory;
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
            values.Add(config.EnableHttps.ToString());
            values.Add(_appHost.EnableHttps.ToString());

            return string.Join("|", values.ToArray());
        }

        void _config_ConfigurationUpdated(object sender, EventArgs e)
        {
            if (!string.Equals(_lastConfigIdentifier, GetConfigIdentifier(), StringComparison.OrdinalIgnoreCase))
            {
                if (_isStarted)
                {
                    DisposeNat();
                }

                Run();
            }
        }

        public void Run()
        {
            NatUtility.Logger = _logger;
            NatUtility.HttpClient = _httpClient;

            if (_config.Configuration.EnableUPnP)
            {
                Start();
            }

            _config.ConfigurationUpdated -= _config_ConfigurationUpdated;
            _config.ConfigurationUpdated += _config_ConfigurationUpdated;
        }

        private void Start()
        {
            _logger.Debug("Starting NAT discovery");
            NatUtility.EnabledProtocols = new List<NatProtocol>
            {
                NatProtocol.Pmp
            };
            NatUtility.DeviceFound += NatUtility_DeviceFound;

            // Mono.Nat does never rise this event. The event is there however it is useless. 
            // You could remove it with no risk. 
            NatUtility.DeviceLost += NatUtility_DeviceLost;


            NatUtility.StartDiscovery();

            _timer = _timerFactory.Create(ClearCreatedRules, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            _deviceDiscovery.DeviceDiscovered += _deviceDiscovery_DeviceDiscovered;

            _lastConfigIdentifier = GetConfigIdentifier();

            _isStarted = true;
        }

        private async void _deviceDiscovery_DeviceDiscovered(object sender, GenericEventArgs<UpnpDeviceInfo> e)
        {
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

            _logger.Debug("Found NAT device: " + identifier);

            IPAddress address;
            if (IPAddress.TryParse(info.Location.Host, out address))
            {
                // The Handle method doesn't need the port
                var endpoint = new IPEndPoint(address, info.Location.Port);

                IPAddress localAddress = null;

                try
                {
                    var localAddressString = await _appHost.GetLocalApiUrl().ConfigureAwait(false);

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
                    return;
                }

                _logger.Debug("Calling Nat.Handle on " + identifier);
                NatUtility.Handle(localAddress, info, endpoint, NatProtocol.Upnp);
            }
        }

        private void ClearCreatedRules(object state)
        {
            _createdRules = new List<string>();
            lock (_usnsHandled)
            {
                _usnsHandled.Clear();
            }
        }

        void NatUtility_DeviceFound(object sender, DeviceEventArgs e)
        {
            try
            {
                var device = e.Device;
                _logger.Debug("NAT device found: {0}", device.LocalAddress.ToString());

                CreateRules(device);
            }
            catch
            {
                // I think it could be a good idea to log the exception because 
                //   you are using permanent portmapping here (never expire) and that means that next time
                //   CreatePortMap is invoked it can fails with a 718-ConflictInMappingEntry or not. That depends
                //   on the router's upnp implementation (specs says it should fail however some routers don't do it)
                //   It also can fail with others like 727-ExternalPortOnlySupportsWildcard, 728-NoPortMapsAvailable
                // and those errors (upnp errors) could be useful for diagnosting.  

                // Commenting out because users are reporting problems out of our control
                //_logger.ErrorException("Error creating port forwarding rules", ex);
            }
        }

        private List<string> _createdRules = new List<string>();
        private List<string> _usnsHandled = new List<string>();
        private void CreateRules(INatDevice device)
        {
            // On some systems the device discovered event seems to fire repeatedly
            // This check will help ensure we're not trying to port map the same device over and over

            var address = device.LocalAddress.ToString();

            if (!_createdRules.Contains(address))
            {
                _createdRules.Add(address);

                CreatePortMap(device, _appHost.HttpPort, _config.Configuration.PublicPort);
                CreatePortMap(device, _appHost.HttpsPort, _config.Configuration.PublicHttpsPort);
            }
        }

        private async void CreatePortMap(INatDevice device, int privatePort, int publicPort)
        {
            _logger.Debug("Creating port map on port {0}", privatePort);

            try
            {
                await device.CreatePortMap(new Mapping(Protocol.Tcp, privatePort, publicPort)
                {
                    Description = _appHost.Name
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error creating port map", ex);
            }
        }

        // As I said before, this method will be never invoked. You can remove it.
        void NatUtility_DeviceLost(object sender, DeviceEventArgs e)
        {
            var device = e.Device;
            _logger.Debug("NAT device lost: {0}", device.LocalAddress.ToString());
        }

        public void Dispose()
        {
            DisposeNat();
        }

        private void DisposeNat()
        {
            _logger.Debug("Stopping NAT discovery");

            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }

            _deviceDiscovery.DeviceDiscovered -= _deviceDiscovery_DeviceDiscovered;

            try
            {
                // This is not a significant improvement
                NatUtility.StopDiscovery();
                NatUtility.DeviceFound -= NatUtility_DeviceFound;
                NatUtility.DeviceLost -= NatUtility_DeviceLost;
            }
            // Statements in try-block will no fail because StopDiscovery is a one-line 
            // method that was no chances to fail.
            //		public static void StopDiscovery ()
            //      {
            //          searching.Reset();
            //      }
            // IMO you could remove the catch-block
            catch (Exception ex)
            {
                _logger.ErrorException("Error stopping NAT Discovery", ex);
            }
            finally
            {
                _isStarted = false;
            }
        }
    }
}