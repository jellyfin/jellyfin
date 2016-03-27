using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.Movies
{
    public class MovieUpdatesPreScanTask : ILibraryPostScanTask
    {
        /// <summary>
        /// The updates URL
        /// </summary>
        private const string UpdatesUrl = "http://api.themoviedb.org/3/movie/changes?start_date={0}&api_key={1}&page={2}";

        /// <summary>
        /// The _HTTP client
        /// </summary>
        private readonly IHttpClient _httpClient;
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;
        /// <summary>
        /// The _config
        /// </summary>
        private readonly IServerConfigurationManager _config;
        private readonly IJsonSerializer _json;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MovieUpdatesPreScanTask"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="config">The config.</param>
        /// <param name="json">The json.</param>
        public MovieUpdatesPreScanTask(ILogger logger, IHttpClient httpClient, IServerConfigurationManager config, IJsonSerializer json, IFileSystem fileSystem, ILibraryManager libraryManager)
        {
            _logger = logger;
            _httpClient = httpClient;
            _config = config;
            _json = json;
            _fileSystem = fileSystem;
            _libraryManager = libraryManager;
        }

        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Runs the specified progress.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (!MovieDbProvider.Current.GetTheMovieDbOptions().EnableAutomaticUpdates)
            {
                progress.Report(100);
                return;
            }

            var path = MovieDbProvider.GetMoviesDataPath(_config.CommonApplicationPaths);

			_fileSystem.CreateDirectory(path);

            var timestampFile = Path.Combine(path, "time.txt");

            var timestampFileInfo = _fileSystem.GetFileInfo(timestampFile);

            // Don't check for updates every single time
            if (timestampFileInfo.Exists && (DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(timestampFileInfo)).TotalDays < 7)
            {
                return;
            }

            // Find out the last time we queried tvdb for updates
			var lastUpdateTime = timestampFileInfo.Exists ? _fileSystem.ReadAllText(timestampFile, Encoding.UTF8) : string.Empty;

            var existingDirectories = Directory.EnumerateDirectories(path).Select(Path.GetFileName).ToList();

            if (!string.IsNullOrEmpty(lastUpdateTime))
            {
                long lastUpdateTicks;

                if (long.TryParse(lastUpdateTime, NumberStyles.Any, UsCulture, out lastUpdateTicks))
                {
                    var lastUpdateDate = new DateTime(lastUpdateTicks, DateTimeKind.Utc);

                    // They only allow up to 14 days of updates
                    if ((DateTime.UtcNow - lastUpdateDate).TotalDays > 13)
                    {
                        lastUpdateDate = DateTime.UtcNow.AddDays(-13);
                    }

                    var updatedIds = await GetIdsToUpdate(lastUpdateDate, 1, cancellationToken).ConfigureAwait(false);

                    var existingDictionary = existingDirectories.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

                    var idsToUpdate = updatedIds.Where(i => !string.IsNullOrWhiteSpace(i) && existingDictionary.ContainsKey(i));

                    await UpdateMovies(idsToUpdate, progress, cancellationToken).ConfigureAwait(false);
                }
            }

			_fileSystem.WriteAllText(timestampFile, DateTime.UtcNow.Ticks.ToString(UsCulture), Encoding.UTF8);
            progress.Report(100);
        }


        /// <summary>
        /// Gets the ids to update.
        /// </summary>
        /// <param name="lastUpdateTime">The last update time.</param>
        /// <param name="page">The page.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{System.String}}.</returns>
        private async Task<IEnumerable<string>> GetIdsToUpdate(DateTime lastUpdateTime, int page, CancellationToken cancellationToken)
        {
            bool hasMorePages;
            var list = new List<string>();

            // First get last time
            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = string.Format(UpdatesUrl, lastUpdateTime.ToString("yyyy-MM-dd"), MovieDbProvider.ApiKey, page),
                CancellationToken = cancellationToken,
                EnableHttpCompression = true,
                ResourcePool = MovieDbProvider.Current.MovieDbResourcePool,
                AcceptHeader = MovieDbProvider.AcceptHeader

            }).ConfigureAwait(false))
            {
                var obj = _json.DeserializeFromStream<RootObject>(stream);

                var data = obj.results.Select(i => i.id.ToString(UsCulture));

                list.AddRange(data);

                hasMorePages = page < obj.total_pages;
            }

            if (hasMorePages)
            {
                var more = await GetIdsToUpdate(lastUpdateTime, page + 1, cancellationToken).ConfigureAwait(false);

                list.AddRange(more);
            }

            return list;
        }

        /// <summary>
        /// Updates the movies.
        /// </summary>
        /// <param name="ids">The ids.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task UpdateMovies(IEnumerable<string> ids, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var list = ids.ToList();
            var numComplete = 0;

            // Gather all movies into a lookup by tmdb id
            var allMovies = _libraryManager.RootFolder
                .GetRecursiveChildren(i => i is Movie && !string.IsNullOrEmpty(i.GetProviderId(MetadataProviders.Tmdb)))
                .ToLookup(i => i.GetProviderId(MetadataProviders.Tmdb));

            foreach (var id in list)
            {
                // Find the preferred language(s) for the movie in the library
                var languages = allMovies[id]
                    .Select(i => i.GetPreferredMetadataLanguage())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var language in languages)
                {
                    try
                    {
                        await UpdateMovie(id, language, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error updating tmdb movie id {0}, language {1}", ex, id, language);
                    }
                }

                numComplete++;
                double percent = numComplete;
                percent /= list.Count;
                percent *= 100;

                progress.Report(percent);
            }
        }

        /// <summary>
        /// Updates the movie.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="preferredMetadataLanguage">The preferred metadata language.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private Task UpdateMovie(string id, string preferredMetadataLanguage, CancellationToken cancellationToken)
        {
            _logger.Info("Updating movie from tmdb " + id + ", language " + preferredMetadataLanguage);

            return MovieDbProvider.Current.DownloadMovieInfo(id, preferredMetadataLanguage, cancellationToken);
        }

        class Result
        {
            public int id { get; set; }
            public bool? adult { get; set; }
        }

        class RootObject
        {
            public List<Result> results { get; set; }
            public int page { get; set; }
            public int total_pages { get; set; }
            public int total_results { get; set; }

            public RootObject()
            {
                results = new List<Result>();
            }
        }
    }
}
