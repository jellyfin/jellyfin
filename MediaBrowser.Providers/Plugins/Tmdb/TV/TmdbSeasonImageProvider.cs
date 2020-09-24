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
    public class TmdbSeasonImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TmdbClientManager _tmdbClientManager;

        public TmdbSeasonImageProvider(IHttpClientFactory httpClientFactory, TmdbClientManager tmdbClientManager)
        {
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
        }

        public int Order => 1;

        public string Name => TmdbUtils.ProviderName;

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var season = (Season)item;
            var series = season?.Series;

            var seriesTmdbId = Convert.ToInt32(series?.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);

            if (seriesTmdbId <= 0 || season?.IndexNumber != null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var language = item.GetPreferredMetadataLanguage();

            var seasonResult = await _tmdbClientManager
                .GetSeasonAsync(seriesTmdbId, season.IndexNumber.Value, language, TmdbUtils.GetImageLanguagesParam(language), cancellationToken)
                .ConfigureAwait(false);

            if (seasonResult?.Images?.Posters == null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var remoteImages = new List<RemoteImageInfo>();
            for (var i = 0; i < seasonResult.Images.Posters.Count; i++)
            {
                var image = seasonResult.Images.Posters[i];
                remoteImages.Add(new RemoteImageInfo
                {
                    Url = _tmdbClientManager.GetPosterUrl(image.FilePath),
                    CommunityRating = image.VoteAverage,
                    VoteCount = image.VoteCount,
                    Width = image.Width,
                    Height = image.Height,
                    Language = TmdbUtils.AdjustImageLanguage(image.Iso_639_1, language),
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    RatingType = RatingType.Score
                });
            }

            return remoteImages.OrderByLanguageDescending(language);
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary
            };
        }

        public bool Supports(BaseItem item)
        {
            return item is Season;
        }
    }
}
