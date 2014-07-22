using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Dlna.PlayTo;
using MediaBrowser.Dlna.Ssdp;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Dlna.Channels
{
    public class DlnaChannelFactory : IChannelFactory, IDisposable
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        
        private DeviceDiscovery _deviceDiscovery;

        private readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);
        private List<Device> _servers = new List<Device>();

        public static DlnaChannelFactory Instance;

        private Func<List<string>> _localServersLookup;

        public DlnaChannelFactory(IServerConfigurationManager config, IHttpClient httpClient, ILogger logger)
        {
            _config = config;
            _httpClient = httpClient;
            _logger = logger;
            Instance = this;
        }

        internal void Start(DeviceDiscovery deviceDiscovery, Func<List<string>> localServersLookup)
        {
            _localServersLookup = localServersLookup;

            _deviceDiscovery = deviceDiscovery;
            deviceDiscovery.DeviceDiscovered += deviceDiscovery_DeviceDiscovered;
            deviceDiscovery.DeviceLeft += deviceDiscovery_DeviceLeft;
        }

        async void deviceDiscovery_DeviceDiscovered(object sender, SsdpMessageEventArgs e)
        {
            string usn;
            if (!e.Headers.TryGetValue("USN", out usn)) usn = string.Empty;

            string nt;
            if (!e.Headers.TryGetValue("NT", out nt)) nt = string.Empty;

            string location;
            if (!e.Headers.TryGetValue("Location", out location)) location = string.Empty;

            if (!IsValid(nt, usn))
            {
                return;
            }

            if (_localServersLookup != null)
            {
                if (_localServersLookup().Any(i => usn.IndexOf(i, StringComparison.OrdinalIgnoreCase) != -1))
                {
                    // Don't add the local Dlna server to this
                    return;
                }
            }

            if (GetExistingServers(usn).Any())
            {
                return;
            }

            await _syncLock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (GetExistingServers(usn).Any())
                {
                    return;
                }

                var device = await Device.CreateuPnpDeviceAsync(new Uri(location), _httpClient, _config, _logger)
                            .ConfigureAwait(false);

                if (!_servers.Any(i => string.Equals(i.Properties.UUID, device.Properties.UUID, StringComparison.OrdinalIgnoreCase)))
                {
                    _servers.Add(device);
                }
            }
            catch (Exception ex)
            {
                
            }
            finally
            {
                _syncLock.Release();
            }
        }

        async void deviceDiscovery_DeviceLeft(object sender, SsdpMessageEventArgs e)
        {
            string usn;
            if (!e.Headers.TryGetValue("USN", out usn)) usn = String.Empty;

            string nt;
            if (!e.Headers.TryGetValue("NT", out nt)) nt = String.Empty;

            if (!IsValid(nt, usn))
            {
                return;
            }

            if (!GetExistingServers(usn).Any())
            {
                return;
            }

            await _syncLock.WaitAsync().ConfigureAwait(false);
            
            try
            {
                var list = _servers.ToList();

                foreach (var device in GetExistingServers(usn).ToList())
                {
                    list.Remove(device);
                }

                _servers = list;
            }
            finally
            {
                _syncLock.Release();
            }
        }

        private bool IsValid(string nt, string usn)
        {
            // It has to report that it's a media renderer
            if (usn.IndexOf("ContentDirectory:", StringComparison.OrdinalIgnoreCase) == -1 &&
                     nt.IndexOf("ContentDirectory:", StringComparison.OrdinalIgnoreCase) == -1 &&
                     usn.IndexOf("MediaServer:", StringComparison.OrdinalIgnoreCase) == -1 &&
                     nt.IndexOf("MediaServer:", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return false;
            }

            return true;
        }

        private IEnumerable<Device> GetExistingServers(string usn)
        {
            return _servers
                .Where(i => usn.IndexOf(i.Properties.UUID, StringComparison.OrdinalIgnoreCase) != -1);
        }

        public IEnumerable<IChannel> GetChannels()
        {
            return _servers.Select(i => new ServerChannel(i)).ToList();
        }

        public void Dispose()
        {
            if (_deviceDiscovery != null)
            {
                _deviceDiscovery.DeviceDiscovered -= deviceDiscovery_DeviceDiscovered;
                _deviceDiscovery.DeviceLeft -= deviceDiscovery_DeviceLeft;
            }
        }
    }

    public class ServerChannel : IChannel, IFactoryChannel
    {
        private readonly Device _device;

        public ServerChannel(Device device)
        {
            _device = device;
        }

        public string Name
        {
            get { return _device.Properties.Name; }
        }

        public string Description
        {
            get { return _device.Properties.ModelDescription; }
        }

        public string DataVersion
        {
            get { return "1"; }
        }

        public string HomePageUrl
        {
            get { return _device.Properties.ModelUrl; }
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                ContentTypes = new List<ChannelMediaContentType>
                {
                    ChannelMediaContentType.Song,
                    ChannelMediaContentType.Clip
                },

                MediaTypes = new List<ChannelMediaType>
                {
                    ChannelMediaType.Audio,
                    ChannelMediaType.Video,
                    ChannelMediaType.Photo
                }
            };
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            // TODO: Implement
            return Task.FromResult(new ChannelItemResult());
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            // TODO: Implement
            return Task.FromResult(new DynamicImageResponse
            {
                HasImage = false
            });
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType>
            {
                ImageType.Primary
            };
        }
    }
}
