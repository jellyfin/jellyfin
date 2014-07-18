using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Dlna.PlayTo;
using MediaBrowser.Dlna.Ssdp;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Dlna.Channels
{
    public class DlnaChannelFactory : IChannelFactory, IDisposable
    {
        private DeviceDiscovery _deviceDiscovery;

        private readonly object _syncLock = new object();
        private readonly List<Device> _servers = new List<Device>();

        public static DlnaChannelFactory Instance;

        public DlnaChannelFactory()
        {
            Instance = this;
        }

        internal void Start(DeviceDiscovery deviceDiscovery)
        {
            _deviceDiscovery = deviceDiscovery;
            deviceDiscovery.DeviceDiscovered += deviceDiscovery_DeviceDiscovered;
        }

        void deviceDiscovery_DeviceDiscovered(object sender, GenericEventArgs<DeviceDiscoveryInfo> e)
        {
            var usn = e.Argument.Usn;
            var nt = e.Argument.Nt;

            // It has to report that it's a media renderer
            if (usn.IndexOf("ContentDirectory:", StringComparison.OrdinalIgnoreCase) == -1 &&
                     nt.IndexOf("ContentDirectory:", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return;
            }

            if (_servers.Any(i => usn.IndexOf(i.Properties.UUID, StringComparison.OrdinalIgnoreCase) != -1))
            {
                return;
            }

            lock (_syncLock)
            {
                _servers.Add(e.Argument.Device);
            }
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
                 
            };
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        public Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ChannelItemResult());
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            return Task.FromResult(new DynamicImageResponse());
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType>();
        }
    }
}
