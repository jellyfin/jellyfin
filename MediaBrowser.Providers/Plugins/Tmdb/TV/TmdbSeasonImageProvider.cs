using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.Tmdb.TV
{
    /// <summary>
    /// TV season image provider powered by TheMovieDb.
    /// </summary>
    public class TmdbSeasonImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TmdbClientManager _tmdbClientManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbSeasonImageProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
        /// <param name="tmdbClientManager">The <see cref="TmdbClientManager"/>.</param>
        public TmdbSeasonImageProvider(IHttpClientFactory httpClientFactory, TmdbClientManager tmdbClientManager)
        {
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
        }

        /// <inheritdoc/>
        public int Order => 1;

        /// <inheritdoc/>
        public string Name => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public bool Supports(BaseItem item)
        {
            return item is Season;
        }

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var season = (Season)item;
            var series = season?.Series;

            var seriesTmdbId = Convert.ToInt32(series?.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);

            if (seriesTmdbId <= 0 || season?.IndexNumber is null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var language = item.GetPreferredMetadataLanguage();

            // TODO use image languages if All Languages isn't toggled, but there's currently no way to get that value in here
            var seasonResult = await _tmdbClientManager
                .GetSeasonAsync(seriesTmdbId, season.IndexNumber.Value, null, null, cancellationToken)
                .ConfigureAwait(false);

            var posters = seasonResult?.Images?.Posters;
            if (posters is null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            return _tmdbClientManager.ConvertPostersToRemoteImageInfo(posters, language);
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }
    }
}
