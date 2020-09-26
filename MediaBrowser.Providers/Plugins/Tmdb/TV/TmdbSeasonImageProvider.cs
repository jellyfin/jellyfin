#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
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
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Plugins.Tmdb.Models.General;
using MediaBrowser.Providers.Plugins.Tmdb.Movies;

namespace MediaBrowser.Providers.Plugins.Tmdb.TV
{
    public class TmdbSeasonImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClientFactory _httpClientFactory;

        public TmdbSeasonImageProvider(IJsonSerializer jsonSerializer, IHttpClientFactory httpClientFactory)
        {
            _jsonSerializer = jsonSerializer;
            _httpClientFactory = httpClientFactory;
        }

        public int Order => 1;

        public string Name => ProviderName;

        public static string ProviderName => TmdbUtils.ProviderName;

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var season = (Season)item;
            var series = season.Series;

            var seriesId = series?.GetProviderId(MetadataProvider.Tmdb);

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
            var seasonNumber = item.IndexNumber.GetValueOrDefault();
            await TmdbSeasonProvider.Current.EnsureSeasonInfo(tmdbId, seasonNumber, language, cancellationToken).ConfigureAwait(false);

            var path = TmdbSeasonProvider.Current.GetDataFilePath(tmdbId, seasonNumber, language);

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
