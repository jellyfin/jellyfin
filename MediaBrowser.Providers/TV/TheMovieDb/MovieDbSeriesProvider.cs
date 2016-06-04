using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
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
using CommonIO;

namespace MediaBrowser.Providers.TV
{
    public class MovieDbSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>, IHasOrder
    {
        private const string GetTvInfo3 = @"https://api.themoviedb.org/3/tv/{0}?api_key={1}&append_to_response=credits,images,keywords,external_ids,videos,content_ratings";
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        internal static MovieDbSeriesProvider Current { get; private set; }

        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly ILogger _logger;
        private readonly ILocalizationManager _localization;
        private readonly IHttpClient _httpClient;
        private readonly ILibraryManager _libraryManager;

        public MovieDbSeriesProvider(IJsonSerializer jsonSerializer, IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILogger logger, ILocalizationManager localization, IHttpClient httpClient, ILibraryManager libraryManager)
        {
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
            _logger = logger;
            _localization = localization;
            _httpClient = httpClient;
            _libraryManager = libraryManager;
            Current = this;
        }

        public string Name
        {
            get { return "TheMovieDb"; }
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            var tmdbId = searchInfo.GetProviderId(MetadataProviders.Tmdb);

            if (!string.IsNullOrEmpty(tmdbId))
            {
                cancellationToken.ThrowIfCancellationRequested();

                await EnsureSeriesInfo(tmdbId, searchInfo.MetadataLanguage, cancellationToken).ConfigureAwait(false);

                var dataFilePath = GetDataFilePath(tmdbId, searchInfo.MetadataLanguage);

                var obj = _jsonSerializer.DeserializeFromFile<RootObject>(dataFilePath);

                var tmdbSettings = await MovieDbProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);
                var tmdbImageUrl = tmdbSettings.images.secure_base_url + "original";

                var remoteResult = new RemoteSearchResult
                {
                    Name = obj.name,
                    SearchProviderName = Name,
                    ImageUrl = string.IsNullOrWhiteSpace(obj.poster_path) ? null : tmdbImageUrl + obj.poster_path
                };

                remoteResult.SetProviderId(MetadataProviders.Tmdb, obj.id.ToString(_usCulture));
                remoteResult.SetProviderId(MetadataProviders.Imdb, obj.external_ids.imdb_id);

                if (obj.external_ids.tvdb_id > 0)
                {
                    remoteResult.SetProviderId(MetadataProviders.Tvdb, obj.external_ids.tvdb_id.ToString(_usCulture));
                }

                return new[] { remoteResult };
            }

            var imdbId = searchInfo.GetProviderId(MetadataProviders.Imdb);

            if (!string.IsNullOrEmpty(imdbId))
            {
                var searchResult = await FindByExternalId(imdbId, "imdb_id", cancellationToken).ConfigureAwait(false);

                if (searchResult != null)
                {
                    return new[] { searchResult };
                }
            }

            var tvdbId = searchInfo.GetProviderId(MetadataProviders.Tvdb);

            if (!string.IsNullOrEmpty(tvdbId))
            {
                var searchResult = await FindByExternalId(tvdbId, "tvdb_id", cancellationToken).ConfigureAwait(false);

                if (searchResult != null)
                {
                    return new[] { searchResult };
                }
            }

            return await new MovieDbSearch(_logger, _jsonSerializer, _libraryManager).GetSearchResults(searchInfo, cancellationToken).ConfigureAwait(false);
        }

        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Series>();

            var tmdbId = info.GetProviderId(MetadataProviders.Tmdb);

            if (string.IsNullOrEmpty(tmdbId))
            {
                var imdbId = info.GetProviderId(MetadataProviders.Imdb);

                if (!string.IsNullOrEmpty(imdbId))
                {
                    var searchResult = await FindByExternalId(imdbId, "imdb_id", cancellationToken).ConfigureAwait(false);

                    if (searchResult != null)
                    {
                        tmdbId = searchResult.GetProviderId(MetadataProviders.Tmdb);
                    }
                }
            }

            if (string.IsNullOrEmpty(tmdbId))
            {
                var tvdbId = info.GetProviderId(MetadataProviders.Tvdb);

                if (!string.IsNullOrEmpty(tvdbId))
                {
                    var searchResult = await FindByExternalId(tvdbId, "tvdb_id", cancellationToken).ConfigureAwait(false);

                    if (searchResult != null)
                    {
                        tmdbId = searchResult.GetProviderId(MetadataProviders.Tmdb);
                    }
                }
            }

            if (string.IsNullOrEmpty(tmdbId))
            {
                var searchResults = await new MovieDbSearch(_logger, _jsonSerializer, _libraryManager).GetSearchResults(info, cancellationToken).ConfigureAwait(false);

                var searchResult = searchResults.FirstOrDefault();

                if (searchResult != null)
                {
                    tmdbId = searchResult.GetProviderId(MetadataProviders.Tmdb);
                }
            }

            if (!string.IsNullOrEmpty(tmdbId))
            {
                cancellationToken.ThrowIfCancellationRequested();

                result.Item = await FetchSeriesData(tmdbId, info.MetadataLanguage, info.MetadataCountryCode, cancellationToken).ConfigureAwait(false);

                result.HasMetadata = result.Item != null;
            }

            return result;
        }

        private async Task<Series> FetchSeriesData(string tmdbId, string language, string preferredCountryCode, CancellationToken cancellationToken)
        {
            string dataFilePath = null;
            RootObject seriesInfo = null;

            if (!string.IsNullOrEmpty(tmdbId))
            {
                seriesInfo = await FetchMainResult(tmdbId, language, cancellationToken).ConfigureAwait(false);
            }

            if (seriesInfo == null)
            {
                return null;
            }

            tmdbId = seriesInfo.id.ToString(_usCulture);

            dataFilePath = GetDataFilePath(tmdbId, language);
			_fileSystem.CreateDirectory(Path.GetDirectoryName(dataFilePath));
            _jsonSerializer.SerializeToFile(seriesInfo, dataFilePath);

            await EnsureSeriesInfo(tmdbId, language, cancellationToken).ConfigureAwait(false);

            var item = new Series();

            ProcessMainInfo(item, seriesInfo, preferredCountryCode);

            return item;
        }

        private void ProcessMainInfo(Series series, RootObject seriesInfo, string preferredCountryCode)
        {
            series.Name = seriesInfo.name;
            series.SetProviderId(MetadataProviders.Tmdb, seriesInfo.id.ToString(_usCulture));

            series.VoteCount = seriesInfo.vote_count;

            string voteAvg = seriesInfo.vote_average.ToString(CultureInfo.InvariantCulture);
            float rating;

            if (float.TryParse(voteAvg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out rating))
            {
                series.CommunityRating = rating;
            }

            series.Overview = seriesInfo.overview;

            if (seriesInfo.networks != null)
            {
                series.Studios = seriesInfo.networks.Select(i => i.name).ToList();
            }

            if (seriesInfo.genres != null)
            {
                series.Genres = seriesInfo.genres.Select(i => i.name).ToList();
            }

            series.HomePageUrl = seriesInfo.homepage;

            series.RunTimeTicks = seriesInfo.episode_run_time.Select(i => TimeSpan.FromMinutes(i).Ticks).FirstOrDefault();

            if (string.Equals(seriesInfo.status, "Ended", StringComparison.OrdinalIgnoreCase))
            {
                series.Status = SeriesStatus.Ended;
                series.EndDate = seriesInfo.last_air_date;
            }
            else
            {
                series.Status = SeriesStatus.Continuing;
            }

            series.PremiereDate = seriesInfo.first_air_date;

            var ids = seriesInfo.external_ids;
            if (ids != null)
            {
                if (!string.IsNullOrWhiteSpace(ids.imdb_id))
                {
                    series.SetProviderId(MetadataProviders.Imdb, ids.imdb_id);
                }
                if (ids.tvrage_id > 0)
                {
                    series.SetProviderId(MetadataProviders.TvRage, ids.tvrage_id.ToString(_usCulture));
                }
                if (ids.tvdb_id > 0)
                {
                    series.SetProviderId(MetadataProviders.Tvdb, ids.tvdb_id.ToString(_usCulture));
                }
            }

            var contentRatings = (seriesInfo.content_ratings ?? new ContentRatings()).results ?? new List<ContentRating>();

            var ourRelease = contentRatings.FirstOrDefault(c => string.Equals(c.iso_3166_1, preferredCountryCode, StringComparison.OrdinalIgnoreCase));
            var usRelease = contentRatings.FirstOrDefault(c => string.Equals(c.iso_3166_1, "US", StringComparison.OrdinalIgnoreCase));
            var minimumRelease = contentRatings.FirstOrDefault();

            if (ourRelease != null)
            {
                series.OfficialRating = ourRelease.rating;
            }
            else if (usRelease != null)
            {
                series.OfficialRating = usRelease.rating;
            }
            else if (minimumRelease != null)
            {
                series.OfficialRating = minimumRelease.rating;
            }

            if (seriesInfo.videos != null && seriesInfo.videos.results != null)
            {
                foreach (var video in seriesInfo.videos.results)
                {
                    if (video.type.Equals("trailer", System.StringComparison.OrdinalIgnoreCase)
                        || video.type.Equals("clip", System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (video.site.Equals("youtube", System.StringComparison.OrdinalIgnoreCase))
                        {
                            var videoUrl = string.Format("http://www.youtube.com/watch?v={0}", video.key);
                            series.AddTrailerUrl(videoUrl, true);
                        }
                    }
                }
            }
        }

        internal static string GetSeriesDataPath(IApplicationPaths appPaths, string tmdbId)
        {
            var dataPath = GetSeriesDataPath(appPaths);

            return Path.Combine(dataPath, tmdbId);
        }

        internal static string GetSeriesDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.CachePath, "tmdb-tv");

            return dataPath;
        }

        internal async Task DownloadSeriesInfo(string id, string preferredMetadataLanguage, CancellationToken cancellationToken)
        {
            var mainResult = await FetchMainResult(id, preferredMetadataLanguage, cancellationToken).ConfigureAwait(false);

            if (mainResult == null) return;

            var dataFilePath = GetDataFilePath(id, preferredMetadataLanguage);

			_fileSystem.CreateDirectory(Path.GetDirectoryName(dataFilePath));

            _jsonSerializer.SerializeToFile(mainResult, dataFilePath);
        }

        internal async Task<RootObject> FetchMainResult(string id, string language, CancellationToken cancellationToken)
        {
            var url = string.Format(GetTvInfo3, id, MovieDbProvider.ApiKey);

            if (!string.IsNullOrEmpty(language))
            {
                url += string.Format("&language={0}", MovieDbProvider.NormalizeLanguage(language));

                // Get images in english and with no language
                url += "&include_image_language=" + MovieDbProvider.GetImageLanguagesParam(language);
            }

            cancellationToken.ThrowIfCancellationRequested();

            RootObject mainResult;

            using (var json = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                AcceptHeader = MovieDbProvider.AcceptHeader

            }).ConfigureAwait(false))
            {
                mainResult = _jsonSerializer.DeserializeFromStream<RootObject>(json);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // If the language preference isn't english, then have the overview fallback to english if it's blank
            if (mainResult != null &&
                string.IsNullOrEmpty(mainResult.overview) &&
                !string.IsNullOrEmpty(language) &&
                !string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Info("MovieDbSeriesProvider couldn't find meta for language " + language + ". Trying English...");

                url = string.Format(GetTvInfo3, id, MovieDbProvider.ApiKey) + "&language=en";

                if (!string.IsNullOrEmpty(language))
                {
                    // Get images in english and with no language
                    url += "&include_image_language=" + MovieDbProvider.GetImageLanguagesParam(language);
                }

                using (var json = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
                {
                    Url = url,
                    CancellationToken = cancellationToken,
                    AcceptHeader = MovieDbProvider.AcceptHeader

                }).ConfigureAwait(false))
                {
                    var englishResult = _jsonSerializer.DeserializeFromStream<RootObject>(json);

                    mainResult.overview = englishResult.overview;
                }
            }

            return mainResult;
        }

        private readonly Task _cachedTask = Task.FromResult(true);
        internal Task EnsureSeriesInfo(string tmdbId, string language, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(tmdbId))
            {
                throw new ArgumentNullException("tmdbId");
            }

            var path = GetDataFilePath(tmdbId, language);

            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.Exists)
            {
                // If it's recent or automatic updates are enabled, don't re-download
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 3)
                {
                    return _cachedTask;
                }
            }

            return DownloadSeriesInfo(tmdbId, language, cancellationToken);
        }

        internal string GetDataFilePath(string tmdbId, string preferredLanguage)
        {
            if (string.IsNullOrEmpty(tmdbId))
            {
                throw new ArgumentNullException("tmdbId");
            }

            var path = GetSeriesDataPath(_configurationManager.ApplicationPaths, tmdbId);

            var filename = string.Format("series-{0}.json", preferredLanguage ?? string.Empty);

            return Path.Combine(path, filename);
        }

        private async Task<RemoteSearchResult> FindByExternalId(string id, string externalSource, CancellationToken cancellationToken)
        {
            var url = string.Format("https://api.themoviedb.org/3/tv/find/{0}?api_key={1}&external_source={2}",
                id,
                MovieDbProvider.ApiKey,
                externalSource);

            using (var json = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                AcceptHeader = MovieDbProvider.AcceptHeader

            }).ConfigureAwait(false))
            {
                var result = _jsonSerializer.DeserializeFromStream<MovieDbSearch.ExternalIdLookupResult>(json);

                if (result != null && result.tv_results != null)
                {
                    var tv = result.tv_results.FirstOrDefault();

                    if (tv != null)
                    {
                        var tmdbSettings = await MovieDbProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);
                        var tmdbImageUrl = tmdbSettings.images.secure_base_url + "original";

                        var remoteResult = new RemoteSearchResult
                        {
                            Name = tv.name,
                            SearchProviderName = Name,
                            ImageUrl = string.IsNullOrWhiteSpace(tv.poster_path) ? null : tmdbImageUrl + tv.poster_path
                        };

                        remoteResult.SetProviderId(MetadataProviders.Tmdb, tv.id.ToString(_usCulture));

                        return remoteResult;
                    }
                }
            }

            return null;
        }

        public class CreatedBy
        {
            public int id { get; set; }
            public string name { get; set; }
            public string profile_path { get; set; }
        }

        public class Genre
        {
            public int id { get; set; }
            public string name { get; set; }
        }

        public class Network
        {
            public int id { get; set; }
            public string name { get; set; }
        }

        public class Season
        {
            public string air_date { get; set; }
            public int episode_count { get; set; }
            public int id { get; set; }
            public string poster_path { get; set; }
            public int season_number { get; set; }
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
            public string credit_id { get; set; }
            public string department { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public string job { get; set; }
            public string profile_path { get; set; }
        }

        public class Credits
        {
            public List<Cast> cast { get; set; }
            public List<Crew> crew { get; set; }
        }

        public class Backdrop
        {
            public double aspect_ratio { get; set; }
            public string file_path { get; set; }
            public int height { get; set; }
            public string iso_639_1 { get; set; }
            public double vote_average { get; set; }
            public int vote_count { get; set; }
            public int width { get; set; }
        }

        public class Poster
        {
            public double aspect_ratio { get; set; }
            public string file_path { get; set; }
            public int height { get; set; }
            public string iso_639_1 { get; set; }
            public double vote_average { get; set; }
            public int vote_count { get; set; }
            public int width { get; set; }
        }

        public class Images
        {
            public List<Backdrop> backdrops { get; set; }
            public List<Poster> posters { get; set; }
        }

        public class Keywords
        {
            public List<object> results { get; set; }
        }

        public class ExternalIds
        {
            public string imdb_id { get; set; }
            public string freebase_id { get; set; }
            public string freebase_mid { get; set; }
            public int tvdb_id { get; set; }
            public int tvrage_id { get; set; }
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

        public class ContentRating
        {
            public string iso_3166_1 { get; set; }
            public string rating { get; set; }
        }

        public class ContentRatings
        {
            public List<ContentRating> results { get; set; }
        }

        public class RootObject
        {
            public string backdrop_path { get; set; }
            public List<CreatedBy> created_by { get; set; }
            public List<int> episode_run_time { get; set; }
            public DateTime first_air_date { get; set; }
            public List<Genre> genres { get; set; }
            public string homepage { get; set; }
            public int id { get; set; }
            public bool in_production { get; set; }
            public List<string> languages { get; set; }
            public DateTime last_air_date { get; set; }
            public string name { get; set; }
            public List<Network> networks { get; set; }
            public int number_of_episodes { get; set; }
            public int number_of_seasons { get; set; }
            public string original_name { get; set; }
            public List<string> origin_country { get; set; }
            public string overview { get; set; }
            public string popularity { get; set; }
            public string poster_path { get; set; }
            public List<Season> seasons { get; set; }
            public string status { get; set; }
            public double vote_average { get; set; }
            public int vote_count { get; set; }
            public Credits credits { get; set; }
            public Images images { get; set; }
            public Keywords keywords { get; set; }
            public ExternalIds external_ids { get; set; }
            public Videos videos { get; set; }
            public ContentRatings content_ratings { get; set; }
        }

        public int Order
        {
            get
            {
                // After Omdb and Tvdb
                return 2;
            }
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
    }
}
