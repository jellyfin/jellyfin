using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.TV
{
    /// <summary>
    /// Class TvdbPrescanTask
    /// </summary>
    public class TvdbPrescanTask : ILibraryPrescanTask
    {
        /// <summary>
        /// The server time URL
        /// </summary>
        private const string ServerTimeUrl = "http://thetvdb.com/api/Updates.php?type=none";

        /// <summary>
        /// The updates URL
        /// </summary>
        private const string UpdatesUrl = "http://thetvdb.com/api/Updates.php?type=all&time={0}";

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

        /// <summary>
        /// Initializes a new instance of the <see cref="TvdbPrescanTask"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="config">The config.</param>
        public TvdbPrescanTask(ILogger logger, IHttpClient httpClient, IServerConfigurationManager config)
        {
            _logger = logger;
            _httpClient = httpClient;
            _config = config;
        }

        /// <summary>
        /// Runs the specified progress.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (!_config.Configuration.EnableInternetProviders)
            {
                progress.Report(100);
                return;
            }

            var path = RemoteSeriesProvider.GetSeriesDataPath(_config.CommonApplicationPaths);

            var timestampFile = Path.Combine(path, "time.txt");

            var timestampFileInfo = new FileInfo(timestampFile);

            // Don't check for tvdb updates anymore frequently than 24 hours
            if (timestampFileInfo.Exists && (DateTime.UtcNow - timestampFileInfo.LastWriteTimeUtc).TotalDays < 1)
            {
                return;
            }

            // Find out the last time we queried tvdb for updates
            var lastUpdateTime = timestampFileInfo.Exists ? File.ReadAllText(timestampFile, Encoding.UTF8) : string.Empty;

            string newUpdateTime;

            var existingDirectories = Directory.EnumerateDirectories(path).Select(Path.GetFileName).ToList();

            // If this is our first time, update all series
            if (string.IsNullOrEmpty(lastUpdateTime))
            {
                // First get tvdb server time
                using (var stream = await _httpClient.Get(new HttpRequestOptions
                {
                    Url = ServerTimeUrl,
                    CancellationToken = cancellationToken,
                    EnableHttpCompression = true,
                    ResourcePool = RemoteSeriesProvider.Current.TvDbResourcePool

                }).ConfigureAwait(false))
                {
                    newUpdateTime = GetUpdateTime(stream);
                }

                await UpdateSeries(existingDirectories, path, progress, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var seriesToUpdate = await GetSeriesIdsToUpdate(existingDirectories, lastUpdateTime, cancellationToken).ConfigureAwait(false);

                newUpdateTime = seriesToUpdate.Item2;

                await UpdateSeries(seriesToUpdate.Item1, path, progress, cancellationToken).ConfigureAwait(false);
            }

            File.WriteAllText(timestampFile, newUpdateTime, Encoding.UTF8);
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
                ResourcePool = RemoteSeriesProvider.Current.TvDbResourcePool

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
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task UpdateSeries(IEnumerable<string> seriesIds, string seriesDataPath, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var list = seriesIds.ToList();
            var numComplete = 0;

            foreach (var seriesId in list)
            {
                try
                {
                    await UpdateSeries(seriesId, seriesDataPath, cancellationToken).ConfigureAwait(false);
                }
                catch (HttpException ex)
                {
                    // Already logged at lower levels, but don't fail the whole operation, unless timed out
                    // We have to fail this to make it run again otherwise new episode data could potentially be missing
                    if (ex.IsTimedOut)
                    {
                        throw;
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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private Task UpdateSeries(string id, string seriesDataPath, CancellationToken cancellationToken)
        {
            _logger.Info("Updating series " + id);

            seriesDataPath = Path.Combine(seriesDataPath, id);

            if (!Directory.Exists(seriesDataPath))
            {
                Directory.CreateDirectory(seriesDataPath);
            }

            return RemoteSeriesProvider.Current.DownloadSeriesZip(id, seriesDataPath, cancellationToken);
        }
    }
}
