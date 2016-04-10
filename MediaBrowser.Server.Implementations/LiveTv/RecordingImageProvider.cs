using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.LiveTv
{
    public class RecordingImageProvider : IDynamicImageProvider, IHasItemChangeMonitor
    {
        private readonly ILiveTvManager _liveTvManager;

        public RecordingImageProvider(ILiveTvManager liveTvManager)
        {
            _liveTvManager = liveTvManager;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new[] { ImageType.Primary };
        }

        public async Task<DynamicImageResponse> GetImage(IHasImages item, ImageType type, CancellationToken cancellationToken)
        {
            var liveTvItem = (ILiveTvRecording)item;

            var imageResponse = new DynamicImageResponse();

            var service = _liveTvManager.Services.FirstOrDefault(i => string.Equals(i.Name, liveTvItem.ServiceName, StringComparison.OrdinalIgnoreCase));

            if (service != null)
            {
                try
                {
                    var response = await service.GetRecordingImageAsync(liveTvItem.ExternalId, cancellationToken).ConfigureAwait(false);

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
            return item is ILiveTvRecording;
        }

        public int Order
        {
            get { return 0; }
        }

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService)
        {
            var liveTvItem = item as ILiveTvRecording;

            if (liveTvItem != null)
            {
                return !liveTvItem.HasImage(ImageType.Primary);
            }
            return false;
        }
    }
}
