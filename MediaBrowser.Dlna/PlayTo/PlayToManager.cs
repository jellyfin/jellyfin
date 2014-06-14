using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Session;
using MediaBrowser.Dlna.Ssdp;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;
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
        private bool _disposed;
        private readonly ILogger _logger;
        private readonly ISessionManager _sessionManager;
        private readonly IHttpClient _httpClient;
        private readonly CancellationTokenSource _tokenSource;

        private readonly IItemRepository _itemRepository;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDlnaManager _dlnaManager;
        private readonly IServerConfigurationManager _config;
        private readonly IServerApplicationHost _appHost;
        private readonly IImageProcessor _imageProcessor;

        private readonly SsdpHandler _ssdpHandler;
        
        public PlayToManager(ILogger logger, IServerConfigurationManager config, ISessionManager sessionManager, IHttpClient httpClient, IItemRepository itemRepository, ILibraryManager libraryManager, IUserManager userManager, IDlnaManager dlnaManager, IServerApplicationHost appHost, IImageProcessor imageProcessor, SsdpHandler ssdpHandler)
        {
            _tokenSource = new CancellationTokenSource();

            _logger = logger;
            _sessionManager = sessionManager;
            _httpClient = httpClient;
            _itemRepository = itemRepository;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dlnaManager = dlnaManager;
            _appHost = appHost;
            _imageProcessor = imageProcessor;
            _ssdpHandler = ssdpHandler;
            _config = config;
        }

        public void Start()
        {
            foreach (var network in GetNetworkInterfaces())
            {
                _logger.Debug("Found interface: {0}. Type: {1}. Status: {2}", network.Name, network.NetworkInterfaceType, network.OperationalStatus);

                if (!network.SupportsMulticast || OperationalStatus.Up != network.OperationalStatus || !network.GetIPProperties().MulticastAddresses.Any())
                    continue;

                var ipV4 = network.GetIPProperties().GetIPv4Properties();
                if (null == ipV4)
                    continue;

                var localIp = network.GetIPProperties().UnicastAddresses
                    .Where(i => i.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(i => i.Address)
                    .FirstOrDefault();

                if (localIp != null)
                {
                    try
                    {
                        CreateListener(localIp, ipV4.Index);
                    }
                    catch (Exception e)
                    {
                        _logger.ErrorException("Failed to Initilize Socket", e);
                    }
                }
            }
        }

        private IEnumerable<NetworkInterface> GetNetworkInterfaces()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in GetAllNetworkInterfaces", ex);
                return new List<NetworkInterface>();
            }
        }

        /// <summary>
        /// Creates a socket for the interface and listends for data.
        /// </summary>
        /// <param name="localIp">The local ip.</param>
        /// <param name="networkInterfaceIndex">Index of the network interface.</param>
        private void CreateListener(IPAddress localIp, int networkInterfaceIndex)
        {
            Task.Factory.StartNew(async (o) =>
            {
                try
                {
                    var socket = GetMulticastSocket(networkInterfaceIndex);

                    var endPoint = new IPEndPoint(localIp, 1900);

                    socket.Bind(endPoint);

                    _logger.Info("Creating SSDP listener");

                    var receiveBuffer = new byte[64000];

                    CreateNotifier(socket);

                    while (!_tokenSource.IsCancellationRequested)
                    {
                        var receivedBytes = await socket.ReceiveAsync(receiveBuffer, 0, 64000);

                        if (receivedBytes > 0)
                        {
                            var args = SsdpHelper.ParseSsdpResponse(receiveBuffer, endPoint);

                            TryCreateController(args, localIp);
                        }
                    }

                    _logger.Info("SSDP listener - Task completed");
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    _logger.ErrorException("Error in listener", e);
                }

            }, _tokenSource.Token, TaskCreationOptions.LongRunning);
        }

        private void TryCreateController(SsdpMessageEventArgs args, IPAddress localIp)
        {
            string nts;
            args.Headers.TryGetValue("NTS", out nts);

            string usn;
            if (!args.Headers.TryGetValue("USN", out usn)) usn = string.Empty;

            string nt;
            if (!args.Headers.TryGetValue("NT", out nt)) nt = string.Empty;

            // Don't create a new controller when a device is indicating it's shutting down
            if (string.Equals(nts, "ssdp:byebye", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // It has to report that it's a media renderer
            if (usn.IndexOf("MediaRenderer:", StringComparison.OrdinalIgnoreCase) == -1 &&
                     nt.IndexOf("MediaRenderer:", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return;
            }
            
            // Need to be able to download device description
            string location;
            if (!args.Headers.TryGetValue("Location", out location) ||
                string.IsNullOrEmpty(location))
            {
                return;
            }

            if (_config.Configuration.DlnaOptions.EnableDebugLogging)
            {
                var headerTexts = args.Headers.Select(i => string.Format("{0}={1}", i.Key, i.Value));
                var headerText = string.Join(",", headerTexts.ToArray());

                _logger.Debug("{0} PlayTo message received from {1}. Headers: {2}", args.Method, args.EndPoint, headerText);
            }

            if (_sessionManager.Sessions.Any(i => usn.IndexOf(i.DeviceId, StringComparison.OrdinalIgnoreCase) != -1))
            {
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    await CreateController(new Uri(location), localIp).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
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
                    var msg = new SsdpMessageBuilder().BuildRendererDiscoveryMessage();
                    var request = Encoding.UTF8.GetBytes(msg);

                    while (true)
                    {
                        socket.SendTo(request, new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900));

                        var delay = _config.Configuration.DlnaOptions.ClientDiscoveryIntervalSeconds * 1000;

                        await Task.Delay(delay).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
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
        private Socket GetMulticastSocket(int networkInterfaceIndex)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse("239.255.255.250"), networkInterfaceIndex));
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 4);
            return socket;
        }

        /// <summary>
        /// Creates a new DlnaSessionController.
        /// and logs the session in SessionManager
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        private async Task CreateController(Uri uri, IPAddress localIp)
        {
            var device = await Device.CreateuPnpDeviceAsync(uri, _httpClient, _config, _logger).ConfigureAwait(false);

            if (device != null && device.RendererCommands != null)
            {
                var sessionInfo = await _sessionManager.LogSessionActivity(device.Properties.ClientType, _appHost.ApplicationVersion.ToString(), device.Properties.UUID, device.Properties.Name, uri.OriginalString, null)
                    .ConfigureAwait(false);

                var controller = sessionInfo.SessionController as PlayToController;

                if (controller == null)
                {
                    var serverAddress = GetServerAddress(localIp);

                    sessionInfo.SessionController = controller = new PlayToController(sessionInfo, 
                        _sessionManager, 
                        _itemRepository, 
                        _libraryManager, 
                        _logger, 
                        _dlnaManager, 
                        _userManager, 
                        _imageProcessor, 
                        _ssdpHandler,
                        serverAddress);

                    controller.Init(device);

                    var profile = _dlnaManager.GetProfile(device.Properties.ToDeviceIdentification()) ??
                                  _dlnaManager.GetDefaultProfile();

                    _sessionManager.ReportCapabilities(sessionInfo.Id, new SessionCapabilities
                    {
                        PlayableMediaTypes = profile.GetSupportedMediaTypes(),

                        SupportedCommands = new List<string>
                        {
                            GeneralCommandType.VolumeDown.ToString(),
                            GeneralCommandType.VolumeUp.ToString(),
                            GeneralCommandType.Mute.ToString(),
                            GeneralCommandType.Unmute.ToString(),
                            GeneralCommandType.ToggleMute.ToString(),
                            GeneralCommandType.SetVolume.ToString()
                        },

                        SupportsMediaControl = true
                    });

                    _logger.Info("DLNA Session created for {0} - {1}", device.Properties.Name, device.Properties.ModelName);
                }
            }
        }

        private string GetServerAddress(IPAddress localIp)
        {
            return string.Format("{0}://{1}:{2}/mediabrowser",

                "http",
                localIp,
                _appHost.HttpServerPort
                );
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
