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
using Jellyfin.Networking.Extensions;
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
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Globalization;
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
        private readonly ISsdpCommunicationsServer _communicationsServer;
        private readonly INetworkManager _networkManager;
        private readonly object _syncLock = new();
        private readonly bool _disabled;

        private PlayToManager _manager;
        private SsdpDevicePublisher _publisher;

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
            ISsdpCommunicationsServer communicationsServer,
            INetworkManager networkManager)
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
            _communicationsServer = communicationsServer;
            _networkManager = networkManager;
            _logger = loggerFactory.CreateLogger<DlnaEntryPoint>();

            var netConfig = config.GetConfiguration<NetworkConfiguration>(NetworkConfigurationStore.StoreKey);
            _disabled = appHost.ListenWithHttps && netConfig.RequireHttps;

            if (_disabled && _config.GetDlnaConfiguration().EnableServer)
            {
                _logger.LogError("The DLNA specification does not support HTTPS.");
            }
        }

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
            StartDeviceDiscovery();

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

        private void StartDeviceDiscovery()
        {
            try
            {
                ((DeviceDiscovery)_deviceDiscovery).Start(_communicationsServer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting device discovery");
            }
        }

        public void StartDevicePublisher(Configuration.DlnaOptions options)
        {
            if (_publisher is not null)
            {
                return;
            }

            try
            {
                _publisher = new SsdpDevicePublisher(
                    _communicationsServer,
                    Environment.OSVersion.Platform.ToString(),
                    // Can not use VersionString here since that includes OS and version
                    Environment.OSVersion.Version.ToString(),
                    _config.GetDlnaConfiguration().SendOnlyMatchedHost)
                {
                    LogFunction = (msg) => _logger.LogDebug("{Msg}", msg),
                    SupportPnpRootDevice = false
                };

                RegisterServerEndpoints();

                if (options.BlastAliveMessages)
                {
                    _publisher.StartSendingAliveNotifications(TimeSpan.FromSeconds(options.BlastAliveMessageIntervalSeconds));
                }
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

            // Only get bind addresses in LAN
            // IPv6 is currently unsupported
            var validInterfaces = _networkManager.GetInternalBindAddresses()
                .Where(x => x.Address is not null)
                .Where(x => x.AddressFamily != AddressFamily.InterNetworkV6)
                .ToList();

            if (validInterfaces.Count == 0)
            {
                // No interfaces returned, fall back to loopback
                validInterfaces = _networkManager.GetLoopbacks().ToList();
            }

            foreach (var intf in validInterfaces)
            {
                var fullService = "urn:schemas-upnp-org:device:MediaServer:1";

                _logger.LogInformation("Registering publisher for {ResourceName} on {DeviceAddress}", fullService, intf.Address);

                var uri = new UriBuilder(_appHost.GetApiUrlForLocalAccess(intf.Address, false) + descriptorUri);

                var device = new SsdpRootDevice
                {
                    CacheLifetime = TimeSpan.FromSeconds(1800), // How long SSDP clients can cache this info.
                    Location = uri.Uri, // Must point to the URL that serves your devices UPnP description document.
                    Address = intf.Address,
                    PrefixLength = NetworkExtensions.MaskToCidr(intf.Subnet.Prefix),
                    FriendlyName = "Jellyfin",
                    Manufacturer = "Jellyfin",
                    ModelName = "Jellyfin Server",
                    Uuid = udn
                    // This must be a globally unique value that survives reboots etc. Get from storage or embedded hardware etc.
                };

                SetProperties(device, fullService);
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

                    SetProperties(embeddedDevice, subDevice);
                    device.AddDevice(embeddedDevice);
                }
            }
        }

        private static string CreateUuid(string text)
        {
            if (!Guid.TryParse(text, out var guid))
            {
                guid = text.GetMD5();
            }

            return guid.ToString("D", CultureInfo.InvariantCulture);
        }

        private static void SetProperties(SsdpDevice device, string fullDeviceType)
        {
            var serviceParts = fullDeviceType
                .Replace("urn:", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(":1", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Split(':');

            device.DeviceTypeNamespace = serviceParts[0].Replace('.', '-');
            device.DeviceClass = serviceParts[1];
            device.DeviceType = serviceParts[2];
        }

        private void StartPlayToManager()
        {
            lock (_syncLock)
            {
                if (_manager is not null)
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
                if (_manager is not null)
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
            if (_publisher is not null)
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
            _disposed = true;
        }
    }
}
