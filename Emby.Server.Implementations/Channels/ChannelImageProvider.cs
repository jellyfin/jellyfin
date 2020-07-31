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
    /// <summary>
    /// An image provider for channels.
    /// </summary>
    public class ChannelImageProvider : IDynamicImageProvider, IHasItemChangeMonitor
    {
        private readonly IChannelManager _channelManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelImageProvider"/> class.
        /// </summary>
        /// <param name="channelManager">The channel manager.</param>
        public ChannelImageProvider(IChannelManager channelManager)
        {
            _channelManager = channelManager;
        }

        /// <inheritdoc />
        public string Name => "Channel Image Provider";

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return GetChannel(item).GetSupportedChannelImages();
        }

        /// <inheritdoc />
        public Task<DynamicImageResponse> GetImage(BaseItem item, ImageType type, CancellationToken cancellationToken)
        {
            var channel = GetChannel(item);

            return channel.GetChannelImage(type, cancellationToken);
        }

        /// <inheritdoc />
        public bool Supports(BaseItem item)
        {
            return item is Channel;
        }

        private IChannel GetChannel(BaseItem item)
        {
            var channel = (Channel)item;

            return ((ChannelManager)_channelManager).GetChannelProvider(channel);
        }

        /// <inheritdoc />
        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
        {
            return GetSupportedImages(item).Any(i => !item.HasImage(i));
        }
    }
}
