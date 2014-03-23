using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Dlna.PlayTo
{
    class PlayToManager : IDisposable
    {
        private bool _disposed = false;
        private readonly ILogger _logger;
        private readonly ISessionManager _sessionManager;
        private readonly IHttpClient _httpClient;
        private readonly CancellationTokenSource _tokenSource;
        private ConcurrentDictionary<string, DateTime> _locations;

        private readonly IItemRepository _itemRepository;
        private readonly ILibraryManager _libraryManager;
        private readonly INetworkManager _networkManager;
        private readonly IUserManager _userManager;
        private readonly IDlnaManager _dlnaManager;
        private readonly IServerConfigurationManager _config;
        private readonly IServerApplicationHost _appHost;

        public PlayToManager(ILogger logger, IServerConfigurationManager config, ISessionManager sessionManager, IHttpClient httpClient, IItemRepository itemRepository, ILibraryManager libraryManager, INetworkManager networkManager, IUserManager userManager, IDlnaManager dlnaManager, IServerApplicationHost appHost)
        {
            _locations = new ConcurrentDictionary<string, DateTime>();
            _tokenSource = new CancellationTokenSource();

            _logger = logger;
            _sessionManager = sessionManager;
            _httpClient = httpClient;
            _itemRepository = itemRepository;
            _libraryManager = libraryManager;
            _networkManager = networkManager;
            _userManager = userManager;
            _dlnaManager = dlnaManager;
            _appHost = appHost;
            _config = config;
        }

        public void Start()
        {
            _locations = new ConcurrentDictionary<string, DateTime>();

            foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
            {
                _logger.Debug("Found interface: {0}. Type: {1}. Status: {2}", network.Name, network.NetworkInterfaceType, network.OperationalStatus);

                if (!network.SupportsMulticast || OperationalStatus.Up != network.OperationalStatus || !network.GetIPProperties().MulticastAddresses.Any())
                    continue;

                var ipV4 = network.GetIPProperties().GetIPv4Properties();
                if (null == ipV4)
                    continue;

                IPAddress localIp = null;

                foreach (var ipInfo in network.GetIPProperties().UnicastAddresses)
                {
                    if (ipInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIp = ipInfo.Address;
                        break;
                    }
                }

                if (localIp == null)
                {
                    continue;
                }

                try
                {
                    CreateListener(localIp);
                }
                catch (Exception e)
                {
                    _logger.ErrorException("Failed to Initilize Socket", e);
                }
            }
        }

        public void Stop()
        {
        }

        /// <summary>
        /// Creates a socket for the interface and listends for data.
        /// </summary>
        /// <param name="localIp">The local ip.</param>
        private void CreateListener(IPAddress localIp)
        {
            Task.Factory.StartNew(async (o) =>
            {
                try
                {
                    var socket = GetMulticastSocket();

                    socket.Bind(new IPEndPoint(localIp, 0));

                    _logger.Info("Creating SSDP listener");

                    var receiveBuffer = new byte[64000];

                    CreateNotifier(socket);

                    while (!_tokenSource.IsCancellationRequested)
                    {
                        var receivedBytes = await socket.ReceiveAsync(receiveBuffer, 0, 64000);

                        if (receivedBytes > 0)
                        {
                            var rawData = Encoding.UTF8.GetString(receiveBuffer, 0, receivedBytes);
                            var uri = SsdpHelper.ParseSsdpResponse(rawData);

                            TryCreateController(uri);
                        }
                    }

                    _logger.Info("SSDP listener - Task completed");
                }
                catch (OperationCanceledException c)
                {
                }
                catch (Exception e)
                {
                    _logger.ErrorException("Error in listener", e);
                }

            }, _tokenSource.Token, TaskCreationOptions.LongRunning);
        }

        private void TryCreateController(Uri uri)
        {
            Task.Run(async () =>
            {
                try
                {
                    await CreateController(uri).ConfigureAwait(false);
                }
                catch (OperationCanceledException c)
                {
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error creating play to controller", ex);
                }
            });
        }

        private void CreateNotifier(Socket socket)
        {
            Task.Factory.StartNew(async (o) =>
            {
                try
                {
                    var request = SsdpHelper.CreateRendererSSDP(3);

                    while (true)
                    {
                        socket.SendTo(request, new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900));

                        await Task.Delay(10000).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException c)
                {
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in notifier", ex);
                }

            }, _tokenSource.Token, TaskCreationOptions.LongRunning);

        }

        /// <summary>
        /// Gets a socket configured for SDDP multicasting.
        /// </summary>
        /// <returns></returns>
        private Socket GetMulticastSocket()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse("239.255.255.250")));
            //socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 3);
            return socket;
        }

        /// <summary>
        /// Creates a new DlnaSessionController.
        /// and logs the session in SessionManager
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        private async Task CreateController(Uri uri)
        {
            if (!IsUriValid(uri))
                return;

            var device = await Device.CreateuPnpDeviceAsync(uri, _httpClient, _config, _logger).ConfigureAwait(false);

            if (device != null && device.RendererCommands != null && !_sessionManager.Sessions.Any(s => string.Equals(s.DeviceId, device.Properties.UUID) && s.IsActive))
            {
                GetProfileSettings(device.Properties);

                var sessionInfo = await _sessionManager.LogSessionActivity(device.Properties.ClientType, device.Properties.Name, device.Properties.UUID, device.Properties.DisplayName, uri.OriginalString, null)
                    .ConfigureAwait(false);

                _sessionManager.ReportCapabilities(sessionInfo.Id, new SessionCapabilities
                {
                    PlayableMediaTypes = new[] { MediaType.Audio, MediaType.Video, MediaType.Photo },
                    SupportsFullscreenToggle = false
                });

                var controller = sessionInfo.SessionController as PlayToController;

                if (controller == null)
                {
                    sessionInfo.SessionController = controller = new PlayToController(sessionInfo, _sessionManager, _itemRepository, _libraryManager, _logger, _networkManager, _dlnaManager, _userManager, _appHost);
                }

                controller.Init(device);

                _logger.Info("DLNA Session created for {0} - {1}", device.Properties.Name, device.Properties.ModelName);
            }
        }

        /// <summary>
        /// Gets the profile settings.
        /// </summary>
        /// <param name="deviceProperties">The device properties.</param>
        /// <returns>The TranscodeSettings for the device</returns>
        private void GetProfileSettings(DeviceInfo deviceProperties)
        {
            var profile = _dlnaManager.GetProfile(deviceProperties.ToDeviceIdentification());

            if (!string.IsNullOrWhiteSpace(profile.Name))
            {
                deviceProperties.DisplayName = profile.Name;
            }
            if (!string.IsNullOrWhiteSpace(profile.ClientType))
            {
                deviceProperties.ClientType = profile.ClientType;
            }
        }

        /// <summary>
        /// Determines if the Uri is valid for further inspection or not.
        /// (the limit for reinspection is 5 minutes)
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>Returns <b>True</b> if the Uri is valid for further inspection</returns>
        private bool IsUriValid(Uri uri)
        {
            if (uri == null)
                return false;

            if (!_locations.ContainsKey(uri.OriginalString))
            {
                _locations.AddOrUpdate(uri.OriginalString, DateTime.UtcNow, (key, existingVal) => existingVal);

                return true;
            }

            var time = _locations[uri.OriginalString];

            if ((DateTime.UtcNow - time).TotalMinutes <= 5)
            {
                return false;
            }
            return _locations.TryUpdate(uri.OriginalString, DateTime.UtcNow, time);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _tokenSource.Cancel();
            }
        }
    }
}
