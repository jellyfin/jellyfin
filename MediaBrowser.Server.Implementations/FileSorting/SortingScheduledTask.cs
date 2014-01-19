using MediaBrowser.Common.IO;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.FileSorting
{
    public class SortingScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;

        public SortingScheduledTask(IServerConfigurationManager config, ILogger logger, ILibraryManager libraryManager, IFileSystem fileSystem)
        {
            _config = config;
            _logger = logger;
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
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
            new TvFileSorter(_libraryManager, _logger, _fileSystem).Sort(_config.Configuration.FileSortingOptions, cancellationToken, progress);
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
