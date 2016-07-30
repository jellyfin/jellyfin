using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using Mono.Nat;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using MediaBrowser.Common.Threading;

namespace MediaBrowser.Server.Implementations.EntryPoints
{
    public class ExternalPortForwarding : IServerEntryPoint
    {
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly ISsdpHandler _ssdp;

        private PeriodicTimer _timer;
        private bool _isStarted;

        public ExternalPortForwarding(ILogManager logmanager, IServerApplicationHost appHost, IServerConfigurationManager config, ISsdpHandler ssdp)
        {
            _logger = logmanager.GetLogger("PortMapper");
            _appHost = appHost;
            _config = config;
            _ssdp = ssdp;
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
            //NatUtility.Logger = new LogWriter(_logger);

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


            // it is hard to say what one should do when an unhandled exception is raised
            // because there isn't anything one can do about it. Probably save a log or ignored it.
            NatUtility.UnhandledException += NatUtility_UnhandledException;
            NatUtility.StartDiscovery();

            _timer = new PeriodicTimer(ClearCreatedRules, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            _ssdp.MessageReceived += _ssdp_MessageReceived;

            _lastConfigIdentifier = GetConfigIdentifier();

            _isStarted = true;
        }

        private void ClearCreatedRules(object state)
        {
            _createdRules = new List<string>();
            _usnsHandled = new List<string>();
        }

        void _ssdp_MessageReceived(object sender, SsdpMessageEventArgs e)
        {
            var endpoint = e.EndPoint as IPEndPoint;

            if (endpoint == null || e.LocalEndPoint == null)
            {
                return;
            }

            string usn;
            if (!e.Headers.TryGetValue("USN", out usn)) usn = string.Empty;

            string nt;
            if (!e.Headers.TryGetValue("NT", out nt)) nt = string.Empty;

            // Filter device type
            if (usn.IndexOf("WANIPConnection:", StringComparison.OrdinalIgnoreCase) == -1 &&
                     nt.IndexOf("WANIPConnection:", StringComparison.OrdinalIgnoreCase) == -1 &&
                     usn.IndexOf("WANPPPConnection:", StringComparison.OrdinalIgnoreCase) == -1 &&
                     nt.IndexOf("WANPPPConnection:", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return;
            }

            var identifier = string.IsNullOrWhiteSpace(usn) ? nt : usn;

            if (!_usnsHandled.Contains(identifier))
            {
                _usnsHandled.Add(identifier);

                _logger.Debug("Calling Nat.Handle on " + identifier);
                NatUtility.Handle(e.LocalEndPoint.Address, e.Message, endpoint, NatProtocol.Upnp);
            }
        }

        void NatUtility_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;

            if (ex == null)
            {
                //_logger.Error("Unidentified error reported by Mono.Nat");
            }
            else
            {
                // Seeing some blank exceptions coming through here
                //_logger.ErrorException("Error reported by Mono.Nat: ", ex);
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
            catch (Exception ex)
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

        private void CreatePortMap(INatDevice device, int privatePort, int publicPort)
        {
            _logger.Debug("Creating port map on port {0}", privatePort);
            device.CreatePortMap(new Mapping(Protocol.Tcp, privatePort, publicPort)
            {
                Description = _appHost.Name
            });
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

            _ssdp.MessageReceived -= _ssdp_MessageReceived;

            try
            {
                // This is not a significant improvement
                NatUtility.StopDiscovery();
                NatUtility.DeviceFound -= NatUtility_DeviceFound;
                NatUtility.DeviceLost -= NatUtility_DeviceLost;
                NatUtility.UnhandledException -= NatUtility_UnhandledException;
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