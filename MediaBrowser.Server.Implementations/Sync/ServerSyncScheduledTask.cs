using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Server.Implementations.Sync
{
    class ServerSyncScheduledTask : IScheduledTask, IConfigurableScheduledTask, IHasKey
    {
        private readonly ISyncManager _syncManager;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IServerApplicationHost _appHost;
        private readonly IConfigurationManager _config;

        public ServerSyncScheduledTask(ISyncManager syncManager, ILogger logger, IFileSystem fileSystem, IServerApplicationHost appHost, IConfigurationManager config)
        {
            _syncManager = syncManager;
            _logger = logger;
            _fileSystem = fileSystem;
            _appHost = appHost;
            _config = config;
        }

        public string Name
        {
            get { return "Cloud & Folder Sync"; }
        }

        public string Description
        {
            get { return "Sync media to the cloud"; }
        }

        public string Category
        {
            get
            {
                return "Sync";
            }
        }

        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return new MultiProviderSync((SyncManager)_syncManager, _appHost, _logger, _fileSystem, _config)
                .Sync(ServerSyncProviders, progress, cancellationToken);
        }

        public IEnumerable<IServerSyncProvider> ServerSyncProviders
        {
            get { return ((SyncManager)_syncManager).ServerSyncProviders; }
        }
        
        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[]
                {
                    new IntervalTrigger { Interval = TimeSpan.FromHours(3) }
                };
        }

        public bool IsHidden
        {
            get { return !IsEnabled; }
        }

        public bool IsEnabled
        {
            get { return ServerSyncProviders.Any(); }
        }

        public string Key
        {
            get { return "ServerSync"; }
        }
    }
}
