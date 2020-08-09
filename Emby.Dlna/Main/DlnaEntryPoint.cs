#pragma warning disable CS1591

using System;
using System.Globalization;
using System.Threading.Tasks;
using Emby.Dlna.PlayTo;
using Emby.Dlna.Ssdp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Networking;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.System;
using Microsoft.Extensions.Logging;
using Rssdp;
using Rssdp.Infrastructure;

using OperatingSystem = MediaBrowser.Common.System.OperatingSystem;

namespace Emby.Dlna.Main
{
    public class DlnaEntryPoint : IServerEntryPoint, IRunBeforeStartup
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILogger<DlnaEntryPoint> _logger;
        private readonly IServerApplicationHost _appHost;
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
        private readonly INetworkManager _networkManager;
        private readonly object _syncLock = new object();
        private readonly IUserViewManager _userViewManager;
        private readonly ITVSeriesManager _tvSeriesManager;
        private readonly ILocalizationManager _localizationManager;
        private readonly ILoggerFactory _loggerFactory;

        private PlayToManager _manager;
        private SsdpDevicePublisher _publisher;
        private ISsdpCommunicationsServer _communicationsServer;
        private bool _isDisposed;

        public DlnaEntryPoint(
            IServerConfigurationManager config,
            ILoggerFactory loggerFactory,
            IServerApplicationHost appHost,
            ISessionManager sessionManager,
            IHttpClient httpClient,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IDlnaManager dlnaManager,
            IImageProcessor imageProcessor,
            IUserDataManager userDataManager,
            ILocalizationManager localizationManager,
            IMediaSourceManager mediaSourceManager,
            IDeviceDiscovery deviceDiscovery,
            IMediaEncoder mediaEncoder,
            INetworkManager networkManager,
            IUserViewManager userViewManager,
            ITVSeriesManager tvSeriesManager)
        {
            _config = config;
            _appHost = appHost;
            _sessionManager = sessionManager;
            _httpClient = httpClient;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dlnaManager = dlnaManager;
            _imageProcessor = imageProcessor;
            _userDataManager = userDataManager;
            _localization = localizationManager;
            _mediaSourceManager = mediaSourceManager;
            _deviceDiscovery = deviceDiscovery;
            _mediaEncoder = mediaEncoder;
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _logger = loggerFactory.CreateLogger<DlnaEntryPoint>();
            _loggerFactory = loggerFactory;
            _localizationManager = localizationManager;
            _userViewManager = userViewManager;
            _tvSeriesManager = tvSeriesManager;
            _networkManager.NetworkChanged += delegate { ReloadComponents(); };

            Current = this;
        }

        public bool DLNAEnabled => _config.GetDlnaConfiguration().EnableServer;

        public ISsdpCommunicationsServer CommunicationsServer => _communicationsServer;

        internal IContentDirectory ContentDirectory { get; private set; }

        internal IConnectionManager ConnectionManager { get; private set; }

        internal IMediaReceiverRegistrar MediaReceiverRegistrar { get; private set; }

        public static DlnaEntryPoint Current { get; internal set; }

        public async Task RunAsync()
        {
            await ((DlnaManager)_dlnaManager).InitProfilesAsync().ConfigureAwait(false);

            ReloadComponents();

            _config.NamedConfigurationUpdated += OnNamedConfigurationUpdated;
        }

        private void OnNamedConfigurationUpdated(object sender, ConfigurationUpdateEventArgs e)
        {
            if (string.Equals(e.Key, "dlna", StringComparison.OrdinalIgnoreCase))
            {
                ReloadComponents();
            }
        }

        /// <summary>
        /// (Re)initialises the DLNA settings.
        /// </summary>
        private void ReloadComponents()
        {
            _logger.LogDebug("Reloading DLNA components.");

            var options = _config.GetDlnaConfiguration();

            if (options.EnableServer)
            {
                if (ContentDirectory != null)
                {
                    ContentDirectory = new ContentDirectory.ContentDirectory(
                        _dlnaManager,
                        _userDataManager,
                        _imageProcessor,
                        _libraryManager,
                        _config,
                        _userManager,
                        _loggerFactory.CreateLogger<ContentDirectory.ContentDirectory>(),
                        _httpClient,
                        _localizationManager,
                        _mediaSourceManager,
                        _userViewManager,
                        _mediaEncoder,
                        _tvSeriesManager);
                }

                if (ConnectionManager != null)
                {
                    ConnectionManager = new ConnectionManager.ConnectionManager(
                        _dlnaManager,
                        _config,
                        _loggerFactory.CreateLogger<ConnectionManager.ConnectionManager>(),
                        _httpClient);
                }

                if (MediaReceiverRegistrar != null)
                {
                    MediaReceiverRegistrar = new MediaReceiverRegistrar.MediaReceiverRegistrar(
                        _loggerFactory.CreateLogger<MediaReceiverRegistrar.MediaReceiverRegistrar>(),
                        _httpClient,
                        _config);
                }

                StartSsdpHandler();
                StartDevicePublisher(options);
            }
            else
            {
                StopSsdpHandler();
                DisposeDevicePublisher();
                MediaReceiverRegistrar = null;
                ContentDirectory = null;
                ConnectionManager = null;
            }

            if (options.EnablePlayTo)
            {
                StartPlayToManager();
            }
            else
            {
                DisposePlayToManager();
            }
        }

        private void StartSsdpHandler()
        {
            try
            {
                if (_communicationsServer == null)
                {
                    _communicationsServer = new SsdpCommunicationsServer(_networkManager, _config, _loggerFactory.CreateLogger<SsdpCommunicationsServer>());

                    try
                    {
                        ((DeviceDiscovery)_deviceDiscovery).Start(_communicationsServer);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error starting device discovery");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting ssdp handlers");
            }
        }

        private void StopSsdpHandler()
        {
            try
            {
                try
                {
                    ((DeviceDiscovery)_deviceDiscovery).Stop();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error starting device discovery");
                }

                if (_communicationsServer != null)
                {
                    _communicationsServer.Dispose();
                    _communicationsServer = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping ssdp handlers");
            }
        }

        private void DisposeDeviceDiscovery()
        {
            try
            {
                _logger.LogInformation("Disposing DeviceDiscovery");
                ((DeviceDiscovery)_deviceDiscovery).Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping device discovery");
            }
        }

        public void StartDevicePublisher(Configuration.DlnaOptions options)
        {
            if (options == null)
            {
                throw new NullReferenceException(nameof(options));
            }

            // See comment at https://github.com/jellyfin/jellyfin/pull/3257 - this stops jellyfin from being a DNLA compliant server.
            // if (!options.BlastAliveMessages)
            // {
            //    return;
            // }

            // This is true on startup and at network change.
            if (_publisher != null)
            {
                // See if there are any more endpoints we need to add due to network change event.
                try
                {
                    RegisterServerEndpoints();
                    // Restart the timer.
                    _publisher.StartBroadcastingAliveMessages(TimeSpan.FromSeconds(options.BlastAliveMessageIntervalSeconds));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error registering endpoint");
                }

                return;
            }

            try
            {
                _publisher = new SsdpDevicePublisher(
                    _communicationsServer,
                    OperatingSystem.Name,
                    Environment.OSVersion.VersionString,
                    _appHost.SystemId,
                    _loggerFactory.CreateLogger<SsdpDevicePublisher>(),
                    _networkManager)
                    // _config.GetDlnaConfiguration().SendOnlyMatchedHost)
                {
                    SupportPnpRootDevice = false
                };

                RegisterServerEndpoints();
                _publisher.StartBroadcastingAliveMessages(TimeSpan.FromSeconds(options.BlastAliveMessageIntervalSeconds));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering endpoint");
            }
        }

        /// <summary>
        /// Registers SSDP endpoints on the internal interfaces.
        /// </summary>
        private void RegisterServerEndpoints()
        {
            var udn = CreateUuid(_appHost.SystemId);

            foreach (IPObject addr in _networkManager.GetInternalBindAddresses())
            {
                if (addr.IsLoopback())
                {
                    // Don't advertise loopbacks
                    continue;
                }

                var fullService = "urn:schemas-upnp-org:device:MediaServer:1";

                _logger.LogInformation("Registering publisher for {0} on {1}", fullService, addr.Address);

                var descriptorUri = "/dlna/" + udn + "/description.xml";
                var uri = new Uri(_appHost.GetSmartApiUrl(addr.Address) + descriptorUri);

                var device = new SsdpRootDevice
                {
                    CacheLifetime = TimeSpan.FromSeconds(1800), // How long SSDP clients can cache this info.
                    Location = uri, // Must point to the URL that serves your devices UPnP description document.
                    Address = addr.Address,
                    SubnetMask = addr.Mask,
                    FriendlyName = "Jellyfin",
                    Manufacturer = "Jellyfin",
                    ModelName = "Jellyfin Server",
                    Uuid = udn
                    // This must be a globally unique value that survives reboots etc. Get from storage or embedded hardware etc.
                };

                SetProperies(device, fullService);

                _ = _publisher.AddDevice(device);

                var embeddedDevices = new[]
                {
                    "urn:schemas-upnp-org:service:ContentDirectory:1",
                    "urn:schemas-upnp-org:service:ConnectionManager:1",
                    // "urn:microsoft.com:service:X_MS_MediaReceiverRegistrar:1"
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
            if (!Guid.TryParse(text, out var guid))
            {
                guid = text.GetMD5();
            }

            return guid.ToString("N", CultureInfo.InvariantCulture);
        }

        private void SetProperies(SsdpDevice device, string fullDeviceType)
        {
            var service = fullDeviceType.Replace("urn:", string.Empty, StringComparison.OrdinalIgnoreCase).Replace(":1", string.Empty, StringComparison.OrdinalIgnoreCase);

            var serviceParts = service.Split(':');

            var deviceTypeNamespace = serviceParts[0].Replace('.', '-');

            device.DeviceTypeNamespace = deviceTypeNamespace;
            device.DeviceClass = serviceParts[1];
            device.DeviceType = serviceParts[2];
        }

        private void StartPlayToManager()
        {
            lock (_syncLock)
            {
                if (_manager != null)
                {
                    return;
                }

                try
                {
                    _manager = new PlayToManager(
                        _logger,
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
                    _logger.LogError(ex, "Error starting PlayTo manager");
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
                        _logger.LogInformation("Disposing PlayToManager");
                        _manager.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing PlayTo manager");
                    }

                    _manager = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                DisposeDevicePublisher();
                DisposePlayToManager();
                DisposeDeviceDiscovery();
                if (_communicationsServer != null && _communicationsServer.IsShared-- <= 0)
                {
                    _logger.LogInformation("Disposing SsdpCommunicationsServer");
                    _communicationsServer.Dispose();
                    _communicationsServer = null;
                }

                ContentDirectory = null;
                ConnectionManager = null;
                MediaReceiverRegistrar = null;
                Current = null;
            }

            _isDisposed = true;
        }

        public void DisposeDevicePublisher()
        {
            if (_publisher != null)
            {
                _logger.LogInformation("Disposing SsdpDevicePublisher");
                _publisher.Dispose();
                _publisher = null;
            }
        }
    }
}
