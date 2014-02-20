using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.TV
{
    class SeriesPostScanTask : ILibraryPostScanTask, IHasOrder
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;

        public SeriesPostScanTask(ILibraryManager libraryManager, ILogger logger, IServerConfigurationManager config)
        {
            _libraryManager = libraryManager;
            _logger = logger;
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

            var seriesGroups = from series in seriesList
                               let tvdbId = series.GetProviderId(MetadataProviders.Tvdb)
                               where !string.IsNullOrEmpty(tvdbId)
                               group series by tvdbId into g
                               select g;

            await new MissingEpisodeProvider(_logger, _config, _libraryManager).Run(seriesGroups, cancellationToken).ConfigureAwait(false);

            var numComplete = 0;

            foreach (var series in seriesList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var episodes = series.RecursiveChildren
                    .OfType<Episode>()
                    .ToList();

                var physicalEpisodes = episodes.Where(i => i.LocationType != LocationType.Virtual)
                    .ToList();

                series.SeasonCount = episodes
                    .Select(i => i.ParentIndexNumber ?? 0)
                    .Where(i => i != 0)
                    .Distinct()
                    .Count();

                series.SpecialFeatureIds = physicalEpisodes
                    .Where(i => i.ParentIndexNumber.HasValue && i.ParentIndexNumber.Value == 0)
                    .Select(i => i.Id)
                    .ToList();

                series.DateLastEpisodeAdded = physicalEpisodes.Select(i => i.DateCreated)
                    .OrderByDescending(i => i)
                    .FirstOrDefault();

                numComplete++;
                double percent = numComplete;
                percent /= seriesList.Count;
                percent *= 100;

                progress.Report(percent);
            }
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
