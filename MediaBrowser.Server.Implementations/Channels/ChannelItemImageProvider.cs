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

namespace MediaBrowser.Server.Implementations.Channels
{
    public class ChannelItemImageProvider : IDynamicImageProvider, IHasChangeMonitor
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

            if (!string.IsNullOrEmpty(channelItem.OriginalImageUrl))
            {
                var options = new HttpRequestOptions
                {
                    CancellationToken = cancellationToken,
                    Url = channelItem.OriginalImageUrl
                };

                var response = await _httpClient.GetResponse(options).ConfigureAwait(false);

                if (response.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    imageResponse.HasImage = true;
                    imageResponse.Stream = response.Content;
                    imageResponse.SetFormatFromMimeType(response.ContentType);
                }
                else
                {
                    _logger.Error("Provider did not return an image content type.");
                }
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

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService, DateTime date)
        {
            var channelItem = item as IChannelItem;

            if (channelItem != null)
            {
                return !channelItem.HasImage(ImageType.Primary) && !string.IsNullOrWhiteSpace(channelItem.OriginalImageUrl);
            }
            return false;
        }
    }
}
