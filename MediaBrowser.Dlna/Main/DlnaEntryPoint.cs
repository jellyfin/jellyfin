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
using System.Net;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding;
using Rssdp;
using Rssdp.Infrastructure;

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

        private readonly IDeviceDiscovery _deviceDiscovery;

        private bool _ssdpHandlerStarted;
        private bool _dlnaServerStarted;
        private SsdpDevicePublisher _Publisher;

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
            IDeviceDiscovery deviceDiscovery, IMediaEncoder mediaEncoder)
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
            _logger = logManager.GetLogger("Dlna");
        }

        public void Run()
        {
            ((DlnaManager)_dlnaManager).InitProfiles();

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
                StartPublishing();
                _ssdpHandlerStarted = true;

                StartDeviceDiscovery();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error starting ssdp handlers", ex);
            }
        }

        private void LogMessage(string msg)
        {
            //_logger.Debug(msg);
        }

        private void StartPublishing()
        {
            SsdpDevicePublisherBase.LogFunction = LogMessage;
            _Publisher = new SsdpDevicePublisher();
        }

        private void StartDeviceDiscovery()
        {
            try
            {
                ((DeviceDiscovery)_deviceDiscovery).Start();
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
            if (!_config.GetDlnaConfiguration().BlastAliveMessages)
            {
                return;
            }

            var cacheLength = _config.GetDlnaConfiguration().BlastAliveMessageIntervalSeconds;
            _Publisher.SupportPnpRootDevice = false;

            var addresses = (await _appHost.GetLocalIpAddresses().ConfigureAwait(false)).ToList();

            foreach (var address in addresses)
            {
                //if (IPAddress.IsLoopback(address))
                //{
                //    // Should we allow this?
                //    continue;
                //}

                var addressString = address.ToString();

                var udn = CreateUuid(addressString);

                var fullService = "urn:schemas-upnp-org:device:MediaServer:1";

                _logger.Info("Registering publisher for {0} on {1}", fullService, addressString);

                var descriptorUri = "/dlna/" + udn + "/description.xml";
                var uri = new Uri(_appHost.GetLocalApiUrl(address) + descriptorUri);

                var device = new SsdpRootDevice
                {
                    CacheLifetime = TimeSpan.FromSeconds(cacheLength), //How long SSDP clients can cache this info.
                    Location = uri, // Must point to the URL that serves your devices UPnP description document. 
                    FriendlyName = "Emby Server",
                    Manufacturer = "Emby",
                    ModelName = "Emby Server",
                    Uuid = udn
                    // This must be a globally unique value that survives reboots etc. Get from storage or embedded hardware etc.                
                };

                SetProperies(device, fullService);
                _Publisher.AddDevice(device);

                var embeddedDevices = new List<string>
                {
                    "urn:schemas-upnp-org:service:ContentDirectory:1",
                    "urn:schemas-upnp-org:service:ConnectionManager:1",
                    "urn:microsoft.com:service:X_MS_MediaReceiverRegistrar:1"
                };

                foreach (var subDevice in embeddedDevices)
                {
                    var embeddedDevice = new SsdpEmbeddedDevice
                    {
                        FriendlyName = device.FriendlyName,
                        Manufacturer = device.Manufacturer,
                        ModelName = device.ModelName,
                        Uuid = udn
                        // This must be a globally unique value that survives reboots etc. Get from storage or embedded hardware etc.                
                    };

                    SetProperies(embeddedDevice, subDevice);
                    device.AddDevice(embeddedDevice);
                }
            }
        }

        private string CreateUuid(string text)
        {
            return text.GetMD5().ToString("N");
        }

        private void SetProperies(SsdpDevice device, string fullDeviceType)
        {
            var service = fullDeviceType.Replace("urn:", string.Empty).Replace(":1", string.Empty);

            var serviceParts = service.Split(':');

            var deviceTypeNamespace = serviceParts[0].Replace('.', '-');

            device.DeviceTypeNamespace = deviceTypeNamespace;
            device.DeviceClass = serviceParts[1];
            device.DeviceType = serviceParts[2];
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
            if (_Publisher != null)
            {
                var devices = _Publisher.Devices.ToList();
                var tasks = devices.Select(i => _Publisher.RemoveDevice(i)).ToArray();

                Task.WaitAll(tasks);
                //foreach (var device in devices)
                //{
                //    try
                //    {
                //        _Publisher.RemoveDevice(device);
                //    }
                //    catch (Exception ex)
                //    {
                //        _logger.ErrorException("Error sending bye bye", ex);
                //    }
                //}
                _Publisher.Dispose();
            }

            _dlnaServerStarted = false;
        }
    }
}
