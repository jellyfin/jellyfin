using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken)
        {
            var imdbId = item.GetProviderId(MetadataProviders.Imdb);

            var list = new List<RemoteImageInfo>();

            if (!string.IsNullOrWhiteSpace(imdbId) && OmdbProvider.Current != null)
            {
                OmdbProvider.RootObject rootObject = await OmdbProvider.Current.GetRootObject(imdbId, cancellationToken);

                if (!string.IsNullOrEmpty(rootObject.Poster))
                {
                    list.Add(new RemoteImageInfo
                    {
                        ProviderName = Name,
                        Url = string.Format("https://img.omdbapi.com/?i={0}&apikey=82e83907", imdbId)
                    });
                }
            }

            return list;
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
            // Supports images for tv movies
            var tvProgram = item as LiveTvProgram;
            if (tvProgram != null && tvProgram.IsMovie)
            {
                return true;
            }

            return item is Movie || item is Trailer || item is Episode;
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
