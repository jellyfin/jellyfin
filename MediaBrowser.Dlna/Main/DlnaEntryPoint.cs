using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Dlna.PlayTo;
using MediaBrowser.Dlna.Ssdp;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Dlna.Channels;

namespace MediaBrowser.Dlna.Main
{
    public class DlnaEntryPoint : IServerEntryPoint
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly IServerApplicationHost _appHost;
        private readonly INetworkManager _network;

        private PlayToManager _manager;
        private readonly ISessionManager _sessionManager;
        private readonly IHttpClient _httpClient;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDlnaManager _dlnaManager;
        private readonly IImageProcessor _imageProcessor;
        private readonly IUserDataManager _userDataManager;
        private readonly ILocalizationManager _localization;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IMediaEncoder _mediaEncoder;

        private readonly SsdpHandler _ssdpHandler;
        private readonly IDeviceDiscovery _deviceDiscovery;

        private readonly List<string> _registeredServerIds = new List<string>();
        private bool _ssdpHandlerStarted;
        private bool _dlnaServerStarted;

        public DlnaEntryPoint(IServerConfigurationManager config,
            ILogManager logManager,
            IServerApplicationHost appHost,
            INetworkManager network,
            ISessionManager sessionManager,
            IHttpClient httpClient,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IDlnaManager dlnaManager,
            IImageProcessor imageProcessor,
            IUserDataManager userDataManager,
            ILocalizationManager localization,
            IMediaSourceManager mediaSourceManager,
            ISsdpHandler ssdpHandler, IDeviceDiscovery deviceDiscovery, IMediaEncoder mediaEncoder)
        {
            _config = config;
            _appHost = appHost;
            _network = network;
            _sessionManager = sessionManager;
            _httpClient = httpClient;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dlnaManager = dlnaManager;
            _imageProcessor = imageProcessor;
            _userDataManager = userDataManager;
            _localization = localization;
            _mediaSourceManager = mediaSourceManager;
            _deviceDiscovery = deviceDiscovery;
            _mediaEncoder = mediaEncoder;
            _ssdpHandler = (SsdpHandler)ssdpHandler;
            _logger = logManager.GetLogger("Dlna");
        }

        public void Run()
        {
            ReloadComponents();

            _config.ConfigurationUpdated += _config_ConfigurationUpdated;
            _config.NamedConfigurationUpdated += _config_NamedConfigurationUpdated;
        }

        private bool _lastEnableUpnP;
        void _config_ConfigurationUpdated(object sender, EventArgs e)
        {
            if (_lastEnableUpnP != _config.Configuration.EnableUPnP)
            {
                ReloadComponents();
            }
            _lastEnableUpnP = _config.Configuration.EnableUPnP;
        }

        void _config_NamedConfigurationUpdated(object sender, ConfigurationUpdateEventArgs e)
        {
            if (string.Equals(e.Key, "dlna", StringComparison.OrdinalIgnoreCase))
            {
                ReloadComponents();
            }
        }

        private async void ReloadComponents()
        {
            var options = _config.GetDlnaConfiguration();

            if (!options.EnableServer && !options.EnablePlayTo && !_config.Configuration.EnableUPnP)
            {
                if (_ssdpHandlerStarted)
                {
                    // Sat/ip live tv depends on device discovery, as well as hd homerun detection
                    // In order to allow this to be disabled, we need a modular way of knowing if there are 
                    // any parts of the system that are dependant on it
                    // DisposeSsdpHandler();
                }
                return;
            }

            if (!_ssdpHandlerStarted)
            {
                StartSsdpHandler();
            }

            var isServerStarted = _dlnaServerStarted;

            if (options.EnableServer && !isServerStarted)
            {
                await StartDlnaServer().ConfigureAwait(false);
            }
            else if (!options.EnableServer && isServerStarted)
            {
                DisposeDlnaServer();
            }

            var isPlayToStarted = _manager != null;

            if (options.EnablePlayTo && !isPlayToStarted)
            {
                StartPlayToManager();
            }
            else if (!options.EnablePlayTo && isPlayToStarted)
            {
                DisposePlayToManager();
            }
        }

        private void StartSsdpHandler()
        {
            try
            {
                _ssdpHandler.Start();
                _ssdpHandlerStarted = true;

                StartDeviceDiscovery();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error starting ssdp handlers", ex);
            }
        }

        private void StartDeviceDiscovery()
        {
            try
            {
                ((DeviceDiscovery)_deviceDiscovery).Start(_ssdpHandler);

                //DlnaChannel.Current.Start(() => _registeredServerIds.ToList());
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error starting device discovery", ex);
            }
        }

        private void DisposeDeviceDiscovery()
        {
            try
            {
                ((DeviceDiscovery)_deviceDiscovery).Dispose();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error stopping device discovery", ex);
            }
        }

        private void DisposeSsdpHandler()
        {
            DisposeDeviceDiscovery();

            try
            {
                ((DeviceDiscovery)_deviceDiscovery).Dispose();

                _ssdpHandler.Dispose();

                _ssdpHandlerStarted = false;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error stopping ssdp handlers", ex);
            }
        }

        public async Task StartDlnaServer()
        {
            try
            {
                await RegisterServerEndpoints().ConfigureAwait(false);

                _dlnaServerStarted = true;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error registering endpoint", ex);
            }
        }

        private async Task RegisterServerEndpoints()
        {
            foreach (var address in await _appHost.GetLocalIpAddresses().ConfigureAwait(false))
            {
                //if (IPAddress.IsLoopback(address))
                //{
                //    // Should we allow this?
                //    continue;
                //}

                var addressString = address.ToString();
                var udn = addressString.GetMD5().ToString("N");

                var descriptorURI = "/dlna/" + udn + "/description.xml";

                var uri = new Uri(_appHost.GetLocalApiUrl(address) + descriptorURI);

                var services = new List<string>
                {
                    "upnp:rootdevice", 
                    "urn:schemas-upnp-org:device:MediaServer:1", 
                    "urn:schemas-upnp-org:service:ContentDirectory:1", 
                    "urn:schemas-upnp-org:service:ConnectionManager:1",
                    "urn:microsoft.com:service:X_MS_MediaReceiverRegistrar:1",
                    "uuid:" + udn
                };

                _ssdpHandler.RegisterNotification(udn, uri, address, services);

                _registeredServerIds.Add(udn);
            }
        }

        private readonly object _syncLock = new object();
        private void StartPlayToManager()
        {
            lock (_syncLock)
            {
                try
                {
                    _manager = new PlayToManager(_logger,
                        _sessionManager,
                        _libraryManager,
                        _userManager,
                        _dlnaManager,
                        _appHost,
                        _imageProcessor,
                        _deviceDiscovery,
                        _httpClient,
                        _config,
                        _userDataManager,
                        _localization,
                        _mediaSourceManager,
                        _mediaEncoder);

                    _manager.Start();
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error starting PlayTo manager", ex);
                }
            }
        }

        private void DisposePlayToManager()
        {
            lock (_syncLock)
            {
                if (_manager != null)
                {
                    try
                    {
                        _manager.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error disposing PlayTo manager", ex);
                    }
                    _manager = null;
                }
            }
        }

        public void Dispose()
        {
            DisposeDlnaServer();
            DisposePlayToManager();
            DisposeSsdpHandler();
        }

        public void DisposeDlnaServer()
        {
            foreach (var id in _registeredServerIds)
            {
                try
                {
                    _ssdpHandler.UnregisterNotification(id);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error unregistering server", ex);
                }
            }

            _registeredServerIds.Clear();

            _dlnaServerStarted = false;
        }
    }
}
