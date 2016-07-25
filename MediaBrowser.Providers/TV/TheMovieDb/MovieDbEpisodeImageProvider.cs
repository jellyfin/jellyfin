using CommonIO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Movies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.TV
{
    public class MovieDbEpisodeImageProvider :
            MovieDbProviderBase,
            IRemoteImageProvider, 
            IHasOrder
    {
        public MovieDbEpisodeImageProvider(IHttpClient httpClient, IServerConfigurationManager configurationManager, IJsonSerializer jsonSerializer, IFileSystem fileSystem, ILocalizationManager localization, ILogManager logManager)
            : base(httpClient, configurationManager, jsonSerializer, fileSystem, localization, logManager)
        {}

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType>
            {
                ImageType.Primary
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken)
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

            var tmdbSettings = await MovieDbProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

            var tmdbImageUrl = tmdbSettings.images.secure_base_url + "original";

            list.AddRange(GetPosters(response.images).Select(i => new RemoteImageInfo
            {
                Url = tmdbImageUrl + i.file_path,
                CommunityRating = i.vote_average,
                VoteCount = i.vote_count,
                Width = i.width,
                Height = i.height,
                Language = MovieDbProvider.AdjustImageLanguage(i.iso_639_1, language),
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
                .ThenByDescending(i => i.VoteCount ?? 0)
                .ToList();

        }

        private IEnumerable<Still> GetPosters(Images images)
        {
            return images.stills ?? new List<Still>();
        }


        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return GetResponse(url, cancellationToken);
        }

        public string Name
        {
            get { return "TheMovieDb"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is Controller.Entities.TV.Episode;
        }

        public int Order
        {
            get
            {
                // After tvdb
                return 1;
            }
        }
    }
}
