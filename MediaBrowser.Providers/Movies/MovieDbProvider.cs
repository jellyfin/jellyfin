using System.Linq;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Movies
{
    /// <summary>
    /// Class MovieDbProvider
    /// </summary>
    public class MovieDbProvider : IRemoteMetadataProvider<Movie, MovieInfo>, IDisposable, IHasOrder
    {
        internal readonly SemaphoreSlim MovieDbResourcePool = new SemaphoreSlim(1, 1);

        internal static MovieDbProvider Current { get; private set; }

        private readonly IJsonSerializer _jsonSerializer;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly ILogger _logger;
        private readonly ILocalizationManager _localization;

        public MovieDbProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILogger logger, ILocalizationManager localization)
        {
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
            _logger = logger;
            _localization = localization;
            Current = this;
        }

        public Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            return GetItemMetadata<Movie>(info, cancellationToken);
        }

        public Task<MetadataResult<T>> GetItemMetadata<T>(ItemLookupInfo id, CancellationToken cancellationToken)
            where T : Video, new ()
        {
            var movieDb = new GenericMovieDbInfo<T>(_logger, _jsonSerializer);

            return movieDb.GetMetadata(id, cancellationToken);
        }

        public string Name
        {
            get { return "TheMovieDb"; }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                MovieDbResourcePool.Dispose();
            }
        }

        /// <summary>
        /// The _TMDB settings task
        /// </summary>
        private TmdbSettingsResult _tmdbSettings;

        private readonly SemaphoreSlim _tmdbSettingsSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Gets the TMDB settings.
        /// </summary>
        /// <returns>Task{TmdbSettingsResult}.</returns>
        internal async Task<TmdbSettingsResult> GetTmdbSettings(CancellationToken cancellationToken)
        {
            if (_tmdbSettings != null)
            {
                return _tmdbSettings;
            }

            await _tmdbSettingsSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // Check again in case it got populated while we were waiting.
                if (_tmdbSettings != null)
                {
                    return _tmdbSettings;
                }

                using (var json = await GetMovieDbResponse(new HttpRequestOptions
                {
                    Url = string.Format(TmdbConfigUrl, ApiKey),
                    CancellationToken = cancellationToken,
                    AcceptHeader = AcceptHeader

                }).ConfigureAwait(false))
                {
                    _tmdbSettings = _jsonSerializer.DeserializeFromStream<TmdbSettingsResult>(json);

                    return _tmdbSettings;
                }
            }
            finally
            {
                _tmdbSettingsSemaphore.Release();
            }
        }

        private const string TmdbConfigUrl = "http://api.themoviedb.org/3/configuration?api_key={0}";
        private const string GetMovieInfo3 = @"http://api.themoviedb.org/3/movie/{0}?api_key={1}&append_to_response=casts,releases,images,keywords,trailers";

        internal static string ApiKey = "f6bd687ffa63cd282b6ff2c6877f2669";
        internal static string AcceptHeader = "application/json,image/*";

        /// <summary>
        /// Gets the movie data path.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="tmdbId">The TMDB id.</param>
        /// <returns>System.String.</returns>
        internal static string GetMovieDataPath(IApplicationPaths appPaths, string tmdbId)
        {
            var dataPath = GetMoviesDataPath(appPaths);

            return Path.Combine(dataPath, tmdbId);
        }

        internal static string GetMoviesDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.DataPath, "tmdb-movies");

            return dataPath;
        }

        /// <summary>
        /// Downloads the movie info.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="preferredMetadataLanguage">The preferred metadata language.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        internal async Task DownloadMovieInfo(string id, string preferredMetadataLanguage, CancellationToken cancellationToken)
        {
            var mainResult = await FetchMainResult(id, preferredMetadataLanguage, cancellationToken).ConfigureAwait(false);

            if (mainResult == null) return;

            var dataFilePath = GetDataFilePath(id, preferredMetadataLanguage);

            Directory.CreateDirectory(Path.GetDirectoryName(dataFilePath));

            _jsonSerializer.SerializeToFile(mainResult, dataFilePath);
        }

        private readonly Task _cachedTask = Task.FromResult(true);
        internal Task EnsureMovieInfo(string tmdbId, string language, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(tmdbId))
            {
                throw new ArgumentNullException("tmdbId");
            }
            if (string.IsNullOrEmpty(language))
            {
                throw new ArgumentNullException("language");
            }

            var path = GetDataFilePath(tmdbId, language);

            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.Exists)
            {
                // If it's recent or automatic updates are enabled, don't re-download
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 7)
                {
                    return _cachedTask;
                }
            }

            return DownloadMovieInfo(tmdbId, language, cancellationToken);
        }

        internal string GetDataFilePath(string tmdbId, string preferredLanguage)
        {
            if (string.IsNullOrEmpty(tmdbId))
            {
                throw new ArgumentNullException("tmdbId");
            }
            if (string.IsNullOrEmpty(preferredLanguage))
            {
                throw new ArgumentNullException("preferredLanguage");
            }

            var path = GetMovieDataPath(_configurationManager.ApplicationPaths, tmdbId);

            var filename = string.Format("all-{0}.json",
                preferredLanguage ?? string.Empty);

            return Path.Combine(path, filename);
        }

        /// <summary>
        /// Fetches the main result.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="language">The language.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Task{CompleteMovieData}.</returns>
        internal async Task<CompleteMovieData> FetchMainResult(string id, string language, CancellationToken cancellationToken)
        {
            var url = string.Format(GetMovieInfo3, id, ApiKey);

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

            CompleteMovieData mainResult;

            cancellationToken.ThrowIfCancellationRequested();

            using (var json = await GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                AcceptHeader = AcceptHeader

            }).ConfigureAwait(false))
            {
                mainResult = _jsonSerializer.DeserializeFromStream<CompleteMovieData>(json);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (mainResult != null && string.IsNullOrEmpty(mainResult.overview))
            {
                if (!string.IsNullOrEmpty(language) && !string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Info("MovieDbProvider couldn't find meta for language " + language + ". Trying English...");

                    url = string.Format(GetMovieInfo3, id, ApiKey) + "&include_image_language=en,null&language=en";

                    using (var json = await GetMovieDbResponse(new HttpRequestOptions
                    {
                        Url = url,
                        CancellationToken = cancellationToken,
                        AcceptHeader = AcceptHeader

                    }).ConfigureAwait(false))
                    {
                        mainResult = _jsonSerializer.DeserializeFromStream<CompleteMovieData>(json);
                    }

                    if (String.IsNullOrEmpty(mainResult.overview))
                    {
                        _logger.Error("MovieDbProvider - Unable to find information for (id:" + id + ")");
                        return null;
                    }
                }
            }
            return mainResult;
        }

        private DateTime _lastRequestDate = DateTime.MinValue;

        /// <summary>
        /// Gets the movie db response.
        /// </summary>
        internal async Task<Stream> GetMovieDbResponse(HttpRequestOptions options)
        {
            var cancellationToken = options.CancellationToken;

            await MovieDbResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // Limit to three requests per second
                var diff = 340 - (DateTime.Now - _lastRequestDate).TotalMilliseconds;

                if (diff > 0)
                {
                    await Task.Delay(Convert.ToInt32(diff), cancellationToken).ConfigureAwait(false);
                }

                _lastRequestDate = DateTime.Now;

                return await _httpClient.Get(options).ConfigureAwait(false);
            }
            finally
            {
                _lastRequestDate = DateTime.Now;

                MovieDbResourcePool.Release();
            }
        }

        public bool HasChanged(IHasMetadata item, DateTime date)
        {
            if (!_configurationManager.Configuration.EnableTmdbUpdates)
            {
                return false;
            }

            var tmdbId = item.GetProviderId(MetadataProviders.Tmdb);

            if (!String.IsNullOrEmpty(tmdbId))
            {
                // Process images
                var dataFilePath = GetDataFilePath(tmdbId, item.GetPreferredMetadataLanguage());

                var fileInfo = new FileInfo(dataFilePath);
                
                return !fileInfo.Exists || _fileSystem.GetLastWriteTimeUtc(fileInfo) > date;
            }
            
            return false;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Class TmdbTitle
        /// </summary>
        internal class TmdbTitle
        {
            /// <summary>
            /// Gets or sets the iso_3166_1.
            /// </summary>
            /// <value>The iso_3166_1.</value>
            public string iso_3166_1 { get; set; }
            /// <summary>
            /// Gets or sets the title.
            /// </summary>
            /// <value>The title.</value>
            public string title { get; set; }
        }

        /// <summary>
        /// Class TmdbAltTitleResults
        /// </summary>
        internal class TmdbAltTitleResults
        {
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            /// <value>The id.</value>
            public int id { get; set; }
            /// <summary>
            /// Gets or sets the titles.
            /// </summary>
            /// <value>The titles.</value>
            public List<TmdbTitle> titles { get; set; }
        }

        internal class BelongsToCollection
        {
            public int id { get; set; }
            public string name { get; set; }
            public string poster_path { get; set; }
            public string backdrop_path { get; set; }
        }

        internal class GenreItem
        {
            public int id { get; set; }
            public string name { get; set; }
        }

        internal class ProductionCompany
        {
            public string name { get; set; }
            public int id { get; set; }
        }

        internal class ProductionCountry
        {
            public string iso_3166_1 { get; set; }
            public string name { get; set; }
        }

        internal class SpokenLanguage
        {
            public string iso_639_1 { get; set; }
            public string name { get; set; }
        }

        internal class Cast
        {
            public int id { get; set; }
            public string name { get; set; }
            public string character { get; set; }
            public int order { get; set; }
            public int cast_id { get; set; }
            public string profile_path { get; set; }
        }

        internal class Crew
        {
            public int id { get; set; }
            public string name { get; set; }
            public string department { get; set; }
            public string job { get; set; }
            public string profile_path { get; set; }
        }

        internal class Casts
        {
            public List<Cast> cast { get; set; }
            public List<Crew> crew { get; set; }
        }

        internal class Country
        {
            public string iso_3166_1 { get; set; }
            public string certification { get; set; }
            public DateTime release_date { get; set; }
        }

        internal class Releases
        {
            public List<Country> countries { get; set; }
        }

        internal class Backdrop
        {
            public string file_path { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public object iso_639_1 { get; set; }
            public double aspect_ratio { get; set; }
            public double vote_average { get; set; }
            public int vote_count { get; set; }
        }

        internal class Poster
        {
            public string file_path { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public string iso_639_1 { get; set; }
            public double aspect_ratio { get; set; }
            public double vote_average { get; set; }
            public int vote_count { get; set; }
        }

        internal class Images
        {
            public List<Backdrop> backdrops { get; set; }
            public List<Poster> posters { get; set; }
        }

        internal class Keyword
        {
            public int id { get; set; }
            public string name { get; set; }
        }

        internal class Keywords
        {
            public List<Keyword> keywords { get; set; }
        }

        internal class Youtube
        {
            public string name { get; set; }
            public string size { get; set; }
            public string source { get; set; }
        }

        internal class Trailers
        {
            public List<object> quicktime { get; set; }
            public List<Youtube> youtube { get; set; }
        }

        internal class CompleteMovieData
        {
            public bool adult { get; set; }
            public string backdrop_path { get; set; }
            public BelongsToCollection belongs_to_collection { get; set; }
            public int budget { get; set; }
            public List<GenreItem> genres { get; set; }
            public string homepage { get; set; }
            public int id { get; set; }
            public string imdb_id { get; set; }
            public string original_title { get; set; }
            public string overview { get; set; }
            public double popularity { get; set; }
            public string poster_path { get; set; }
            public List<ProductionCompany> production_companies { get; set; }
            public List<ProductionCountry> production_countries { get; set; }
            public DateTime release_date { get; set; }
            public int revenue { get; set; }
            public int runtime { get; set; }
            public List<SpokenLanguage> spoken_languages { get; set; }
            public string status { get; set; }
            public string tagline { get; set; }
            public string title { get; set; }
            public string name { get; set; }
            public double vote_average { get; set; }
            public int vote_count { get; set; }
            public Casts casts { get; set; }
            public Releases releases { get; set; }
            public Images images { get; set; }
            public Keywords keywords { get; set; }
            public Trailers trailers { get; set; }
        }

        public int Order
        {
            get
            {
                // After Omdb
                return 1;
            }
        }
    }
}
