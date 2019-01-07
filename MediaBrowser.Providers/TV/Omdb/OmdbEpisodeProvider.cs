using MediaBrowser.Model.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Omdb;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;

namespace MediaBrowser.Providers.TV
{
    class OmdbEpisodeProvider :
            IRemoteMetadataProvider<Episode, EpisodeInfo>,
            IHasOrder
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        private readonly OmdbItemProvider _itemProvider;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IApplicationHost _appHost;

        public OmdbEpisodeProvider(IJsonSerializer jsonSerializer, IApplicationHost appHost, IHttpClient httpClient, ILogger logger, ILibraryManager libraryManager, IFileSystem fileSystem, IServerConfigurationManager configurationManager)
        {
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
            _appHost = appHost;
            _itemProvider = new OmdbItemProvider(jsonSerializer, _appHost, httpClient, logger, libraryManager, fileSystem, configurationManager);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            return _itemProvider.GetSearchResults(searchInfo, "episode", cancellationToken);
        }

        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Episode>()
            {
                Item = new Episode(),
                QueriedById = true
            };

            // Allowing this will dramatically increase scan times
            if (info.IsMissingEpisode)
            {
                return result;
            }

            string seriesImdbId;
            if (info.SeriesProviderIds.TryGetValue(MetadataProviders.Imdb.ToString(), out seriesImdbId) && !string.IsNullOrEmpty(seriesImdbId))
            {
                if (info.IndexNumber.HasValue && info.ParentIndexNumber.HasValue)
                {
                    result.HasMetadata = await new OmdbProvider(_jsonSerializer, _httpClient, _fileSystem, _appHost, _configurationManager)
                        .FetchEpisodeData(result, info.IndexNumber.Value, info.ParentIndexNumber.Value, info.GetProviderId(MetadataProviders.Imdb), seriesImdbId, info.MetadataLanguage, info.MetadataCountryCode, cancellationToken).ConfigureAwait(false);
                }
            }

            return result;
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
