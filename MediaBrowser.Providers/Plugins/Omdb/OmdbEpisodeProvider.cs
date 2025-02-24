#pragma warning disable CS1591

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.Omdb
{
    public class OmdbEpisodeProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>, IHasOrder
    {
        private readonly OmdbItemProvider _itemProvider;
        private readonly OmdbProvider _omdbProvider;

        public OmdbEpisodeProvider(
            IHttpClientFactory httpClientFactory,
            ILibraryManager libraryManager,
            IFileSystem fileSystem,
            IServerConfigurationManager configurationManager)
        {
            _itemProvider = new OmdbItemProvider(httpClientFactory, libraryManager, fileSystem, configurationManager);
            _omdbProvider = new OmdbProvider(httpClientFactory, fileSystem, configurationManager);
        }

        // After TheTvDb
        public int Order => 1;

        public string Name => "The Open Movie Database";

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            return _itemProvider.GetSearchResults(searchInfo, cancellationToken);
        }

        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Episode>
            {
                Item = new Episode(),
                QueriedById = true
            };

            // Allowing this will dramatically increase scan times
            if (info.IsMissingEpisode)
            {
                return result;
            }

            if (info.SeriesProviderIds.TryGetValue(MetadataProvider.Imdb.ToString(), out string? seriesImdbId)
                && !string.IsNullOrEmpty(seriesImdbId)
                && info.IndexNumber.HasValue)
            {
                result.HasMetadata = await _omdbProvider.FetchEpisodeData(
                    result,
                    info.IndexNumber.Value,
                    info.ParentIndexNumber ?? 1,
                    info.GetProviderId(MetadataProvider.Imdb),
                    seriesImdbId,
                    info.MetadataLanguage,
                    info.MetadataCountryCode,
                    cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _itemProvider.GetImageResponse(url, cancellationToken);
        }
    }
}
