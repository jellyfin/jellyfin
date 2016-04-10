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
            var seriesList = _libraryManager.RootFolder
                .GetRecursiveChildren(i => i is Series)
                .Cast<Series>()
                .ToList();

            var seriesGroups = FindSeriesGroups(seriesList).Where(g => !string.IsNullOrEmpty(g.Key)).ToList();

            await new MissingEpisodeProvider(_logger, _config, _libraryManager, _localization, _fileSystem).Run(seriesGroups, cancellationToken).ConfigureAwait(false);

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

        private IEnumerable<IGrouping<string, Series>> FindSeriesGroups(List<Series> seriesList)
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

        private void FindAllLinked(Series series, HashSet<Series> visited, IDictionary<Series, List<Series>> linksMap, List<Series> results)
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

        private bool ShareProviderId(Series a, Series b)
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

}
