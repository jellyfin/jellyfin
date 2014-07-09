using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Movies;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.TV
{
    public class MovieDbEpisodeImageProvider : IRemoteImageProvider, IHasOrder
    {
        private const string GetTvInfo3 = @"http://api.themoviedb.org/3/tv/{0}/season/{1}/episode/{2}?api_key={3}&append_to_response=images,external_ids,credits,videos";
        private readonly IHttpClient _httpClient;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly ILocalizationManager _localization;

        public MovieDbEpisodeImageProvider(IHttpClient httpClient, IServerConfigurationManager configurationManager, IJsonSerializer jsonSerializer, IFileSystem fileSystem, ILocalizationManager localization)
        {
            _httpClient = httpClient;
            _configurationManager = configurationManager;
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            _localization = localization;
        }

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

            var response = await GetEpisodeInfo(seriesId, seasonNumber.Value, episodeNumber.Value,
                        item.GetPreferredMetadataLanguage(), cancellationToken).ConfigureAwait(false);

            var tmdbSettings = await MovieDbProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

            var tmdbImageUrl = tmdbSettings.images.base_url + "original";

            list.AddRange(GetPosters(response.images).Select(i => new RemoteImageInfo
            {
                Url = tmdbImageUrl + i.file_path,
                CommunityRating = i.vote_average,
                VoteCount = i.vote_count,
                Width = i.width,
                Height = i.height,
                ProviderName = Name,
                Type = ImageType.Primary,
                RatingType = RatingType.Score
            }));

            var language = item.GetPreferredMetadataLanguage();

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
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = MovieDbProvider.Current.MovieDbResourcePool
            });
        }

        public string Name
        {
            get { return "TheMovieDb"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is Controller.Entities.TV.Episode;
        }

        private async Task<RootObject> GetEpisodeInfo(string seriesTmdbId, int season, int episodeNumber, string preferredMetadataLanguage,
            CancellationToken cancellationToken)
        {
            await EnsureEpisodeInfo(seriesTmdbId, season, episodeNumber, preferredMetadataLanguage, cancellationToken)
                    .ConfigureAwait(false);

            var dataFilePath = GetDataFilePath(seriesTmdbId, season, episodeNumber, preferredMetadataLanguage);

            return _jsonSerializer.DeserializeFromFile<RootObject>(dataFilePath);
        }

        internal Task EnsureEpisodeInfo(string tmdbId, int seasonNumber, int episodeNumber, string language, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(tmdbId))
            {
                throw new ArgumentNullException("tmdbId");
            }
            if (string.IsNullOrEmpty(language))
            {
                throw new ArgumentNullException("language");
            }

            var path = GetDataFilePath(tmdbId, seasonNumber, episodeNumber, language);

            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.Exists)
            {
                // If it's recent or automatic updates are enabled, don't re-download
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 3)
                {
                    return Task.FromResult(true);
                }
            }

            return DownloadEpisodeInfo(tmdbId, seasonNumber, episodeNumber, language, cancellationToken);
        }

        internal string GetDataFilePath(string tmdbId, int seasonNumber, int episodeNumber, string preferredLanguage)
        {
            if (string.IsNullOrEmpty(tmdbId))
            {
                throw new ArgumentNullException("tmdbId");
            }
            if (string.IsNullOrEmpty(preferredLanguage))
            {
                throw new ArgumentNullException("preferredLanguage");
            }

            var path = MovieDbSeriesProvider.GetSeriesDataPath(_configurationManager.ApplicationPaths, tmdbId);

            var filename = string.Format("season-{0}-episode-{1}-{2}.json",
                seasonNumber.ToString(CultureInfo.InvariantCulture),
                episodeNumber.ToString(CultureInfo.InvariantCulture),
                preferredLanguage);

            return Path.Combine(path, filename);
        }

        internal async Task DownloadEpisodeInfo(string id, int seasonNumber, int episodeNumber, string preferredMetadataLanguage, CancellationToken cancellationToken)
        {
            var mainResult = await FetchMainResult(id, seasonNumber, episodeNumber, preferredMetadataLanguage, cancellationToken).ConfigureAwait(false);

            var dataFilePath = GetDataFilePath(id, seasonNumber, episodeNumber, preferredMetadataLanguage);

            Directory.CreateDirectory(Path.GetDirectoryName(dataFilePath));
            _jsonSerializer.SerializeToFile(mainResult, dataFilePath);
        }

        internal async Task<RootObject> FetchMainResult(string id, int seasonNumber, int episodeNumber, string language, CancellationToken cancellationToken)
        {
            var url = string.Format(GetTvInfo3, id, seasonNumber.ToString(CultureInfo.InvariantCulture), episodeNumber, MovieDbProvider.ApiKey);

            var imageLanguages = _localization.GetCultures()
                .Select(i => i.TwoLetterISOLanguageName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            imageLanguages.Add("null");

            if (!string.IsNullOrEmpty(language))
            {
                // If preferred language isn't english, get those images too
                if (imageLanguages.Contains(language, StringComparer.OrdinalIgnoreCase))
                {
                    imageLanguages.Add(language);
                }

                url += string.Format("&language={0}", language);
            }

            // Get images in english and with no language
            url += "&include_image_language=" + string.Join(",", imageLanguages.ToArray());

            cancellationToken.ThrowIfCancellationRequested();

            using (var json = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                AcceptHeader = MovieDbProvider.AcceptHeader

            }).ConfigureAwait(false))
            {
                return _jsonSerializer.DeserializeFromStream<RootObject>(json);
            }
        }

        public class Still
        {
            public double aspect_ratio { get; set; }
            public string file_path { get; set; }
            public int height { get; set; }
            public string id { get; set; }
            public object iso_639_1 { get; set; }
            public double vote_average { get; set; }
            public int vote_count { get; set; }
            public int width { get; set; }
        }

        public class Images
        {
            public List<Still> stills { get; set; }
        }

        public class ExternalIds
        {
            public string imdb_id { get; set; }
            public object freebase_id { get; set; }
            public string freebase_mid { get; set; }
            public int tvdb_id { get; set; }
            public int tvrage_id { get; set; }
        }

        public class Cast
        {
            public string character { get; set; }
            public string credit_id { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public string profile_path { get; set; }
            public int order { get; set; }
        }

        public class Crew
        {
            public int id { get; set; }
            public string credit_id { get; set; }
            public string name { get; set; }
            public string department { get; set; }
            public string job { get; set; }
            public string profile_path { get; set; }
        }

        public class GuestStar
        {
            public int id { get; set; }
            public string name { get; set; }
            public string credit_id { get; set; }
            public string character { get; set; }
            public int order { get; set; }
            public string profile_path { get; set; }
        }

        public class Credits
        {
            public List<Cast> cast { get; set; }
            public List<Crew> crew { get; set; }
            public List<GuestStar> guest_stars { get; set; }
        }

        public class Videos
        {
            public List<object> results { get; set; }
        }

        public class RootObject
        {
            public string air_date { get; set; }
            public int episode_number { get; set; }
            public string name { get; set; }
            public string overview { get; set; }
            public int id { get; set; }
            public object production_code { get; set; }
            public int season_number { get; set; }
            public string still_path { get; set; }
            public double vote_average { get; set; }
            public int vote_count { get; set; }
            public Images images { get; set; }
            public ExternalIds external_ids { get; set; }
            public Credits credits { get; set; }
            public Videos videos { get; set; }
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
