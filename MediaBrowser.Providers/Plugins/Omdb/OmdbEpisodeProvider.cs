#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Providers.Plugins.Omdb
{
    public class OmdbEpisodeProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>, IHasOrder
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OmdbItemProvider _itemProvider;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _configurationManager;

        public OmdbEpisodeProvider(
            IJsonSerializer jsonSerializer,
            IHttpClientFactory httpClientFactory,
            ILibraryManager libraryManager,
            IFileSystem fileSystem,
            IServerConfigurationManager configurationManager)
        {
            _jsonSerializer = jsonSerializer;
            _httpClientFactory = httpClientFactory;
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
            _itemProvider = new OmdbItemProvider(jsonSerializer, httpClientFactory, libraryManager, fileSystem, configurationManager);
        }

        // After TheTvDb
        public int Order => 1;

        public string Name => "The Open Movie Database";

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            return _itemProvider.GetSearchResults(searchInfo, "episode", cancellationToken);
        }

        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

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

            if (info.SeriesProviderIds.TryGetValue(MetadataProvider.Imdb.ToString(), out string seriesImdbId) && !string.IsNullOrEmpty(seriesImdbId))
            {
                if (info.IndexNumber.HasValue && info.ParentIndexNumber.HasValue)
                {
                    result.HasMetadata = await new OmdbProvider(_jsonSerializer, _httpClientFactory, _fileSystem, _configurationManager)
                        .FetchEpisodeData(result, info.IndexNumber.Value, info.ParentIndexNumber.Value, info.GetProviderId(MetadataProvider.Imdb), seriesImdbId, info.MetadataLanguage, info.MetadataCountryCode, cancellationToken).ConfigureAwait(false);
                }
            }

            return result;
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _itemProvider.GetImageResponse(url, cancellationToken);
        }
    }
}
