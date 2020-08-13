#pragma warning disable CS1591
using System;
using System.Globalization;
using System.Threading.Tasks;
using Emby.Dlna.PlayTo;
using Emby.Dlna.Rssdp;
using Emby.Dlna.Rssdp.Devices;
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
using MediaBrowser.Model.Globalization;
using Microsoft.Extensions.Logging;

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
        private SocketServer _socketServer;
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

        public static DlnaEntryPoint Current { get; internal set; }

        public bool DLNAEnabled => _config.GetDlnaConfiguration().EnableServer;

        public bool EnablePlayTo => _config.GetDlnaConfiguration().EnablePlayTo;

        public SocketServer SocketServer => _socketServer;

        internal IContentDirectory ContentDirectory { get; private set; }

        internal IConnectionManager ConnectionManager { get; private set; }

        internal IMediaReceiverRegistrar MediaReceiverRegistrar { get; private set; }

        public async Task RunAsync()
        {
            await ((DlnaManager)_dlnaManager).InitProfilesAsync().ConfigureAwait(false);

            ReloadComponents();

            _config.NamedConfigurationUpdated += OnNamedConfigurationUpdated;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
                try
                {
                    _logger.LogInformation("Disposing DeviceDiscovery");
                    _deviceDiscovery.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping device discovery");
                }

                _socketServer?.Dispose();
                _socketServer = null;
                ContentDirectory = null;
                ConnectionManager = null;
                MediaReceiverRegistrar = null;
                Current = null;
            }

            _isDisposed = true;
        }

        private void OnNamedConfigurationUpdated(object sender, ConfigurationUpdateEventArgs e)
        {
            if (string.Equals(e.Key, "dlna", StringComparison.OrdinalIgnoreCase))
            {
                ReloadComponents();
                _publisher.AliveMessageInterval = _config.GetDlnaConfiguration().BlastAliveMessageIntervalSeconds;
            }
        }

        /// <summary>
        /// (Re)initialises the DLNA settings.
        /// </summary>
        private void ReloadComponents()
        {
            _logger.LogDebug("(Re)loading DLNA components.");

            var options = _config.GetDlnaConfiguration();

            if (options.EnableServer)
            {
                if (ContentDirectory == null)
                {
                    ContentDirectory = new ContentDirectory.DlnaContentDirectory(
                        _dlnaManager,
                        _userDataManager,
                        _imageProcessor,
                        _libraryManager,
                        _config,
                        _userManager,
                        _loggerFactory.CreateLogger<ContentDirectory.DlnaContentDirectory>(),
                        _httpClient,
                        _localizationManager,
                        _mediaSourceManager,
                        _userViewManager,
                        _mediaEncoder,
                        _tvSeriesManager);
                }

                if (ConnectionManager == null)
                {
                    ConnectionManager = new ConnectionManager.DlnaConnectionManager(
                        _dlnaManager,
                        _config,
                        _loggerFactory.CreateLogger<ConnectionManager.DlnaConnectionManager>(),
                        _httpClient);
                }

                if (MediaReceiverRegistrar == null)
                {
                    MediaReceiverRegistrar = new MediaReceiverRegistrar.DlnaMediaReceiverRegistrar(
                        _loggerFactory.CreateLogger<MediaReceiverRegistrar.DlnaMediaReceiverRegistrar>(),
                        _httpClient,
                        _config);
                }

                // Start device SSDP
                try
                {
                    if (_socketServer == null)
                    {
                        _socketServer = new SocketServer(_networkManager, _config, _loggerFactory);
                        _deviceDiscovery.Start(_socketServer);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error starting ssdp handlers");
                }

                // This is true on startup and at network change.
                if (_publisher == null)
                {
                    _publisher = new SsdpDevicePublisher(_socketServer, _appHost, _loggerFactory, _networkManager, options.BlastAliveMessageIntervalSeconds)
                    {
                        SupportPnpRootDevice = false
                    };

                    RegisterServerEndpoints();
                }
            }
            else
            {
                // Disable the server
                _deviceDiscovery.Stop();

                if (_socketServer != null)
                {
                    _socketServer.Dispose();
                    _socketServer = null;
                }

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

                SsdpRootDevice device = new SsdpRootDevice(
                    TimeSpan.FromSeconds(1800), // How long SSDP clients can cache this info.
                    uri, // Must point to the URL that serves your devices UPnP description document.
                    addr.Address,
                    addr.Mask,
                    "Jellyfin",
                    "Jellyfin",
                    "Jellyfin Server",
                    udn); // This must be a globally unique value that survives reboots etc. Get from storage or embedded hardware etc.

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
                    var embeddedDevice = new SsdpEmbeddedDevice(device, udn);  // This must be a globally unique value that survives reboots etc. Get from storage or embedded hardware etc.
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
    }
}
