using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Channels
{
    public class ChannelImageProvider : IDynamicImageProvider, IHasItemChangeMonitor
    {
        private readonly IChannelManager _channelManager;

        public ChannelImageProvider(IChannelManager channelManager)
        {
            _channelManager = channelManager;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return GetChannel(item).GetSupportedChannelImages();
        }

        public Task<DynamicImageResponse> GetImage(IHasImages item, ImageType type, CancellationToken cancellationToken)
        {
            var channel = GetChannel(item);

            return channel.GetChannelImage(type, cancellationToken);
        }

        public string Name
        {
            get { return "Channel Image Provider"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is Channel;
        }

        private IChannel GetChannel(IHasImages item)
        {
            var channel = (Channel)item;

            return ((ChannelManager)_channelManager).GetChannelProvider(channel);
        }

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService)
        {
            return GetSupportedImages(item).Any(i => !item.HasImage(i));
        }
    }
}
