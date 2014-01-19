using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.FileSorting
{
    public class SortingScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;

        public SortingScheduledTask(IServerConfigurationManager config, ILogger logger, ILibraryManager libraryManager)
        {
            _config = config;
            _logger = logger;
            _libraryManager = libraryManager;
        }

        public string Name
        {
            get { return "Sort new files"; }
        }

        public string Description
        {
            get { return "Processes new files available in the configured sorting location."; }
        }

        public string Category
        {
            get { return "Library"; }
        }

        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return Task.Run(() => SortFiles(cancellationToken, progress), cancellationToken);
        }

        private void SortFiles(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var numComplete = 0;

            var paths = _config.Configuration.FileSortingOptions.TvWatchLocations.ToList();

            foreach (var path in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    SortFiles(path);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error sorting files from {0}", ex, path);
                }

                numComplete++;
                double percent = numComplete;
                percent /= paths.Count;

                progress.Report(100 * percent);
            }
        }

        private void SortFiles(string path)
        {
            new TvFileSorter(_libraryManager, _logger).Sort(path, _config.Configuration.FileSortingOptions);
        }

        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[]
                {
                    new IntervalTrigger{ Interval = TimeSpan.FromMinutes(5)}
                };
        }

        public bool IsHidden
        {
            get { return !_config.Configuration.FileSortingOptions.IsEnabled; }
        }

        public bool IsEnabled
        {
            get { return _config.Configuration.FileSortingOptions.IsEnabled; }
        }
    }
}
