using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.LiveTv
{
    public class ChannelImageProvider : IDynamicImageProvider, IHasItemChangeMonitor
    {
        private readonly ILiveTvManager _liveTvManager;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IApplicationHost _appHost;

        public ChannelImageProvider(ILiveTvManager liveTvManager, IHttpClient httpClient, ILogger logger, IApplicationHost appHost)
        {
            _liveTvManager = liveTvManager;
            _httpClient = httpClient;
            _logger = logger;
            _appHost = appHost;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new[] { ImageType.Primary };
        }

        public async Task<DynamicImageResponse> GetImage(IHasImages item, ImageType type, CancellationToken cancellationToken)
        {
            var liveTvItem = (LiveTvChannel)item;

            var imageResponse = new DynamicImageResponse();

            var service = _liveTvManager.Services.FirstOrDefault(i => string.Equals(i.Name, liveTvItem.ServiceName, StringComparison.OrdinalIgnoreCase));

            if (service != null && !item.HasImage(ImageType.Primary))
            {
                try
                {
                    var response = await service.GetChannelImageAsync(liveTvItem.ExternalId, cancellationToken).ConfigureAwait(false);

                    if (response != null)
                    {
                        imageResponse.HasImage = true;
                        imageResponse.Stream = response.Stream;
                        imageResponse.Format = response.Format;
                    }
                }
                catch (NotImplementedException)
                {
                }
            }

            return imageResponse;
        }

        public string Name
        {
            get { return "Live TV Service Provider"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is LiveTvChannel;
        }

        public int Order
        {
            get { return 0; }
        }

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService)
        {
            return GetSupportedImages(item).Any(i => !item.HasImage(i));
        }
    }
}
