using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Tasks;

namespace MediaBrowser.Providers.TV
{
    class SeriesGroup : List<Series>, IGrouping<string, Series>
    {
        public string Key { get; set; }
    }

    class SeriesPostScanTask : ILibraryPostScanTask, IHasOrder
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly ILocalizationManager _localization;
        private readonly IFileSystem _fileSystem;

        public SeriesPostScanTask(ILibraryManager libraryManager, ILogger logger, IServerConfigurationManager config, ILocalizationManager localization, IFileSystem fileSystem)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _config = config;
            _localization = localization;
            _fileSystem = fileSystem;
        }

        public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return RunInternal(progress, cancellationToken);
        }

        private async Task RunInternal(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var seriesList = _libraryManager.GetItemList(new InternalItemsQuery()
            {
                IncludeItemTypes = new[] { typeof(Series).Name },
                Recursive = true,
                GroupByPresentationUniqueKey = false

            }).Cast<Series>().ToList();

            var seriesGroups = FindSeriesGroups(seriesList).Where(g => !string.IsNullOrEmpty(g.Key)).ToList();

            await new MissingEpisodeProvider(_logger, _config, _libraryManager, _localization, _fileSystem)
                .Run(seriesGroups, true, cancellationToken).ConfigureAwait(false);

            var numComplete = 0;

            foreach (var series in seriesList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var episodes = series.GetRecursiveChildren(i => i is Episode)
                    .Cast<Episode>()
                    .ToList();

                var physicalEpisodes = episodes.Where(i => i.LocationType != LocationType.Virtual)
                    .ToList();

                series.SpecialFeatureIds = physicalEpisodes
                    .Where(i => i.ParentIndexNumber.HasValue && i.ParentIndexNumber.Value == 0)
                    .Select(i => i.Id)
                    .ToList();

                numComplete++;
                double percent = numComplete;
                percent /= seriesList.Count;
                percent *= 100;

                progress.Report(percent);
            }
        }

        internal static IEnumerable<IGrouping<string, Series>> FindSeriesGroups(List<Series> seriesList)
        {
            var links = seriesList.ToDictionary(s => s, s => seriesList.Where(c => c != s && ShareProviderId(s, c)).ToList());

            var visited = new HashSet<Series>();

            foreach (var series in seriesList)
            {
                if (!visited.Contains(series))
                {
                    var group = new SeriesGroup();
                    FindAllLinked(series, visited, links, group);

                    group.Key = group.Select(s => s.GetProviderId(MetadataProviders.Tvdb)).FirstOrDefault(id => !string.IsNullOrEmpty(id));

                    yield return group;
                }
            }
        }

        private static void FindAllLinked(Series series, HashSet<Series> visited, IDictionary<Series, List<Series>> linksMap, List<Series> results)
        {
            results.Add(series);
            visited.Add(series);

            var links = linksMap[series];

            foreach (var s in links)
            {
                if (!visited.Contains(s))
                {
                    FindAllLinked(s, visited, linksMap, results);
                }
            }
        }

        private static bool ShareProviderId(Series a, Series b)
        {
            return a.ProviderIds.Any(id =>
            {
                string value;
                return b.ProviderIds.TryGetValue(id.Key, out value) && id.Value == value;
            });
        }

        public int Order
        {
            get
            {
                // Run after tvdb update task
                return 1;
            }
        }
    }

    public class CleanMissingEpisodesEntryPoint : IServerEntryPoint
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly ILocalizationManager _localization;
        private readonly IFileSystem _fileSystem;
        private readonly object _libraryChangedSyncLock = new object();
        private const int LibraryUpdateDuration = 180000;
        private readonly ITaskManager _taskManager;

        public CleanMissingEpisodesEntryPoint(ILibraryManager libraryManager, IServerConfigurationManager config, ILogger logger, ILocalizationManager localization, IFileSystem fileSystem, ITaskManager taskManager)
        {
            _libraryManager = libraryManager;
            _config = config;
            _logger = logger;
            _localization = localization;
            _fileSystem = fileSystem;
            _taskManager = taskManager;
        }

        private Timer LibraryUpdateTimer { get; set; }

        public void Run()
        {
            _libraryManager.ItemAdded += _libraryManager_ItemAdded;
        }

        private void _libraryManager_ItemAdded(object sender, ItemChangeEventArgs e)
        {
            if (!FilterItem(e.Item))
            {
                return;
            }

            lock (_libraryChangedSyncLock)
            {
                if (LibraryUpdateTimer == null)
                {
                    LibraryUpdateTimer = new Timer(LibraryUpdateTimerCallback, null, LibraryUpdateDuration, Timeout.Infinite);
                }
                else
                {
                    LibraryUpdateTimer.Change(LibraryUpdateDuration, Timeout.Infinite);
                }
            }
        }

        private async void LibraryUpdateTimerCallback(object state)
        {
            try
            {
                if (MissingEpisodeProvider.IsRunning)
                {
                    return;
                }

                if (_libraryManager.IsScanRunning)
                {
                    return;
                }

                var seriesList = _libraryManager.GetItemList(new InternalItemsQuery()
                {
                    IncludeItemTypes = new[] { typeof(Series).Name },
                    Recursive = true,
                    GroupByPresentationUniqueKey = false

                }).Cast<Series>().ToList();

                var seriesGroups = SeriesPostScanTask.FindSeriesGroups(seriesList).Where(g => !string.IsNullOrEmpty(g.Key)).ToList();

                await new MissingEpisodeProvider(_logger, _config, _libraryManager, _localization, _fileSystem)
                    .Run(seriesGroups, false, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in SeriesPostScanTask", ex);
            }
        }

        private bool FilterItem(BaseItem item)
        {
            return item is Episode && item.LocationType != LocationType.Virtual;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                if (LibraryUpdateTimer != null)
                {
                    LibraryUpdateTimer.Dispose();
                    LibraryUpdateTimer = null;
                }

                _libraryManager.ItemAdded -= _libraryManager_ItemAdded;
            }
        }
    }
}
