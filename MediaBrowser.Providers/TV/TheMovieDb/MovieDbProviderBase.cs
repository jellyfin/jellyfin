using CommonIO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Movies;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.TV
{
    public abstract class MovieDbProviderBase
    {
        private const string EpisodeUrlPattern = @"https://api.themoviedb.org/3/tv/{0}/season/{1}/episode/{2}?api_key={3}&append_to_response=images,external_ids,credits,videos";
        private readonly IHttpClient _httpClient;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly ILocalizationManager _localization;
        private readonly ILogger _logger;

        public MovieDbProviderBase(IHttpClient httpClient, IServerConfigurationManager configurationManager, IJsonSerializer jsonSerializer, IFileSystem fileSystem, ILocalizationManager localization, ILogManager logManager)
        {
            _httpClient = httpClient;
            _configurationManager = configurationManager;
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            _localization = localization;
            _logger = logManager.GetLogger(GetType().Name);
        }

        protected ILogger Logger
        {
            get { return _logger; }
        }

        protected async Task<RootObject> GetEpisodeInfo(string seriesTmdbId, int season, int episodeNumber, string preferredMetadataLanguage,
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
            var mainResult = await FetchMainResult(EpisodeUrlPattern, id, seasonNumber, episodeNumber, preferredMetadataLanguage, cancellationToken).ConfigureAwait(false);

            var dataFilePath = GetDataFilePath(id, seasonNumber, episodeNumber, preferredMetadataLanguage);

			_fileSystem.CreateDirectory(Path.GetDirectoryName(dataFilePath));
            _jsonSerializer.SerializeToFile(mainResult, dataFilePath);
        }

        internal async Task<RootObject> FetchMainResult(string urlPattern, string id, int seasonNumber, int episodeNumber, string language, CancellationToken cancellationToken)
        {
            var url = string.Format(urlPattern, id, seasonNumber.ToString(CultureInfo.InvariantCulture), episodeNumber, MovieDbProvider.ApiKey);

            if (!string.IsNullOrEmpty(language))
            {
                url += string.Format("&language={0}", language);
            }

            var includeImageLanguageParam = MovieDbProvider.GetImageLanguagesParam(language);
            // Get images in english and with no language
            url += "&include_image_language=" + includeImageLanguageParam;

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

        protected Task<HttpResponseInfo> GetResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = MovieDbProvider.Current.MovieDbResourcePool
            });
        }

        public class Still
        {
            public double aspect_ratio { get; set; }
            public string file_path { get; set; }
            public int height { get; set; }
            public string id { get; set; }
            public string iso_639_1 { get; set; }
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
            public List<Video> results { get; set; }
        }

        public class Video
        {
            public string id { get; set; }
            public string iso_639_1 { get; set; }
            public string iso_3166_1 { get; set; }
            public string key { get; set; }
            public string name { get; set; }
            public string site { get; set; }
            public string size { get; set; }
            public string type { get; set; }
        }

        public class RootObject
        {
            public DateTime air_date { get; set; }
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
    }
}
