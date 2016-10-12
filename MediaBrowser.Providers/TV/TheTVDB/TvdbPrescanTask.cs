using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CommonIO;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Providers.TV
{
    /// <summary>
    /// Class TvdbPrescanTask
    /// </summary>
    public class TvdbPrescanTask : ILibraryPostScanTask
    {
        /// <summary>
        /// The server time URL
        /// </summary>
        private const string ServerTimeUrl = "https://thetvdb.com/api/Updates.php?type=none";

        /// <summary>
        /// The updates URL
        /// </summary>
        private const string UpdatesUrl = "https://thetvdb.com/api/Updates.php?type=all&time={0}";

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
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TvdbPrescanTask"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="config">The config.</param>
        public TvdbPrescanTask(ILogger logger, IHttpClient httpClient, IServerConfigurationManager config, IFileSystem fileSystem, ILibraryManager libraryManager)
        {
            _logger = logger;
            _httpClient = httpClient;
            _config = config;
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
            var seriesConfig = _config.Configuration.MetadataOptions.FirstOrDefault(i => string.Equals(i.ItemType, typeof(Series).Name, StringComparison.OrdinalIgnoreCase));

            if (seriesConfig != null && seriesConfig.DisabledMetadataFetchers.Contains(TvdbSeriesProvider.Current.Name, StringComparer.OrdinalIgnoreCase))
            {
                progress.Report(100);
                return;
            }

            var path = TvdbSeriesProvider.GetSeriesDataPath(_config.CommonApplicationPaths);

            _fileSystem.CreateDirectory(path);

            var timestampFile = Path.Combine(path, "time.txt");

            var timestampFileInfo = _fileSystem.GetFileInfo(timestampFile);

            // Don't check for tvdb updates anymore frequently than 24 hours
            if (timestampFileInfo.Exists && (DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(timestampFileInfo)).TotalDays < 1)
            {
                return;
            }

            // Find out the last time we queried tvdb for updates
            var lastUpdateTime = timestampFileInfo.Exists ? _fileSystem.ReadAllText(timestampFile, Encoding.UTF8) : string.Empty;

            string newUpdateTime;

            var existingDirectories = Directory.EnumerateDirectories(path)
                .Select(Path.GetFileName)
                .ToList();

            var seriesList = _libraryManager.GetItemList(new InternalItemsQuery()
            {
                IncludeItemTypes = new[] { typeof(Series).Name },
                Recursive = true,
                GroupByPresentationUniqueKey = false

            }).Cast<Series>()
            .ToList();

            var seriesIdsInLibrary = seriesList
               .Where(i => !string.IsNullOrEmpty(i.GetProviderId(MetadataProviders.Tvdb)))
               .Select(i => i.GetProviderId(MetadataProviders.Tvdb))
               .ToList();

            var missingSeries = seriesIdsInLibrary.Except(existingDirectories, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var enableInternetProviders = seriesList.Count == 0 ? false : seriesList[0].IsInternetMetadataEnabled();
            if (!enableInternetProviders)
            {
                progress.Report(100);
                return;
            }

            // If this is our first time, update all series
            if (string.IsNullOrEmpty(lastUpdateTime))
            {
                // First get tvdb server time
                using (var stream = await _httpClient.Get(new HttpRequestOptions
                {
                    Url = ServerTimeUrl,
                    CancellationToken = cancellationToken,
                    EnableHttpCompression = true,
                    ResourcePool = TvdbSeriesProvider.Current.TvDbResourcePool

                }).ConfigureAwait(false))
                {
                    newUpdateTime = GetUpdateTime(stream);
                }

                existingDirectories.AddRange(missingSeries);

                await UpdateSeries(existingDirectories, path, null, progress, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var seriesToUpdate = await GetSeriesIdsToUpdate(existingDirectories, lastUpdateTime, cancellationToken).ConfigureAwait(false);

                newUpdateTime = seriesToUpdate.Item2;

                long lastUpdateValue;

                long.TryParse(lastUpdateTime, NumberStyles.Any, UsCulture, out lastUpdateValue);

                var nullableUpdateValue = lastUpdateValue == 0 ? (long?)null : lastUpdateValue;

                var listToUpdate = seriesToUpdate.Item1.ToList();
                listToUpdate.AddRange(missingSeries);

                await UpdateSeries(listToUpdate, path, nullableUpdateValue, progress, cancellationToken).ConfigureAwait(false);
            }

            _fileSystem.WriteAllText(timestampFile, newUpdateTime, Encoding.UTF8);
            progress.Report(100);
        }

        /// <summary>
        /// Gets the update time.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns>System.String.</returns>
        private string GetUpdateTime(Stream response)
        {
            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            using (var streamReader = new StreamReader(response, Encoding.UTF8))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, settings))
                {
                    reader.MoveToContent();

                    // Loop through each element
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.Name)
                            {
                                case "Time":
                                    {
                                        return (reader.ReadElementContentAsString() ?? string.Empty).Trim();
                                    }
                                default:
                                    reader.Skip();
                                    break;
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the series ids to update.
        /// </summary>
        /// <param name="existingSeriesIds">The existing series ids.</param>
        /// <param name="lastUpdateTime">The last update time.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{System.String}}.</returns>
        private async Task<Tuple<IEnumerable<string>, string>> GetSeriesIdsToUpdate(IEnumerable<string> existingSeriesIds, string lastUpdateTime, CancellationToken cancellationToken)
        {
            // First get last time
            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = string.Format(UpdatesUrl, lastUpdateTime),
                CancellationToken = cancellationToken,
                EnableHttpCompression = true,
                ResourcePool = TvdbSeriesProvider.Current.TvDbResourcePool

            }).ConfigureAwait(false))
            {
                var data = GetUpdatedSeriesIdList(stream);

                var existingDictionary = existingSeriesIds.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

                var seriesList = data.Item1
                    .Where(i => !string.IsNullOrWhiteSpace(i) && existingDictionary.ContainsKey(i));

                return new Tuple<IEnumerable<string>, string>(seriesList, data.Item2);
            }
        }

        private Tuple<List<string>, string> GetUpdatedSeriesIdList(Stream stream)
        {
            string updateTime = null;
            var idList = new List<string>();

            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            using (var streamReader = new StreamReader(stream, Encoding.UTF8))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, settings))
                {
                    reader.MoveToContent();

                    // Loop through each element
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.Name)
                            {
                                case "Time":
                                    {
                                        updateTime = (reader.ReadElementContentAsString() ?? string.Empty).Trim();
                                        break;
                                    }
                                case "Series":
                                    {
                                        var id = (reader.ReadElementContentAsString() ?? string.Empty).Trim();
                                        idList.Add(id);
                                        break;
                                    }
                                default:
                                    reader.Skip();
                                    break;
                            }
                        }
                    }
                }
            }

            return new Tuple<List<string>, string>(idList, updateTime);
        }

        /// <summary>
        /// Updates the series.
        /// </summary>
        /// <param name="seriesIds">The series ids.</param>
        /// <param name="seriesDataPath">The series data path.</param>
        /// <param name="lastTvDbUpdateTime">The last tv db update time.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task UpdateSeries(IEnumerable<string> seriesIds, string seriesDataPath, long? lastTvDbUpdateTime, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var list = seriesIds.ToList();
            var numComplete = 0;

            var seriesList = _libraryManager.GetItemList(new InternalItemsQuery()
            {
                IncludeItemTypes = new[] { typeof(Series).Name },
                Recursive = true,
                GroupByPresentationUniqueKey = false

            }).Cast<Series>();

            // Gather all series into a lookup by tvdb id
            var allSeries = seriesList
                .Where(i => !string.IsNullOrEmpty(i.GetProviderId(MetadataProviders.Tvdb)))
                .ToLookup(i => i.GetProviderId(MetadataProviders.Tvdb));

            foreach (var seriesId in list)
            {
                // Find the preferred language(s) for the movie in the library
                var languages = allSeries[seriesId]
                    .Select(i => i.GetPreferredMetadataLanguage())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var language in languages)
                {
                    try
                    {
                        await UpdateSeries(seriesId, seriesDataPath, lastTvDbUpdateTime, language, cancellationToken).ConfigureAwait(false);
                    }
                    catch (HttpException ex)
                    {
                        _logger.ErrorException("Error updating tvdb series id {0}, language {1}", ex, seriesId, language);

                        // Already logged at lower levels, but don't fail the whole operation, unless timed out
                        // We have to fail this to make it run again otherwise new episode data could potentially be missing
                        if (ex.IsTimedOut)
                        {
                            throw;
                        }
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
        /// Updates the series.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="seriesDataPath">The series data path.</param>
        /// <param name="lastTvDbUpdateTime">The last tv db update time.</param>
        /// <param name="preferredMetadataLanguage">The preferred metadata language.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private Task UpdateSeries(string id, string seriesDataPath, long? lastTvDbUpdateTime, string preferredMetadataLanguage, CancellationToken cancellationToken)
        {
            _logger.Info("Updating series from tvdb " + id + ", language " + preferredMetadataLanguage);

            seriesDataPath = Path.Combine(seriesDataPath, id);

            _fileSystem.CreateDirectory(seriesDataPath);

            return TvdbSeriesProvider.Current.DownloadSeriesZip(id, MetadataProviders.Tvdb.ToString(), seriesDataPath, lastTvDbUpdateTime, preferredMetadataLanguage, cancellationToken);
        }
    }
}
