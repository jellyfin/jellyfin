using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.TV
{
    class SeriesPostScanTask : ILibraryPostScanTask
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly IDirectoryWatchers _directoryWatchers;

        public SeriesPostScanTask(ILibraryManager libraryManager, ILogger logger, IDirectoryWatchers directoryWatchers, IServerConfigurationManager config)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _directoryWatchers = directoryWatchers;
            _config = config;
        }

        public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return RunInternal(progress, cancellationToken);
        }

        private async Task RunInternal(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var seriesList = _libraryManager.RootFolder
                .RecursiveChildren
                .OfType<Series>()
                .ToList();

            var numComplete = 0;

            foreach (var series in seriesList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await new MissingEpisodeProvider(_logger, _directoryWatchers, _config).Run(series, cancellationToken).ConfigureAwait(false);

                var episodes = series.RecursiveChildren
                    .OfType<Episode>()
                    .ToList();

                series.SpecialFeatureIds = episodes
                    .Where(i => i.ParentIndexNumber.HasValue && i.ParentIndexNumber.Value == 0)
                    .Select(i => i.Id)
                    .ToList();

                series.SeasonCount = episodes
                    .Select(i => i.ParentIndexNumber ?? 0)
                    .Where(i => i != 0)
                    .Distinct()
                    .Count();

                series.DateLastEpisodeAdded = episodes.Select(i => i.DateCreated)
                    .OrderByDescending(i => i)
                    .FirstOrDefault();

                numComplete++;
                double percent = numComplete;
                percent /= seriesList.Count;
                percent *= 100;

                progress.Report(percent);
            }
        }
    }

    class MissingEpisodeProvider
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly IDirectoryWatchers _directoryWatchers;

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        public MissingEpisodeProvider(ILogger logger, IDirectoryWatchers directoryWatchers, IServerConfigurationManager config)
        {
            _logger = logger;
            _directoryWatchers = directoryWatchers;
            _config = config;
        }

        public async Task Run(Series series, CancellationToken cancellationToken)
        {
            var tvdbId = series.GetProviderId(MetadataProviders.Tvdb);

            // Can't proceed without a tvdb id
            if (string.IsNullOrEmpty(tvdbId))
            {
                return;
            }

            var seriesDataPath = RemoteSeriesProvider.GetSeriesDataPath(_config.ApplicationPaths, tvdbId);

            var episodeFiles = Directory.EnumerateFiles(seriesDataPath, "*.xml", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(i => i.StartsWith("episode-", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var episodeLookup = episodeFiles
                .Select(i =>
                {
                    var parts = i.Split('-');

                    if (parts.Length == 3)
                    {
                        var seasonNumberString = parts[1];

                        int seasonNumber;

                        if (int.TryParse(seasonNumberString, NumberStyles.Integer, UsCulture, out seasonNumber))
                        {
                            var episodeNumberString = parts[2];

                            int episodeNumber;

                            if (int.TryParse(episodeNumberString, NumberStyles.Integer, UsCulture, out episodeNumber))
                            {
                                return new Tuple<int, int>(seasonNumber, episodeNumber);
                            }
                        }
                    }

                    return new Tuple<int, int>(-1, -1);
                })
                .Where(i => i.Item1 != -1 && i.Item2 != -1)
                .ToList();

            var existingEpisodes = series.RecursiveChildren
                .OfType<Episode>()
                .Where(i => i.IndexNumber.HasValue && i.ParentIndexNumber.HasValue)
                .ToList();

            var hasChanges = false;

            if (_config.Configuration.CreateVirtualMissingEpisodes || _config.Configuration.CreateVirtualFutureEpisodes)
            {
                if (_config.Configuration.EnableInternetProviders)
                {
                    hasChanges = await AddMissingEpisodes(series, seriesDataPath, existingEpisodes, episodeLookup, cancellationToken).ConfigureAwait(false);
                }
            }

            var anyRemoved = await RemoveObsoleteMissingEpsiodes(series, existingEpisodes, cancellationToken).ConfigureAwait(false);

            if (hasChanges || anyRemoved)
            {
                await series.RefreshMetadata(cancellationToken, true).ConfigureAwait(false);
                await series.ValidateChildren(new Progress<double>(), cancellationToken, true).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Adds the missing episodes.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="seriesDataPath">The series data path.</param>
        /// <param name="existingEpisodes">The existing episodes.</param>
        /// <param name="episodeLookup">The episode lookup.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task<bool> AddMissingEpisodes(Series series, string seriesDataPath, List<Episode> existingEpisodes, IEnumerable<Tuple<int, int>> episodeLookup, CancellationToken cancellationToken)
        {
            var hasChanges = false;
            
            foreach (var tuple in episodeLookup)
            {
                if (tuple.Item1 <= 0)
                {
                    // Ignore season zeros
                    continue;
                }

                if (tuple.Item2 <= 0)
                {
                    // Ignore episode zeros
                    continue;
                }

                var existingEpisode = GetExistingEpisode(existingEpisodes, tuple);

                if (existingEpisode != null)
                {
                    continue;
                }

                var airDate = GetAirDate(seriesDataPath, tuple.Item1, tuple.Item2);

                if (!airDate.HasValue)
                {
                    continue;
                }

                if (airDate.Value < DateTime.UtcNow && _config.Configuration.CreateVirtualMissingEpisodes)
                {
                    // tvdb has a lot of nearly blank episodes
                    _logger.Info("Creating virtual missing episode {0} {1}x{2}", series.Name, tuple.Item1, tuple.Item2);

                    await AddEpisode(series, tuple.Item1, tuple.Item2, cancellationToken).ConfigureAwait(false);

                    hasChanges = true;
                }
                else if (airDate.Value > DateTime.UtcNow && _config.Configuration.CreateVirtualFutureEpisodes)
                {
                    // tvdb has a lot of nearly blank episodes
                    _logger.Info("Creating virtual future episode {0} {1}x{2}", series.Name, tuple.Item1, tuple.Item2);

                    await AddEpisode(series, tuple.Item1, tuple.Item2, cancellationToken).ConfigureAwait(false);

                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        /// <summary>
        /// Removes the virtual entry after a corresponding physical version has been added
        /// </summary>
        private async Task<bool> RemoveObsoleteMissingEpsiodes(Series series, List<Episode> existingEpisodes, CancellationToken cancellationToken)
        {
            var physicalEpisodes = existingEpisodes
                .Where(i => i.LocationType != LocationType.Virtual)
                .ToList();

            var episodesToRemove = existingEpisodes
                .Where(i => i.LocationType == LocationType.Virtual)
                .Where(i =>
                {
                    if (i.IndexNumber.HasValue && i.ParentIndexNumber.HasValue)
                    {
                        return physicalEpisodes.Any(p => p.ParentIndexNumber.HasValue && p.ParentIndexNumber.Value == i.ParentIndexNumber.Value && p.ContainsEpisodeNumber(i.IndexNumber.Value));
                    }

                    return false;
                })
                .ToList();

            var hasChanges = false;

            foreach (var episodeToRemove in episodesToRemove)
            {
                _logger.Info("Removing {0} {1}x{2}", series.Name, episodeToRemove.ParentIndexNumber, episodeToRemove.IndexNumber);

                await episodeToRemove.Parent.RemoveChild(episodeToRemove, cancellationToken).ConfigureAwait(false);

                hasChanges = true;
            }

            return hasChanges;
        }

        /// <summary>
        /// Adds the episode.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="episodeNumber">The episode number.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task AddEpisode(Series series, int seasonNumber, int episodeNumber, CancellationToken cancellationToken)
        {
            var season = series.Children.OfType<Season>().FirstOrDefault(i => i.IndexNumber.HasValue && i.IndexNumber.Value == seasonNumber);

            if (season == null)
            {
                season = await AddSeason(series, seasonNumber, cancellationToken).ConfigureAwait(false);
            }

            var name = string.Format("Episode {0}", episodeNumber.ToString(UsCulture));

            var episode = new Episode
            {
                Name = string.Format("Episode {0}", episodeNumber.ToString(UsCulture)),
                IndexNumber = episodeNumber,
                ParentIndexNumber = seasonNumber,
                Parent = season,
                DisplayMediaType = typeof(Episode).Name,
                Id = (series.Id + name).GetMBId(typeof(Episode))
            };

            await season.AddChild(episode, cancellationToken).ConfigureAwait(false);

            await episode.RefreshMetadata(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds the season.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Season}.</returns>
        private async Task<Season> AddSeason(Series series, int seasonNumber, CancellationToken cancellationToken)
        {
            _logger.Info("Creating Season {0} entry for {1}", seasonNumber, series.Name);

            var name = string.Format("Season {0}", seasonNumber.ToString(UsCulture));

            var path = Path.Combine(series.Path, name);

            var season = new Season
            {
                Name = name,
                IndexNumber = seasonNumber,
                Path = path,
                Parent = series,
                DisplayMediaType = typeof(Season).Name
            };

            _directoryWatchers.TemporarilyIgnore(path);

            try
            {
                var info = Directory.CreateDirectory(path);

                season.DateCreated = info.CreationTimeUtc;
                season.DateModified = info.LastWriteTimeUtc;

                await series.AddChild(season, cancellationToken).ConfigureAwait(false);

                await season.RefreshMetadata(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _directoryWatchers.RemoveTempIgnore(path);
            }


            return season;
        }

        /// <summary>
        /// Gets the existing episode.
        /// </summary>
        /// <param name="existingEpisodes">The existing episodes.</param>
        /// <param name="tuple">The tuple.</param>
        /// <returns>Episode.</returns>
        private Episode GetExistingEpisode(IEnumerable<Episode> existingEpisodes, Tuple<int, int> tuple)
        {
            return existingEpisodes
                .FirstOrDefault(i => (i.ParentIndexNumber ?? -1) == tuple.Item1 && i.ContainsEpisodeNumber(tuple.Item2));
        }

        /// <summary>
        /// Gets the air date.
        /// </summary>
        /// <param name="seriesDataPath">The series data path.</param>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="episodeNumber">The episode number.</param>
        /// <returns>System.Nullable{DateTime}.</returns>
        private DateTime? GetAirDate(string seriesDataPath, int seasonNumber, int episodeNumber)
        {
            // First open up the tvdb xml file and make sure it has valid data
            var filename = string.Format("episode-{0}-{1}.xml", seasonNumber.ToString(UsCulture), episodeNumber.ToString(UsCulture));

            var xmlPath = Path.Combine(seriesDataPath, filename);

            // It appears the best way to filter out invalid entries is to only include those with valid air dates

            using (var streamReader = new StreamReader(xmlPath, Encoding.UTF8))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, new XmlReaderSettings
                {
                    CheckCharacters = false,
                    IgnoreProcessingInstructions = true,
                    IgnoreComments = true,
                    ValidationType = ValidationType.None
                }))
                {
                    reader.MoveToContent();

                    // Loop through each element
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.Name)
                            {
                                case "FirstAired":
                                    {
                                        var val = reader.ReadElementContentAsString();

                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            DateTime date;
                                            if (DateTime.TryParse(val, out date))
                                            {
                                                return date.ToUniversalTime();
                                            }
                                        }

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

            return null;
        }
    }
}
