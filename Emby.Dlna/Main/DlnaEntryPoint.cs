#pragma warning disable SA1611 // Element parameters should be documented
#nullable enable
using System;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Emby.Dlna.ConnectionManager;
using Emby.Dlna.ContentDirectory;
using Emby.Dlna.MediaReceiverRegistrar;
using Emby.Dlna.Net;
using Emby.Dlna.PlayTo;
using Emby.Dlna.PlayTo.Devices;
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
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Globalization;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna
{
    /// <summary>
    /// Manages all DLNA functionality.
    /// </summary>
    public class DlnaEntryPoint : IServerEntryPoint, IRunBeforeStartup
    {
#pragma warning disable IDE0032 // Convert to auto: _name only needs to be calculated once. _nLS MUST stay the same until a network change.
        private static readonly string _name = $"{MediaBrowser.Common.System.OperatingSystem.Name}/{Environment.OSVersion.VersionString} UPnP/1.0 RSSDP/1.0";
        private static string _nLS = Guid.NewGuid().ToString();
#pragma warning restore IDO0032
        private static DlnaEntryPoint? _instance;

        private readonly object _syncLock = new object();
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
        private readonly IUserViewManager _userViewManager;
        private readonly ITVSeriesManager _tvSeriesManager;
        private readonly ILocalizationManager _localizationManager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly INotificationManager _notificationManager;
        private readonly SocketServer _socketManager;

        private SsdpServerPublisher? _publisher;
        private IDeviceDiscovery? _deviceDiscovery;
        private bool _isDisposed;
        private int _changes;

        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaEntryPoint"/> class.
        /// </summary>
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
            ITVSeriesManager tvSeriesManager,
            INotificationManager notificationManager)
        {
            _configurationManager = config;
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
            _mediaEncoder = mediaEncoder;
            _networkManager = networkManager;
            _loggerFactory = loggerFactory;
            _localizationManager = localizationManager;
            _userViewManager = userViewManager;
            _tvSeriesManager = tvSeriesManager;
            _notificationManager = notificationManager;

            _logger = loggerFactory.CreateLogger<DlnaEntryPoint>();
            Instance = this;
            _socketManager = new SocketServer(_networkManager, _configurationManager, loggerFactory, appHost);

            _networkManager.NetworkChanged += NetworkChanged;
            NetworkChange.NetworkAddressChanged += this.OnNetworkAddressChanged;
        }

        /// <summary>
        /// Gets the GUID of this Dlna instance.
        /// </summary>
        public static string NetworkLocationSignature => _nLS;

        /// <summary>
        /// Gets the singleton instance of this object.
        /// </summary>
        public static DlnaEntryPoint Instance
        {
            get
            {
                return GetInstance();
            }

            internal set
            {
                _instance = value;
            }
        }

        /// <summary>
        /// Gets the SsdpServer name used in advertisements.
        /// </summary>
        public static string Name => _name;

        /// <summary>
        /// Gets the DLNA server' ContentDirectory instance.
        /// </summary>
        public static IContentDirectory? ContentDirectory { get; private set; }

        /// <summary>
        /// Gets the DLNA server' ConnectionManager instance.
        /// </summary>
        public static IConnectionManager? ConnectionManager { get; private set; }

        /// <summary>
        /// Gets the DLNA server's MediaReceiverRegistrar instance.
        /// </summary>
        public static IMediaReceiverRegistrar? MediaReceiverRegistrar { get; private set; }

        /// <summary>
        /// Gets the PlayToManager instance.
        /// </summary>
        public static PlayToManager? PlayToManager { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the DLNA server is active.
        /// </summary>
        public bool IsDLNAServerEnabled => _configurationManager.GetDlnaConfiguration().EnableServer;

        /// <summary>
        /// Gets a value indicating whether DLNA PlayTo is enabled.
        /// </summary>
        public bool IsPlayToEnabled => _configurationManager.GetDlnaConfiguration().EnablePlayTo;

        /// <summary>
        /// Gets the unqiue user agent used in ssdp communications.
        /// </summary>
        public string SsdpUserAgent => $"UPnP/1.0 DLNADOC/1.50 Platinum/1.0.4.2 /{_appHost.SystemId}";

        /// <summary>
        /// Gets the number of times the network address has changed.
        /// </summary>
        public string NetworkChangeCount => _changes.ToString("d2", CultureInfo.InvariantCulture);

        /// <summary>
        /// Executes DlnaEntryPoint's functionality.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync()
        {
            await _dlnaManager.InitProfilesAsync().ConfigureAwait(false);

            CheckComponents();

            _configurationManager.NamedConfigurationUpdated += OnNamedConfigurationUpdated;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the device publisher instance.
        /// </summary>
        public void DisposeDevicePublisher()
        {
            if (_publisher != null)
            {
                _logger.LogInformation("Disposing SsdpDevicePublisher");
                _publisher.Dispose();
                _publisher = null;
            }
        }

        /// <summary>
        /// Disposes of unmanaged resources.
        /// </summary>
        /// <param name="disposing">A Boolean value that indicates whether the method call comes from a Dispose method
        /// or from a finalizer (its value is false).</param>
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
                    PlayToManager?.Dispose();
                    PlayToManager = null;
                }

                _networkManager.NetworkChanged -= NetworkChanged;
                _configurationManager.NamedConfigurationUpdated -= OnNamedConfigurationUpdated;

                NetworkChange.NetworkAddressChanged -= this.OnNetworkAddressChanged;

                ContentDirectory = null;
                ConnectionManager = null;
                MediaReceiverRegistrar = null;
                _instance = null;

                // DeviceDiscovery has an event use count, and will only dispose if no longer in use.
                _deviceDiscovery?.Dispose();
                _deviceDiscovery = null;
            }

            _isDisposed = true;
        }

        private static DlnaEntryPoint GetInstance()
        {
            if (_instance == null)
            {
                throw new ApplicationException("DlnaEntryPoint is not initialised.");
            }

            return _instance;
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

        /// <summary>
        /// Handler for network change events.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnNetworkAddressChanged(object sender, EventArgs e)
        {
            // As per UPnP Device Architecture v1.0 Annex A - IPv6 Support.
            _nLS = Guid.NewGuid().ToString();
            _changes++;
            if (_changes > 99)
            {
                _changes = 1;
            }
        }

        /// <summary>
        /// Triggerer every time the configuration is updated.
        /// </summary>
        /// <param name="sender">Configuration instance.</param>
        /// <param name="e">Configuration that was updated.</param>
        private void OnNamedConfigurationUpdated(object sender, ConfigurationUpdateEventArgs e)
        {
            if (string.Equals(e.Key, "dlna", StringComparison.OrdinalIgnoreCase))
            {
                CheckComponents();

                if (_publisher != null)
                {
                    _publisher.AliveMessageInterval = _configurationManager.GetDlnaConfiguration().BlastAliveMessageIntervalSeconds;
                }
            }
        }

        /// <summary>
        /// Triggered every time there is a network event.
        /// </summary>
        /// <param name="sender">NetworkManager instance.</param>
        /// <param name="e">Event argument.</param>
        private void NetworkChanged(object sender, EventArgs e)
        {
            CheckComponents();
        }

        /// <summary>
        /// (Re)initialises the DLNA settings.
        /// </summary>
        private void CheckComponents()
        {
            lock (_syncLock)
            {
                var options = _configurationManager.GetDlnaConfiguration();

                if (options.EnableServer)
                {
                    if (ContentDirectory == null)
                    {
                        _logger.LogDebug("DLNA Server : Starting Content Directory service.");
                        ContentDirectory = new ContentDirectoryService(
                            _dlnaManager,
                            _userDataManager,
                            _imageProcessor,
                            _libraryManager,
                            _configurationManager,
                            _userManager,
                            _loggerFactory.CreateLogger<ContentDirectoryService>(),
                            _httpClient,
                            _localizationManager,
                            _mediaSourceManager,
                            _userViewManager,
                            _mediaEncoder,
                            _tvSeriesManager);
                    }

                    if (ConnectionManager == null)
                    {
                        _logger.LogDebug("DLNA Server : Starting Connection Manager service.");
                        ConnectionManager = new ConnectionManagerService(
                            _dlnaManager,
                            _configurationManager,
                            _loggerFactory.CreateLogger<ConnectionManagerService>(),
                            _httpClient);
                    }

                    if (MediaReceiverRegistrar == null)
                    {
                        _logger.LogDebug("DLNA Server : Starting Media Receiver Registrar service.");
                        MediaReceiverRegistrar = new MediaReceiverRegistrarService(
                            _loggerFactory.CreateLogger<MediaReceiverRegistrarService>(),
                            _httpClient,
                            _configurationManager);
                    }

                    // This is true on startup and at network change.
                    if (_publisher == null)
                    {
                        _logger.LogDebug("DLNA Server : Starting DLNA advertisements.");
                        _publisher = new SsdpServerPublisher(_socketManager, _loggerFactory, _networkManager, options.BlastAliveMessageIntervalSeconds);
                        RegisterDLNAServerEndpoints();
                    }
                }
                else
                {
                    _logger.LogDebug("DLNA Server : Stopping all DLNA services.");
                    DisposeDevicePublisher();

                    // This object will actually only dispose if no longer in use.
                    _deviceDiscovery?.Dispose();
                    _deviceDiscovery = null;

                    ContentDirectory = null;
                    MediaReceiverRegistrar = null;
                    ConnectionManager = null;
                    GC.Collect();
                }

                if (options.EnablePlayTo)
                {
                    if (_deviceDiscovery == null)
                    {
                        _logger.LogDebug("DLNA PlayTo: Starting Device Discovery.");
                        _deviceDiscovery = new DeviceDiscovery(_configurationManager, _loggerFactory, _networkManager, _socketManager);
                    }

                    if (PlayToManager == null)
                    {
                        _logger.LogDebug("DLNA PlayTo: Starting Service.");
                        PlayToManager = new PlayToManager(
                            _loggerFactory.CreateLogger<PlayToManager>(),
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
                            _mediaEncoder,
                            _notificationManager);
                    }
                }
                else
                {
                    if (PlayToManager != null)
                    {
                        _logger.LogDebug("DLNA PlayTo: Stopping Service.");
                        lock (_syncLock)
                        {
                            PlayToManager?.Dispose();
                            PlayToManager = null;
                        }

                        GC.Collect();
                    }
                }
            }
        }

        /// <summary>
        /// Registers SSDP endpoints on the internal interfaces.
        /// </summary>
        private void RegisterDLNAServerEndpoints()
        {
            const string FullService = "urn:schemas-upnp-org:device:MediaServer:1";

            var udn = CreateUuid(_appHost.SystemId);

            foreach (IPObject addr in _networkManager.GetInternalBindAddresses())
            {
                if (addr.IsLoopback())
                {
                    // Don't advertise loopbacks
                    continue;
                }

                _logger.LogInformation("Registering publisher for {0} on {1}", FullService, addr.Address);

                var descriptorUri = "/dlna/" + udn + "/description.xml";
                var uri = new Uri(_appHost.GetSmartApiUrl(addr.Address) + descriptorUri);

                SsdpRootDevice device = new SsdpRootDevice(
                    TimeSpan.FromSeconds(1800), // How long SSDP clients can cache this info.
                    uri, // Must point to the URL that serves your devices UPnP description document.
                    addr,
                    "Jellyfin",
                    "Jellyfin",
                    "Jellyfin Server",
                    udn); // This must be a globally unique value that survives reboots etc. Get from storage or embedded hardware etc.

                SetProperies(device, FullService);

                _ = _publisher?.AddDevice(device);

                var embeddedDevices = new[]
                {
                    "urn:schemas-upnp-org:service:ContentDirectory:1",
                    "urn:schemas-upnp-org:service:ConnectionManager:1",
                    // "urn:microsoft.com:service:X_MS_MediaReceiverRegistrar:1" // Windows WMDRM.
                };

                foreach (var subDevice in embeddedDevices)
                {
                    var embeddedDevice = new SsdpEmbeddedDevice(
                        device.FriendlyName,
                        device.Manufacturer,
                        device.ModelName,
                        udn);
                    SetProperies(embeddedDevice, subDevice);
                    device.AddDevice(embeddedDevice);
                }
            }
        }
    }
}
