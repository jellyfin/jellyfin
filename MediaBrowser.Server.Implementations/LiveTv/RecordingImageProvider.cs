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
    public class RecordingImageProvider : IDynamicImageProvider, IHasChangeMonitor
    {
        private readonly ILiveTvManager _liveTvManager;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public RecordingImageProvider(ILiveTvManager liveTvManager, IHttpClient httpClient, ILogger logger)
        {
            _liveTvManager = liveTvManager;
            _httpClient = httpClient;
            _logger = logger;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new[] { ImageType.Primary };
        }

        public async Task<DynamicImageResponse> GetImage(IHasImages item, ImageType type, CancellationToken cancellationToken)
        {
            var liveTvItem = (ILiveTvRecording)item;

            var imageResponse = new DynamicImageResponse();

            if (!string.IsNullOrEmpty(liveTvItem.RecordingInfo.ImagePath))
            {
                imageResponse.Path = liveTvItem.RecordingInfo.ImagePath;
                imageResponse.HasImage = true;
            }
            else if (!string.IsNullOrEmpty(liveTvItem.RecordingInfo.ImageUrl))
            {
                var options = new HttpRequestOptions
                {
                    CancellationToken = cancellationToken,
                    Url = liveTvItem.RecordingInfo.ImageUrl
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
            else if (liveTvItem.RecordingInfo.HasImage ?? true)
            {
                var service = _liveTvManager.Services.FirstOrDefault(i => string.Equals(i.Name, liveTvItem.ServiceName, StringComparison.OrdinalIgnoreCase));

                if (service != null)
                {
                    try
                    {
                        var response = await service.GetRecordingImageAsync(liveTvItem.RecordingInfo.Id, cancellationToken).ConfigureAwait(false);

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
            }

            return imageResponse;
        }

        public string Name
        {
            get { return "Live TV Service Provider"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is ILiveTvRecording;
        }

        public int Order
        {
            get { return 0; }
        }

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService, DateTime date)
        {
            return !item.HasImage(ImageType.Primary) && (DateTime.UtcNow - date).TotalHours >= 3;
        }
    }
}
