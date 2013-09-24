using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.TV
{
    class SeriesPostScanTask : ILibraryPostScanTask
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        public SeriesPostScanTask(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            RunInternal(progress, cancellationToken);

            return Task.FromResult(true);
        }

        private void RunInternal(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var seriesList = _libraryManager.RootFolder
                .RecursiveChildren
                .OfType<Series>()
                .ToList();

            var numComplete = 0;

            foreach (var series in seriesList)
            {
                cancellationToken.ThrowIfCancellationRequested();

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

                numComplete++;
                double percent = numComplete;
                percent /= seriesList.Count;
                percent *= 100;

                progress.Report(percent);
            }
        }
    }
}
