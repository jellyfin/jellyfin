#pragma warning disable CS1591

using System;
using System.Globalization;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Emby.Dlna.PlayTo;
using Emby.Dlna.Ssdp;
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
        private readonly ILogger _logger;
        private readonly IServerApplicationHost _appHost;

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

        private SsdpDevicePublisher _Publisher;

        private readonly ISocketFactory _socketFactory;
        private readonly INetworkManager _networkManager;

        private ISsdpCommunicationsServer _communicationsServer;

        internal IContentDirectory ContentDirectory { get; private set; }

        internal IConnectionManager ConnectionManager { get; private set; }

        internal IMediaReceiverRegistrar MediaReceiverRegistrar { get; private set; }

        public static DlnaEntryPoint Current;

        public DlnaEntryPoint(IServerConfigurationManager config,
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
            ISocketFactory socketFactory,
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
            _socketFactory = socketFactory;
            _networkManager = networkManager;
            _logger = loggerFactory.CreateLogger("Dlna");

            ContentDirectory = new ContentDirectory.ContentDirectory(
                dlnaManager,
                userDataManager,
                imageProcessor,
                libraryManager,
                config,
                userManager,
                loggerFactory.CreateLogger<ContentDirectory.ContentDirectory>(),
                httpClient,
                localizationManager,
                mediaSourceManager,
                userViewManager,
                mediaEncoder,
                tvSeriesManager);

            ConnectionManager = new ConnectionManager.ConnectionManager(
                dlnaManager,
                config,
                loggerFactory.CreateLogger<ConnectionManager.ConnectionManager>(),
                httpClient);

            MediaReceiverRegistrar = new MediaReceiverRegistrar.MediaReceiverRegistrar(
                loggerFactory.CreateLogger<MediaReceiverRegistrar.MediaReceiverRegistrar>(),
                httpClient,
                config);
            Current = this;
        }

        public async Task RunAsync()
        {
            await ((DlnaManager)_dlnaManager).InitProfilesAsync().ConfigureAwait(false);

            await ReloadComponents().ConfigureAwait(false);

            _config.NamedConfigurationUpdated += OnNamedConfigurationUpdated;
        }

        private async void OnNamedConfigurationUpdated(object sender, ConfigurationUpdateEventArgs e)
        {
            if (string.Equals(e.Key, "dlna", StringComparison.OrdinalIgnoreCase))
            {
                await ReloadComponents().ConfigureAwait(false);
            }
        }

        private async Task ReloadComponents()
        {
            var options = _config.GetDlnaConfiguration();

            StartSsdpHandler();

            if (options.EnableServer)
            {
                await StartDevicePublisher(options).ConfigureAwait(false);
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
                    var enableMultiSocketBinding = OperatingSystem.Id == OperatingSystemId.Windows ||
                                                   OperatingSystem.Id == OperatingSystemId.Linux;

                    _communicationsServer = new SsdpCommunicationsServer(_config, _socketFactory, _networkManager, _logger, enableMultiSocketBinding)
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

        private void LogMessage(string msg)
        {
            _logger.LogDebug(msg);
        }

        private void StartDeviceDiscovery(ISsdpCommunicationsServer communicationsServer)
        {
            try
            {
                ((DeviceDiscovery)_deviceDiscovery).Start(communicationsServer);
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

        public async Task StartDevicePublisher(Configuration.DlnaOptions options)
        {
            if (!options.BlastAliveMessages)
            {
                return;
            }

            if (_Publisher != null)
            {
                return;
            }

            try
            {
                _Publisher = new SsdpDevicePublisher(_communicationsServer, _networkManager, OperatingSystem.Name, Environment.OSVersion.VersionString, _config.GetDlnaConfiguration().SendOnlyMatchedHost);
                _Publisher.LogFunction = LogMessage;
                _Publisher.SupportPnpRootDevice = false;

                await RegisterServerEndpoints().ConfigureAwait(false);

                _Publisher.StartBroadcastingAliveMessages(TimeSpan.FromSeconds(options.BlastAliveMessageIntervalSeconds));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering endpoint");
            }
        }

        private async Task RegisterServerEndpoints()
        {
            var addresses = await _appHost.GetLocalIpAddresses(CancellationToken.None).ConfigureAwait(false);

            var udn = CreateUuid(_appHost.SystemId);

            foreach (var address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // Not supporting IPv6 right now
                    continue;
                }

                var fullService = "urn:schemas-upnp-org:device:MediaServer:1";

                _logger.LogInformation("Registering publisher for {0} on {1}", fullService, address);

                var descriptorUri = "/dlna/" + udn + "/description.xml";
                var uri = new Uri(_appHost.GetLocalApiUrl(address) + descriptorUri);

                var device = new SsdpRootDevice
                {
                    CacheLifetime = TimeSpan.FromSeconds(1800), //How long SSDP clients can cache this info.
                    Location = uri, // Must point to the URL that serves your devices UPnP description document.
                    Address = address,
                    SubnetMask = _networkManager.GetLocalIpSubnetMask(address),
                    FriendlyName = "Jellyfin",
                    Manufacturer = "Jellyfin",
                    ModelName = "Jellyfin Server",
                    Uuid = udn
                    // This must be a globally unique value that survives reboots etc. Get from storage or embedded hardware etc.
                };

                SetProperies(device, fullService);
                _Publisher.AddDevice(device);

                var embeddedDevices = new[]
                {
                    "urn:schemas-upnp-org:service:ContentDirectory:1",
                    "urn:schemas-upnp-org:service:ConnectionManager:1",
                    //"urn:microsoft.com:service:X_MS_MediaReceiverRegistrar:1"
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
                if (_manager != null)
                {
                    return;
                }

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
        }

        public void DisposeDevicePublisher()
        {
            if (_Publisher != null)
            {
                _logger.LogInformation("Disposing SsdpDevicePublisher");
                _Publisher.Dispose();
                _Publisher = null;
            }
        }
    }
}
