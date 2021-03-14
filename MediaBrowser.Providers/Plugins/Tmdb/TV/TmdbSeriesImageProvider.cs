#pragma warning disable CS1591

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
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.Tmdb.TV
{
    public class TmdbSeriesImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TmdbClientManager _tmdbClientManager;

        public TmdbSeriesImageProvider(IHttpClientFactory httpClientFactory, TmdbClientManager tmdbClientManager)
        {
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
        }

        public string Name => TmdbUtils.ProviderName;

        // After tvdb and fanart
        public int Order => 2;

        public bool Supports(BaseItem item)
        {
            return item is Series;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Backdrop
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var tmdbId = item.GetProviderId(MetadataProvider.Tmdb);

            if (string.IsNullOrEmpty(tmdbId))
            {
                return null;
            }

            var language = item.GetPreferredMetadataLanguage();

            // TODO use image languages if All Languages isn't toggled, but there's currently no way to get that value in here
            var series = await _tmdbClientManager
                .GetSeriesAsync(Convert.ToInt32(tmdbId, CultureInfo.InvariantCulture), null, null, cancellationToken)
                .ConfigureAwait(false);

            if (series?.Images == null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var posters = series.Images.Posters;
            var backdrops = series.Images.Backdrops;

            var remoteImages = new RemoteImageInfo[posters.Count + backdrops.Count];

            for (var i = 0; i < posters.Count; i++)
            {
                var poster = posters[i];
                remoteImages[i] = new RemoteImageInfo
                {
                    Url = _tmdbClientManager.GetPosterUrl(poster.FilePath),
                    CommunityRating = poster.VoteAverage,
                    VoteCount = poster.VoteCount,
                    Width = poster.Width,
                    Height = poster.Height,
                    Language = TmdbUtils.AdjustImageLanguage(poster.Iso_639_1, language),
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    RatingType = RatingType.Score
                };
            }

            for (var i = 0; i < backdrops.Count; i++)
            {
                var backdrop = series.Images.Backdrops[i];
                remoteImages[posters.Count + i] = new RemoteImageInfo
                {
                    Url = _tmdbClientManager.GetBackdropUrl(backdrop.FilePath),
                    CommunityRating = backdrop.VoteAverage,
                    VoteCount = backdrop.VoteCount,
                    Width = backdrop.Width,
                    Height = backdrop.Height,
                    ProviderName = Name,
                    Type = ImageType.Backdrop,
                    RatingType = RatingType.Score
                };
            }

            return remoteImages.OrderByLanguageDescending(language);
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }
    }
}
