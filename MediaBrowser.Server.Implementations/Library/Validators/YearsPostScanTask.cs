using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Library.Validators
{
    public class YearsPostScanTask : ILibraryPostScanTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;

        public YearsPostScanTask(ILibraryManager libraryManager, ILogger logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
        }

        public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var allYears = _libraryManager.RootFolder.GetRecursiveChildren(i => i.ProductionYear.HasValue)
                .Select(i => i.ProductionYear ?? -1)
                .Where(i => i > 0)
                .Distinct()
                .ToList();

            var count = allYears.Count;
            var numComplete = 0;

            foreach (var yearNumber in allYears)
            {
                try
                {
                    var year = _libraryManager.GetYear(yearNumber);

                    await year.RefreshMetadata(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Don't clutter the log
                    break;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error refreshing year {0}", ex, yearNumber);
                }

                numComplete++;
                double percent = numComplete;
                percent /= count;
                percent *= 100;

                progress.Report(percent);
            }
        }
    }
}
