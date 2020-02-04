using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Tmdb.Models.General;
using MediaBrowser.Providers.Tmdb.Movies;

namespace MediaBrowser.Providers.Tmdb.TV
{
    public class TmdbSeasonImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;

        public TmdbSeasonImageProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
        }

        public int Order => 1;

        public string Name => ProviderName;

        public static string ProviderName => TmdbUtils.ProviderName;

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var season = (Season)item;
            var series = season.Series;

            var seriesId = series?.GetProviderId(MetadataProviders.Tmdb);

            if (string.IsNullOrEmpty(seriesId))
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var seasonNumber = season.IndexNumber;

            if (!seasonNumber.HasValue)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var language = item.GetPreferredMetadataLanguage();

            var results = await FetchImages(season, seriesId, language, cancellationToken).ConfigureAwait(false);

            var tmdbSettings = await TmdbMovieProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

            var tmdbImageUrl = tmdbSettings.images.GetImageUrl("original");

            var list = results.Select(i => new RemoteImageInfo
            {
                Url = tmdbImageUrl + i.File_Path,
                CommunityRating = i.Vote_Average,
                VoteCount = i.Vote_Count,
                Width = i.Width,
                Height = i.Height,
                Language = TmdbMovieProvider.AdjustImageLanguage(i.Iso_639_1, language),
                ProviderName = Name,
                Type = ImageType.Primary,
                RatingType = RatingType.Score
            });

            var isLanguageEn = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase);

            return list.OrderByDescending(i =>
            {
                if (string.Equals(language, i.Language, StringComparison.OrdinalIgnoreCase))
                {
                    return 3;
                }

                if (!isLanguageEn)
                {
                    if (string.Equals("en", i.Language, StringComparison.OrdinalIgnoreCase))
                    {
                        return 2;
                    }
                }

                if (string.IsNullOrEmpty(i.Language))
                {
                    return isLanguageEn ? 3 : 2;
                }

                return 0;
            })
                .ThenByDescending(i => i.CommunityRating ?? 0)
                .ThenByDescending(i => i.VoteCount ?? 0);
        }

        private async Task<List<Poster>> FetchImages(Season item, string tmdbId, string language, CancellationToken cancellationToken)
        {
            await TmdbSeasonProvider.Current.EnsureSeasonInfo(tmdbId, item.IndexNumber.GetValueOrDefault(), language, cancellationToken).ConfigureAwait(false);

            var path = TmdbSeriesProvider.Current.GetDataFilePath(tmdbId, language);

            if (!string.IsNullOrEmpty(path))
            {
                if (File.Exists(path))
                {
                    return _jsonSerializer.DeserializeFromFile<Models.TV.SeasonResult>(path).Images.Posters;
                }
            }

            return null;
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
