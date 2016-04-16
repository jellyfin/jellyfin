using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Omdb;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.TV
{
    class OmdbEpisodeProvider :
            IRemoteMetadataProvider<Episode, EpisodeInfo>,
            IHasOrder
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        private OmdbItemProvider _itemProvider;

        public OmdbEpisodeProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogger logger, ILibraryManager libraryManager)
        {
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            _itemProvider = new OmdbItemProvider(jsonSerializer, httpClient, logger, libraryManager);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            return _itemProvider.GetSearchResults(searchInfo, "episode", cancellationToken);
        }

        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Episode>
            {
                Item = new Episode()
            };

            var imdbId = info.GetProviderId(MetadataProviders.Imdb);
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                imdbId = await GetEpisodeImdbId(info, cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(imdbId))
            {
                result.Item.SetProviderId(MetadataProviders.Imdb, imdbId);
                result.HasMetadata = true;

                await new OmdbProvider(_jsonSerializer, _httpClient).Fetch(result.Item, imdbId, info.MetadataLanguage, info.MetadataCountryCode, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        private async Task<string> GetEpisodeImdbId(EpisodeInfo info, CancellationToken cancellationToken)
        {
            var results = await GetSearchResults(info, cancellationToken).ConfigureAwait(false);
            var first = results.FirstOrDefault();
            return first == null ? null : first.GetProviderId(MetadataProviders.Imdb);
        }

        public int Order
        {
            get
            {
                // After TheTvDb
                return 1;
            }
        }

        public string Name
        {
            get { return "The Open Movie Database"; }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _itemProvider.GetImageResponse(url, cancellationToken);
        }
    }
}
