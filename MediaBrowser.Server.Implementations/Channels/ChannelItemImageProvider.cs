using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Server.Implementations.Channels
{
    public class ChannelItemImageProvider : IDynamicImageProvider, IHasItemChangeMonitor
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public ChannelItemImageProvider(IHttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new[] { ImageType.Primary };
        }

        public async Task<DynamicImageResponse> GetImage(IHasImages item, ImageType type, CancellationToken cancellationToken)
        {
            var channelItem = (IChannelItem)item;

            var imageResponse = new DynamicImageResponse();

            if (!string.IsNullOrEmpty(channelItem.ExternalImagePath))
            {
                imageResponse.Path = channelItem.ExternalImagePath;
                imageResponse.Protocol = MediaProtocol.Http;
                imageResponse.HasImage = true;
            }

            return imageResponse;
        }

        public string Name
        {
            get { return "Channel Image Provider"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is IChannelItem;
        }

        public bool HasChanged(IHasMetadata item, MetadataStatus status, IDirectoryService directoryService)
        {
            var channelItem = item as IChannelItem;

            if (channelItem != null)
            {
                return !channelItem.HasImage(ImageType.Primary) && !string.IsNullOrWhiteSpace(channelItem.ExternalImagePath);
            }
            return false;
        }
    }
}
