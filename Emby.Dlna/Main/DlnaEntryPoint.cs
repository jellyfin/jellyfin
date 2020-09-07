#pragma warning disable SA1611 // Element parameters should be documented
#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Emby.Dlna.Configuration;
using Emby.Dlna.ConnectionManager;
using Emby.Dlna.ContentDirectory;
using Emby.Dlna.MediaReceiverRegistrar;
using Emby.Dlna.Net;
using Emby.Dlna.PlayTo;
using Emby.Dlna.PlayTo.Devices;
using Jellyfin.Networking.Manager;
using Jellyfin.Networking.Ssdp;
using Jellyfin.Networking.Structures;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
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

namespace Emby.Dlna.Main
{
    /// <summary>
    /// Manages all DLNA functionality.
    /// </summary>
    public class DlnaEntryPoint : IServerEntryPoint, IRunBeforeStartup
    {
        private static DlnaEntryPoint? _instance;
        private readonly object _syncLock = new object();
        private readonly IServerConfigurationManager _configurationManager;
        private readonly ILogger<DlnaEntryPoint> _logger;
        private readonly IServerApplicationHost _appHost;
        private readonly ISessionManager _sessionManager;
        private readonly IHttpClientFactory _httpClientFactory;
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
        private readonly ISsdpServer _ssdpServer;

        private SsdpServerPublisher? _publisher;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaEntryPoint"/> class.
        /// </summary>
        public DlnaEntryPoint(
            IServerConfigurationManager config,
            ILoggerFactory loggerFactory,
            IServerApplicationHost appHost,
            ISessionManager sessionManager,
            IHttpClientFactory httpClientFactory,
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
            _httpClientFactory = httpClientFactory;
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
            _ssdpServer = SsdpServer.GetOrCreateInstance(_networkManager, config, loggerFactory.CreateLogger<SsdpServer>(), appHost);
            Instance = this;

            _networkManager.NetworkChanged += NetworkChanged;
        }

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
        /// Override this method and dispose any objects you own the lifetime of if disposing is true.
        /// </summary>
        /// <param name="disposing">True if managed objects should be disposed, if false, only unmanaged resources should be released.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                _publisher?.Dispose();
                _publisher = null;

                lock (_syncLock)
                {
                    PlayToManager?.Dispose();
                    PlayToManager = null;
                }

                _networkManager.NetworkChanged -= NetworkChanged;
                _configurationManager.NamedConfigurationUpdated -= OnNamedConfigurationUpdated;

                ContentDirectory = null;
                ConnectionManager = null;
                MediaReceiverRegistrar = null;
                _instance = null;
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
                    _publisher.AliveMessageInterval = _configurationManager.GetDlnaConfiguration().AliveMessageIntervalSeconds;
                }
            }
        }

        /// <summary>
        /// Triggered every time there is a network event.
        /// </summary>
        /// <param name="sender">NetworkManager instance.</param>
        /// <param name="e">Event argument.</param>
        private void NetworkChanged(object? sender, EventArgs e)
        {
            _publisher?.Dispose();
            _publisher = null;
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

                if (options.EnablePlayTo)
                {
                    if (PlayToManager == null)
                    {
                        _logger.LogDebug("DLNA PlayTo: Starting Service.");
                        PlayToManager = new PlayToManager(
                            _loggerFactory,
                            _sessionManager,
                            _libraryManager,
                            _userManager,
                            _dlnaManager,
                            _appHost,
                            _imageProcessor,
                            _httpClientFactory,
                            _configurationManager,
                            _userDataManager,
                            _localization,
                            _mediaSourceManager,
                            _mediaEncoder,
                            _notificationManager,
                            _configurationManager);
                    }
                }
                else if (PlayToManager != null)
                {
                    _logger.LogDebug("DLNA PlayTo: Stopping Service.");
                    lock (_syncLock)
                    {
                        PlayToManager?.Dispose();
                        PlayToManager = null;
                    }

                    GC.Collect();
                }

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
                            _httpClientFactory,
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
                            _httpClientFactory);
                    }

                    if (MediaReceiverRegistrar == null)
                    {
                        _logger.LogDebug("DLNA Server : Starting Media Receiver Registrar service.");
                        MediaReceiverRegistrar = new MediaReceiverRegistrarService(
                            _loggerFactory.CreateLogger<MediaReceiverRegistrarService>(),
                            _httpClientFactory,
                            _configurationManager);
                    }

                    // This is true on startup and at network change.
                    if (_publisher == null)
                    {
                        _logger.LogDebug("DLNA Server : Starting DLNA advertisements.");
                        _publisher = new SsdpServerPublisher(_ssdpServer, _loggerFactory, _networkManager, options.AliveMessageIntervalSeconds);
                        RegisterDLNAServerEndpoints();
                    }
                }
                else if (_publisher != null)
                {
                    _logger.LogDebug("DLNA Server : Stopping all DLNA server services.");
                    ContentDirectory = null;
                    MediaReceiverRegistrar = null;
                    ConnectionManager = null;
                    _publisher?.Dispose();
                    _publisher = null;
                    GC.Collect();
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

            var ba = _networkManager.GetInternalBindAddresses()
                .Where(i => i.AddressFamily == AddressFamily.InterNetwork || (i.AddressFamily == AddressFamily.InterNetworkV6 && i.Address.ScopeId != 0));

            if (!ba.Any())
            {
                // No interfaces returned, so use loopback.
                ba = _networkManager.GetLoopbacks();
            }

            foreach (IPObject addr in ba)
            {
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
