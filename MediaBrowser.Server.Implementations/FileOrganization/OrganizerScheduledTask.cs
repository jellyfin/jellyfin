using MediaBrowser.Common.IO;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.FileOrganization;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.FileOrganization
{
    public class OrganizerScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;
        private readonly IFileOrganizationService _iFileSortingRepository;

        public OrganizerScheduledTask(IServerConfigurationManager config, ILogger logger, ILibraryManager libraryManager, IFileSystem fileSystem, IFileOrganizationService iFileSortingRepository)
        {
            _config = config;
            _logger = logger;
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
            _iFileSortingRepository = iFileSortingRepository;
        }

        public string Name
        {
            get { return "Organize new media files"; }
        }

        public string Description
        {
            get { return "Processes new files available in the configured watch folder."; }
        }

        public string Category
        {
            get { return "Library"; }
        }

        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return new TvFileSorter(_libraryManager, _logger, _fileSystem, _iFileSortingRepository).Sort(_config.Configuration.TvFileOrganizationOptions, cancellationToken, progress);
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
            get { return !_config.Configuration.TvFileOrganizationOptions.IsEnabled; }
        }

        public bool IsEnabled
        {
            get { return _config.Configuration.TvFileOrganizationOptions.IsEnabled; }
        }
    }
}
