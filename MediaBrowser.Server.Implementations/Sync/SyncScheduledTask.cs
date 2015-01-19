using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class SyncScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ISyncRepository _syncRepo;
        private readonly ISyncManager _syncManager;
        private readonly ILogger _logger;
        private readonly IUserManager _userManager;
        private readonly ITVSeriesManager _tvSeriesManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly ISubtitleEncoder _subtitleEncoder;
        private readonly IConfigurationManager _config;
        private readonly IFileSystem _fileSystem;

        public SyncScheduledTask(ILibraryManager libraryManager, ISyncRepository syncRepo, ISyncManager syncManager, ILogger logger, IUserManager userManager, ITVSeriesManager tvSeriesManager, IMediaEncoder mediaEncoder, ISubtitleEncoder subtitleEncoder, IConfigurationManager config, IFileSystem fileSystem)
        {
            _libraryManager = libraryManager;
            _syncRepo = syncRepo;
            _syncManager = syncManager;
            _logger = logger;
            _userManager = userManager;
            _tvSeriesManager = tvSeriesManager;
            _mediaEncoder = mediaEncoder;
            _subtitleEncoder = subtitleEncoder;
            _config = config;
            _fileSystem = fileSystem;
        }

        public string Name
        {
            get { return "Sync preparation"; }
        }

        public string Description
        {
            get { return "Runs scheduled sync jobs"; }
        }

        public string Category
        {
            get
            {
                return "Library";
            }
        }

        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return new SyncJobProcessor(_libraryManager, _syncRepo, _syncManager, _logger, _userManager, _tvSeriesManager, _mediaEncoder, _subtitleEncoder, _config, _fileSystem)
                .Sync(progress, cancellationToken);
        }

        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[]
                {
                    new IntervalTrigger { Interval = TimeSpan.FromHours(3) },
                    new StartupTrigger{ DelayMs = Convert.ToInt32(TimeSpan.FromMinutes(5).TotalMilliseconds)}
                };
        }

        public bool IsHidden
        {
            get { return false; }
        }

        public bool IsEnabled
        {
            get { return true; }
        }
    }
}
