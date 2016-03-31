using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Omdb
{
    public class OmdbImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClient _httpClient;

        public OmdbImageProvider(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType>
            {
                ImageType.Primary
            };
        }

        public Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken)
        {
            var imdbId = item.GetProviderId(MetadataProviders.Imdb);

            var list = new List<RemoteImageInfo>();

            if (!string.IsNullOrWhiteSpace(imdbId))
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = string.Format("http://img.omdbapi.com/?i={0}&apikey=82e83907", imdbId)
                });
            }

            return Task.FromResult<IEnumerable<RemoteImageInfo>>(list);
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = OmdbProvider.ResourcePool

            });
        }

        public string Name
        {
            get { return "The Open Movie Database"; }
        }

        public bool Supports(IHasImages item)
        {
            // We'll hammer Omdb if we enable this
            if (item is Person)
            {
                return false;
            }

            // Save the http requests since we know it's not currently supported
            if (item is Season || item is Episode)
            {
                return false;
            }

            // Supports images for tv movies
            var tvProgram = item as LiveTvProgram;
            if (tvProgram != null && tvProgram.IsMovie)
            {
                return true;
            }

            return item is Movie || item is Trailer;
        }

        public int Order
        {
            get
            {
                // After other internet providers, because they're better
                // But before fallback providers like screengrab
                return 90;
            }
        }
    }
}
