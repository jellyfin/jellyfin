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
using Emby.Dlna.PlayTo.Devices;
using Jellyfin.Networking.Ssdp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Globalization;
using Microsoft.Extensions.Logging;
using NetworkCollection;

namespace Emby.Dlna.Main
{
    /// <summary>
    /// Defines the <see cref="DlnaServerManager"/> class.
    /// </summary>
    public class DlnaServerManager : IDisposable, IDlnaServerManager
    {
        private readonly object _syncLock = new object();
        private readonly IServerConfigurationManager _configurationManager;
        private readonly ILogger<DlnaServerManager> _logger;
        private readonly IServerApplicationHost _appHost;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDlnaManager _dlnaManager;
        private readonly IImageProcessor _imageProcessor;
        private readonly IUserDataManager _userDataManager;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly INetworkManager _networkManager;
        private readonly IUserViewManager _userViewManager;
        private readonly ITVSeriesManager _tvSeriesManager;
        private readonly ILocalizationManager _localizationManager;
        private readonly ILoggerFactory _loggerFactory;

        private SsdpServerPublisher? _publisher;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaServerManager"/> class.
        /// </summary>
        /// <param name="config">The <see cref="IServerConfigurationManager"/> instance.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> instance.</param>
        /// <param name="appHost">The apHost<see cref="IServerApplicationHost"/> instance.</param>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> instance.</param>
        /// <param name="libraryManager">The <see cref="ILibraryManager"/> instance.</param>
        /// <param name="userManager">The <see cref="IUserManager"/> instance.</param>
        /// <param name="dlnaManager">The <see cref="IDlnaManager"/> instance.</param>
        /// <param name="imageProcessor">The <see cref="IImageProcessor"/> instance.</param>
        /// <param name="userDataManager">The <see cref="IUserDataManager"/> instance.</param>
        /// <param name="localizationManager">The <see cref="ILocalizationManager"/> instance.</param>
        /// <param name="mediaSourceManager">The <see cref="IMediaSourceManager"/> instance.</param>
        /// <param name="mediaEncoder">The <see cref="IMediaEncoder"/> instance.</param>
        /// <param name="networkManager">The <see cref="INetworkManager"/> instance.</param>
        /// <param name="userViewManager">The <see cref="IUserViewManager"/> instance.</param>
        /// <param name="tvSeriesManager">The <see cref="ITVSeriesManager"/> instance.</param>
        public DlnaServerManager(
            IServerConfigurationManager config,
            ILoggerFactory loggerFactory,
            IServerApplicationHost appHost,
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
            ITVSeriesManager tvSeriesManager)
        {
            _configurationManager = config;
            _appHost = appHost;
            _httpClientFactory = httpClientFactory;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dlnaManager = dlnaManager;
            _imageProcessor = imageProcessor;
            _userDataManager = userDataManager;
            _mediaSourceManager = mediaSourceManager;
            _mediaEncoder = mediaEncoder;
            _networkManager = networkManager;
            _loggerFactory = loggerFactory;
            _localizationManager = localizationManager;
            _userViewManager = userViewManager;
            _tvSeriesManager = tvSeriesManager;
            _logger = loggerFactory.CreateLogger<DlnaServerManager>();
            _networkManager.NetworkChanged += NetworkChanged;
            _configurationManager.NamedConfigurationUpdated += NamedConfigurationUpdated;
            _ = _dlnaManager.InitProfilesAsync();
            CheckComponents();
        }

        /// <summary>
        /// Gets the DLNA server' ContentDirectory instance.
        /// </summary>
        public IContentDirectory? ContentDirectory { get; private set; }

        /// <summary>
        /// Gets the DLNA server' ConnectionManager instance.
        /// </summary>
        public IConnectionManager? ConnectionManager { get; private set; }

        /// <summary>
        /// Gets the DLNA server's MediaReceiverRegistrar instance.
        /// </summary>
        public IMediaReceiverRegistrar? MediaReceiverRegistrar { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the DLNA server is active.
        /// </summary>
        public bool IsDLNAServerEnabled { get; private set; }

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

                _networkManager.NetworkChanged -= NetworkChanged;
                _configurationManager.NamedConfigurationUpdated -= NamedConfigurationUpdated;

                ContentDirectory = null;
                ConnectionManager = null;
                MediaReceiverRegistrar = null;
            }

            _isDisposed = true;
        }

        /// <summary>
        /// The CreateUuid.
        /// </summary>
        /// <param name="text">The text<see cref="string"/>.</param>
        /// <returns>The <see cref="string"/>.</returns>
        private static string CreateUuid(string text)
        {
            if (!Guid.TryParse(text, out var guid))
            {
                guid = text.GetMD5();
            }

            return guid.ToString("N", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The SetProperies.
        /// </summary>
        /// <param name="device">The device<see cref="SsdpDevice"/>.</param>
        /// <param name="fullDeviceType">The fullDeviceType<see cref="string"/>.</param>
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
        private void NamedConfigurationUpdated(object sender, ConfigurationUpdateEventArgs e)
        {
            if (string.Equals(e.Key, "dlna", StringComparison.OrdinalIgnoreCase))
            {
                CheckComponents();
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
                IsDLNAServerEnabled = options.EnableServer;

                if (IsDLNAServerEnabled)
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
                        _publisher = new SsdpServerPublisher(
                            SsdpServer.GetOrCreateInstance(
                                _networkManager,
                                _configurationManager,
                                _loggerFactory.CreateLogger<SsdpServer>(),
                                _appHost),
                            _loggerFactory,
                            _networkManager,
                            options.AliveMessageIntervalSeconds);
                        RegisterDLNAServerEndpoints();
                    }
                    else
                    {
                        _publisher.AliveMessageInterval = options.AliveMessageIntervalSeconds;
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

            var ba = new NetCollection(
                _networkManager.GetInternalBindAddresses()
                .Where(i => i.AddressFamily == AddressFamily.InterNetwork || (i.AddressFamily == AddressFamily.InterNetworkV6 && i.Address.ScopeId != 0)));

            if (ba.Count == 0)
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
                    cacheLifetime: TimeSpan.FromSeconds(1800),
                    location: uri,
                    address: addr,
                    friendlyName: "Jellyfin",
                    manufacturer: "Jellyfin",
                    modelName: "Jellyfin Server",
                    uuid: udn);

                SetProperies(device, FullService);

                _ = _publisher?.AddDevice(device);

                var embeddedDevices = new[]
                {
                    "urn:schemas-upnp-org:service:ContentDirectory:1",
                    "urn:schemas-upnp-org:service:ConnectionManager:1",
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
