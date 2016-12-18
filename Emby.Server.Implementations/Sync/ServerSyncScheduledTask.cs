using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;

namespace Emby.Server.Implementations.Sync
{
    class ServerSyncScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ISyncManager _syncManager;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IServerApplicationHost _appHost;
        private readonly IConfigurationManager _config;
        private readonly ICryptoProvider _cryptographyProvider;

        public ServerSyncScheduledTask(ISyncManager syncManager, ILogger logger, IFileSystem fileSystem, IServerApplicationHost appHost, IConfigurationManager config, ICryptoProvider cryptographyProvider)
        {
            _syncManager = syncManager;
            _logger = logger;
            _fileSystem = fileSystem;
            _appHost = appHost;
            _config = config;
            _cryptographyProvider = cryptographyProvider;
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
            return new MultiProviderSync((SyncManager)_syncManager, _appHost, _logger, _fileSystem, _config, _cryptographyProvider)
                .Sync(ServerSyncProviders, progress, cancellationToken);
        }

        public IEnumerable<IServerSyncProvider> ServerSyncProviders
        {
            get { return ((SyncManager)_syncManager).ServerSyncProviders; }
        }
        
        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[] { 
            
                // Every so often
                new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(3).Ticks}
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

        public bool IsLogged
        {
            get { return true; }
        }

        public string Key
        {
            get { return "ServerSync"; }
        }
    }
}
