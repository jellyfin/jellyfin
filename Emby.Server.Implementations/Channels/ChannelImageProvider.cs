#pragma warning disable CS1591
#pragma warning disable SA1600

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Emby.Server.Implementations.Channels
{
    public class ChannelImageProvider : IDynamicImageProvider, IHasItemChangeMonitor
    {
        private readonly IChannelManager _channelManager;

        public ChannelImageProvider(IChannelManager channelManager)
        {
            _channelManager = channelManager;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return GetChannel(item).GetSupportedChannelImages();
        }

        public Task<DynamicImageResponse> GetImage(BaseItem item, ImageType type, CancellationToken cancellationToken)
        {
            var channel = GetChannel(item);

            return channel.GetChannelImage(type, cancellationToken);
        }

        public string Name => "Channel Image Provider";

        public bool Supports(BaseItem item)
        {
            return item is Channel;
        }

        private IChannel GetChannel(BaseItem item)
        {
            var channel = (Channel)item;

            return ((ChannelManager)_channelManager).GetChannelProvider(channel);
        }

        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
        {
            return GetSupportedImages(item).Any(i => !item.HasImage(i));
        }
    }
}
