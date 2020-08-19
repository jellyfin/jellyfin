#nullable enable
using System;
using System.Globalization;
using System.Threading.Tasks;
using Emby.Dlna.ConnectionManager;
using Emby.Dlna.ContentDirectory;
using Emby.Dlna.MediaReceiverRegistrar;
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
        private readonly IServerConfigurationManager _configurationManager;
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
        private readonly INetworkManager _networkManager;
        private readonly object _syncLock = new object();
        private readonly IUserViewManager _userViewManager;
        private readonly ITVSeriesManager _tvSeriesManager;
        private readonly ILocalizationManager _localizationManager;
        private readonly ILoggerFactory _loggerFactory;
        private PlayToManager? _manager;
        private SsdpServerPublisher? _publisher;
        private SocketServer? _socketManager;
        private IDeviceDiscovery? _deviceDiscovery;
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
            IMediaEncoder mediaEncoder,
            INetworkManager networkManager,
            IUserViewManager userViewManager,
            ITVSeriesManager tvSeriesManager)
        {
            _configurationManager = config ?? throw new ArgumentNullException(nameof(config));
            _appHost = appHost ?? throw new ArgumentNullException(nameof(appHost));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _libraryManager = libraryManager ?? throw new ArgumentNullException(nameof(libraryManager));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _dlnaManager = dlnaManager ?? throw new ArgumentNullException(nameof(dlnaManager));
            _imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
            _userDataManager = userDataManager ?? throw new ArgumentNullException(nameof(userDataManager));
            _localization = localizationManager ?? throw new ArgumentNullException(nameof(localizationManager));
            _mediaSourceManager = mediaSourceManager ?? throw new ArgumentNullException(nameof(mediaSourceManager));
            _mediaEncoder = mediaEncoder ?? throw new ArgumentNullException(nameof(mediaEncoder));
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _localizationManager = localizationManager ?? throw new ArgumentNullException(nameof(localizationManager));
            _userViewManager = userViewManager ?? throw new ArgumentNullException(nameof(userViewManager));
            _tvSeriesManager = tvSeriesManager ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<DlnaEntryPoint>();
            _networkManager.NetworkChanged += TriggerReload;

            Instance = this;
        }

        public static DlnaEntryPoint? Instance { get; internal set; }

        public bool DLNAEnabled => _configurationManager.GetDlnaConfiguration().EnableServer;

        public bool EnablePlayTo => _configurationManager.GetDlnaConfiguration().EnablePlayTo;

        public IContentDirectory? ContentDirectory { get; private set; }

        public IConnectionManager? ConnectionManager { get; private set; }

        public IMediaReceiverRegistrar? MediaReceiverRegistrar { get; private set; }

        public async Task RunAsync()
        {
            await ((DlnaManager)_dlnaManager).InitProfilesAsync().ConfigureAwait(false);

            ReloadComponents();

            _configurationManager.NamedConfigurationUpdated += OnNamedConfigurationUpdated;
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

                lock (_syncLock)
                {
                    _manager?.Dispose();
                    _manager = null;
                }

                _networkManager.NetworkChanged -= TriggerReload;
                _configurationManager.NamedConfigurationUpdated -= OnNamedConfigurationUpdated;

                ContentDirectory = null;
                ConnectionManager = null;
                MediaReceiverRegistrar = null;
                Instance = null;

                // This object will actually only dispose if no longer in use.
                _deviceDiscovery?.Dispose();
                _deviceDiscovery = null;

                // This object will actually only dispose if no longer in use.
                _socketManager?.Dispose();
                _socketManager = null;
            }

            _isDisposed = true;
        }

        private static string CreateUuid(string text)
        {
            if (!Guid.TryParse(text, out var guid))
            {
                guid = text.GetMD5();
            }

            return guid.ToString("N", CultureInfo.InvariantCulture);
        }

        private static void SetProperies(SsdpDevice device, string fullDeviceType)
        {
            var service = fullDeviceType.Replace("urn:", string.Empty, StringComparison.OrdinalIgnoreCase).Replace(":1", string.Empty, StringComparison.OrdinalIgnoreCase);

            var serviceParts = service.Split(':');

            var deviceTypeNamespace = serviceParts[0].Replace('.', '-');

            device.DeviceTypeNamespace = deviceTypeNamespace;
            device.DeviceClass = serviceParts[1];
            device.DeviceType = serviceParts[2];
        }

        private void OnNamedConfigurationUpdated(object sender, ConfigurationUpdateEventArgs e)
        {
            if (string.Equals(e.Key, "dlna", StringComparison.OrdinalIgnoreCase))
            {
                ReloadComponents();
                if (_publisher != null)
                {
                    _publisher.AliveMessageInterval = _configurationManager.GetDlnaConfiguration().BlastAliveMessageIntervalSeconds;
                }
            }
        }

        private void TriggerReload(object sender, EventArgs e)
        {
            ReloadComponents();
        }

        /// <summary>
        /// (Re)initialises the DLNA settings.
        /// </summary>
        private void ReloadComponents()
        {
            _logger.LogDebug("(Re)loading DLNA components.");
            lock (_syncLock)
            {
                var options = _configurationManager.GetDlnaConfiguration();

                if (options.EnablePlayTo || options.EnableServer)
                {
                    // Start SSDP communication handlers.
                    _socketManager = _socketManager = SocketServer.Instance ?? new SocketServer(_networkManager, _configurationManager, _loggerFactory);
                }

                if (options.EnableServer)
                {
                    _logger.LogDebug("Starting DLNA Server.");

                    // Create SSDP server.
                    if (ContentDirectory == null)
                    {
                        ContentDirectory = new DlnaContentDirectory(
                            _dlnaManager,
                            _userDataManager,
                            _imageProcessor,
                            _libraryManager,
                            _configurationManager,
                            _userManager,
                            _httpClient,
                            _localizationManager,
                            _mediaSourceManager,
                            _userViewManager,
                            _mediaEncoder,
                            _tvSeriesManager,
                            _loggerFactory);
                    }

                    if (ConnectionManager == null)
                    {
                        ConnectionManager = new DlnaConnectionManager(
                            _dlnaManager,
                            _configurationManager,
                            _loggerFactory.CreateLogger<DlnaControlHandler>(),
                            _httpClient);
                    }

                    if (MediaReceiverRegistrar == null)
                    {
                        MediaReceiverRegistrar = new DlnaMediaReceiverRegistrar(
                            _loggerFactory.CreateLogger<DlnaMediaReceiverRegistrar>(),
                            _httpClient,
                            _configurationManager);
                    }

                    // This is true on startup and at network change.
                    if (_publisher == null)
                    {
                        _publisher = new SsdpServerPublisher(_socketManager, _loggerFactory, _networkManager, options.BlastAliveMessageIntervalSeconds);

                        RegisterServerEndpoints();
                    }
                }
                else
                {
                    _logger.LogDebug("Stopping DLNA Server.");

                    // This object will actually only dispose if no longer in use.
                    _deviceDiscovery?.Dispose();
                    _deviceDiscovery = null;

                    // This object will actually only dispose if no longer in use.
                    _socketManager?.Dispose();
                    _socketManager = null;

                    DisposeDevicePublisher();

                    MediaReceiverRegistrar = null;
                    ContentDirectory = null;
                    ConnectionManager = null;
                    GC.Collect();
                }

                if (options.EnablePlayTo)
                {
                    if (_deviceDiscovery == null)
                    {
                        _deviceDiscovery = new DeviceDiscovery(_configurationManager, _loggerFactory, _networkManager, _socketManager);
                    }

                    if (_manager == null)
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
                            _configurationManager,
                            _userDataManager,
                            _localization,
                            _mediaSourceManager,
                            _mediaEncoder);

                        _manager.Start();
                    }
                }
                else
                {
                    lock (_syncLock)
                    {
                        _manager?.Dispose();
                        _manager = null;
                        GC.Collect();
                    }
                }
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

                _ = _publisher?.AddDevice(device);

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
    }
}
