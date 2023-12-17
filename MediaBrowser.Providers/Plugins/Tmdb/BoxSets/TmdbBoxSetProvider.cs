using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.Tmdb.BoxSets
{
    /// <summary>
    /// BoxSet provider powered by TMDb.
    /// </summary>
    public class TmdbBoxSetProvider : IRemoteMetadataProvider<BoxSet, BoxSetInfo>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TmdbClientManager _tmdbClientManager;
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbBoxSetProvider"/> class.
        /// </summary>
        /// <param name="libraryManager">The <see cref="ILibraryManager"/>.</param>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
        /// <param name="tmdbClientManager">The <see cref="TmdbClientManager"/>.</param>
        public TmdbBoxSetProvider(IHttpClientFactory httpClientFactory, TmdbClientManager tmdbClientManager, ILibraryManager libraryManager)
        {
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
            _libraryManager = libraryManager;
        }

        /// <inheritdoc />
        public string Name => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(BoxSetInfo searchInfo, CancellationToken cancellationToken)
        {
            var tmdbId = Convert.ToInt32(searchInfo.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);
            var language = searchInfo.MetadataLanguage;

            if (tmdbId > 0)
            {
                var collection = await _tmdbClientManager.GetCollectionAsync(tmdbId, language, TmdbUtils.GetImageLanguagesParam(language), cancellationToken).ConfigureAwait(false);

                if (collection is null)
                {
                    return Enumerable.Empty<RemoteSearchResult>();
                }

                var result = new RemoteSearchResult
                {
                    Name = collection.Name,
                    SearchProviderName = Name
                };

                if (collection.Images is not null)
                {
                    result.ImageUrl = _tmdbClientManager.GetPosterUrl(collection.PosterPath);
                }

                result.SetProviderId(MetadataProvider.Tmdb, collection.Id.ToString(CultureInfo.InvariantCulture));

                return new[] { result };
            }

            var collectionSearchResults = await _tmdbClientManager.SearchCollectionAsync(searchInfo.Name, language, cancellationToken).ConfigureAwait(false);

            var collections = new RemoteSearchResult[collectionSearchResults.Count];
            for (var i = 0; i < collectionSearchResults.Count; i++)
            {
                var result = collectionSearchResults[i];
                var collection = new RemoteSearchResult
                {
                    Name = result.Name,
                    SearchProviderName = Name,
                    ImageUrl = _tmdbClientManager.GetPosterUrl(result.PosterPath)
                };
                collection.SetProviderId(MetadataProvider.Tmdb, result.Id.ToString(CultureInfo.InvariantCulture));

                collections[i] = collection;
            }

            return collections;
        }

        /// <inheritdoc />
        public async Task<MetadataResult<BoxSet>> GetMetadata(BoxSetInfo info, CancellationToken cancellationToken)
        {
            var tmdbId = Convert.ToInt32(info.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);
            var language = info.MetadataLanguage;
            // We don't already have an Id, need to fetch it
            if (tmdbId <= 0)
            {
                // ParseName is required here.
                // Caller provides the filename with extension stripped and NOT the parsed filename
                var parsedName = _libraryManager.ParseName(info.Name);
                var cleanedName = TmdbUtils.CleanName(parsedName.Name);
                var searchResults = await _tmdbClientManager.SearchCollectionAsync(cleanedName, language, cancellationToken).ConfigureAwait(false);

                if (searchResults is not null && searchResults.Count > 0)
                {
                    tmdbId = searchResults[0].Id;
                }
            }

            var result = new MetadataResult<BoxSet>();

            if (tmdbId > 0)
            {
                var collection = await _tmdbClientManager.GetCollectionAsync(tmdbId, language, TmdbUtils.GetImageLanguagesParam(language), cancellationToken).ConfigureAwait(false);

                if (collection is not null)
                {
                    var item = new BoxSet
                    {
                        Name = collection.Name,
                        Overview = collection.Overview
                    };

                    item.SetProviderId(MetadataProvider.Tmdb, collection.Id.ToString(CultureInfo.InvariantCulture));

                    result.HasMetadata = true;
                    result.Item = item;
                }
            }

            return result;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }
    }
}
