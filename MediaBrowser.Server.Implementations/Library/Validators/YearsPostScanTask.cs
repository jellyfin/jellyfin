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
            var yearNumber = 1900;
            var maxYear = DateTime.UtcNow.Year + 3;
            var count = maxYear - yearNumber + 1;
            var numComplete = 0;

            while (yearNumber < maxYear)
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
                yearNumber++;
            }
        }
    }
}
