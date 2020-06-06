using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Tmdb.Models.General;
using MediaBrowser.Providers.Tmdb.Movies;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Tmdb.TV
{
    public class TmdbEpisodeImageProvider :
            TmdbEpisodeProviderBase,
            IRemoteImageProvider,
            IHasOrder
    {
        public TmdbEpisodeImageProvider(IHttpClient httpClient, IServerConfigurationManager configurationManager, IJsonSerializer jsonSerializer, IFileSystem fileSystem, ILocalizationManager localization, ILoggerFactory loggerFactory)
            : base(httpClient, configurationManager, jsonSerializer, fileSystem, localization, loggerFactory)
        { }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var episode = (Controller.Entities.TV.Episode)item;
            var series = episode.Series;

            var seriesId = series != null ? series.GetProviderId(MetadataProviders.Tmdb) : null;

            var list = new List<RemoteImageInfo>();

            if (string.IsNullOrEmpty(seriesId))
            {
                return list;
            }

            var seasonNumber = episode.ParentIndexNumber;
            var episodeNumber = episode.IndexNumber;

            if (!seasonNumber.HasValue || !episodeNumber.HasValue)
            {
                return list;
            }

            var language = item.GetPreferredMetadataLanguage();

            var response = await GetEpisodeInfo(seriesId, seasonNumber.Value, episodeNumber.Value,
                        language, cancellationToken).ConfigureAwait(false);

            var tmdbSettings = await TmdbMovieProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

            var tmdbImageUrl = tmdbSettings.images.GetImageUrl("original");

            list.AddRange(GetPosters(response.Images).Select(i => new RemoteImageInfo
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
            }));


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

        private IEnumerable<Still> GetPosters(StillImages images)
        {
            return images.Stills ?? new List<Still>();
        }


        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return GetResponse(url, cancellationToken);
        }

        public string Name => TmdbUtils.ProviderName;

        public bool Supports(BaseItem item)
        {
            return item is Controller.Entities.TV.Episode;
        }

        // After TheTvDb
        public int Order => 1;
    }
}
