#nullable disable

#pragma warning disable CS1591

using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Emby.Dlna.PlayTo;
using Emby.Dlna.Ssdp;
using Jellyfin.Networking.Configuration;
using Jellyfin.Networking.Manager;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
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
using Microsoft.Extensions.Logging;
using Rssdp;
using Rssdp.Infrastructure;

namespace Emby.Dlna.Main
{
    public sealed class DlnaEntryPoint : IServerEntryPoint, IRunBeforeStartup
    {
        private readonly IServerConfigurationManager _config;
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
        private readonly IDeviceDiscovery _deviceDiscovery;
        private readonly ISocketFactory _socketFactory;
        private readonly INetworkManager _networkManager;
        private readonly object _syncLock = new object();
        private readonly bool _disabled;

        private PlayToManager _manager;
        private SsdpDevicePublisher _publisher;
        private ISsdpCommunicationsServer _communicationsServer;

        private bool _disposed;

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
            IDeviceDiscovery deviceDiscovery,
            IMediaEncoder mediaEncoder,
            ISocketFactory socketFactory,
            INetworkManager networkManager,
            IUserViewManager userViewManager,
            ITVSeriesManager tvSeriesManager)
        {
            _config = config;
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
            _deviceDiscovery = deviceDiscovery;
            _mediaEncoder = mediaEncoder;
            _socketFactory = socketFactory;
            _networkManager = networkManager;
            _logger = loggerFactory.CreateLogger<DlnaEntryPoint>();

            ContentDirectory = new ContentDirectory.ContentDirectoryService(
                dlnaManager,
                userDataManager,
                imageProcessor,
                libraryManager,
                config,
                userManager,
                loggerFactory.CreateLogger<ContentDirectory.ContentDirectoryService>(),
                httpClientFactory,
                localizationManager,
                mediaSourceManager,
                userViewManager,
                mediaEncoder,
                tvSeriesManager);

            ConnectionManager = new ConnectionManager.ConnectionManagerService(
                dlnaManager,
                config,
                loggerFactory.CreateLogger<ConnectionManager.ConnectionManagerService>(),
                httpClientFactory);

            MediaReceiverRegistrar = new MediaReceiverRegistrar.MediaReceiverRegistrarService(
                loggerFactory.CreateLogger<MediaReceiverRegistrar.MediaReceiverRegistrarService>(),
                httpClientFactory,
                config);
            Current = this;

            var netConfig = config.GetConfiguration<NetworkConfiguration>(NetworkConfigurationStore.StoreKey);
            _disabled = appHost.ListenWithHttps && netConfig.RequireHttps;

            if (_disabled && _config.GetDlnaConfiguration().EnableServer)
            {
                _logger.LogError("The DLNA specification does not support HTTPS.");
            }
        }

        public static DlnaEntryPoint Current { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the dlna server is enabled.
        /// </summary>
        public static bool Enabled { get; private set; }

        public IContentDirectory ContentDirectory { get; private set; }

        public IConnectionManager ConnectionManager { get; private set; }

        public IMediaReceiverRegistrar MediaReceiverRegistrar { get; private set; }

        public async Task RunAsync()
        {
            await ((DlnaManager)_dlnaManager).InitProfilesAsync().ConfigureAwait(false);

            if (_disabled)
            {
                // No use starting as dlna won't work, as we're running purely on HTTPS.
                return;
            }

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

        private void ReloadComponents()
        {
            var options = _config.GetDlnaConfiguration();
            Enabled = options.EnableServer;

            StartSsdpHandler();

            if (options.EnableServer)
            {
                StartDevicePublisher(options);
            }
            else
            {
                DisposeDevicePublisher();
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
                    var enableMultiSocketBinding = OperatingSystem.IsWindows() ||
                                                   OperatingSystem.IsLinux();

                    _communicationsServer = new SsdpCommunicationsServer(_socketFactory, _networkManager, _logger, enableMultiSocketBinding)
                    {
                        IsShared = true
                    };

                    StartDeviceDiscovery(_communicationsServer);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting ssdp handlers");
            }
        }

        private void StartDeviceDiscovery(ISsdpCommunicationsServer communicationsServer)
        {
            try
            {
                if (communicationsServer != null)
                {
                    ((DeviceDiscovery)_deviceDiscovery).Start(communicationsServer);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting device discovery");
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
            if (!options.BlastAliveMessages)
            {
                return;
            }

            if (_publisher != null)
            {
                return;
            }

            try
            {
                _publisher = new SsdpDevicePublisher(
                    _communicationsServer,
                    MediaBrowser.Common.System.OperatingSystem.Name,
                    Environment.OSVersion.VersionString,
                    _config.GetDlnaConfiguration().SendOnlyMatchedHost)
                {
                    LogFunction = (msg) => _logger.LogDebug("{Msg}", msg),
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

        private void RegisterServerEndpoints()
        {
            var udn = CreateUuid(_appHost.SystemId);
            var descriptorUri = "/dlna/" + udn + "/description.xml";

            var bindAddresses = NetworkManager.CreateCollection(
                _networkManager.GetInternalBindAddresses()
                .Where(i => i.AddressFamily == AddressFamily.InterNetwork || (i.AddressFamily == AddressFamily.InterNetworkV6 && i.Address.ScopeId != 0)));

            if (bindAddresses.Count == 0)
            {
                // No interfaces returned, so use loopback.
                bindAddresses = _networkManager.GetLoopbacks();
            }

            foreach (IPNetAddress address in bindAddresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // Not supporting IPv6 right now
                    continue;
                }

                // Limit to LAN addresses only
                if (!_networkManager.IsInLocalNetwork(address))
                {
                    continue;
                }

                var fullService = "urn:schemas-upnp-org:device:MediaServer:1";

                _logger.LogInformation("Registering publisher for {ResourceName} on {DeviceAddress}", fullService, address);

                var uri = new UriBuilder(_appHost.GetApiUrlForLocalAccess(address, false) + descriptorUri);

                var device = new SsdpRootDevice
                {
                    CacheLifetime = TimeSpan.FromSeconds(1800), // How long SSDP clients can cache this info.
                    Location = uri.Uri, // Must point to the URL that serves your devices UPnP description document.
                    Address = address.Address,
                    PrefixLength = address.PrefixLength,
                    FriendlyName = "Jellyfin",
                    Manufacturer = "Jellyfin",
                    ModelName = "Jellyfin Server",
                    Uuid = udn
                    // This must be a globally unique value that survives reboots etc. Get from storage or embedded hardware etc.
                };

                SetProperies(device, fullService);
                _publisher.AddDevice(device);

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

            return guid.ToString("D", CultureInfo.InvariantCulture);
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
                        _httpClientFactory,
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

        public void DisposeDevicePublisher()
        {
            if (_publisher != null)
            {
                _logger.LogInformation("Disposing SsdpDevicePublisher");
                _publisher.Dispose();
                _publisher = null;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            DisposeDevicePublisher();
            DisposePlayToManager();
            DisposeDeviceDiscovery();

            if (_communicationsServer != null)
            {
                _logger.LogInformation("Disposing SsdpCommunicationsServer");
                _communicationsServer.Dispose();
                _communicationsServer = null;
            }

            ContentDirectory = null;
            ConnectionManager = null;
            MediaReceiverRegistrar = null;
            Current = null;

            _disposed = true;
        }
    }
}
