using System;
using MediaBrowser.Controller.Library;
using System.Threading;
using MediaBrowser.Model.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.EntryPoints
{
    /// <summary>
    /// Class RefreshUsersMetadata
    /// </summary>
    public class RefreshUsersMetadata : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILogger _logger;
        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        private IFileSystem _fileSystem;

        public string Name => "Refresh Users";

        public string Key => "RefreshUsers";

        public string Description => "Refresh user infos";

        public string Category
        {
            get { return "Library"; }
        }

        public bool IsHidden => true;

        public bool IsEnabled => true;

        public bool IsLogged => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshUsersMetadata" /> class.
        /// </summary>
        public RefreshUsersMetadata(ILogger logger, IUserManager userManager, IFileSystem fileSystem)
        {
            _logger = logger;
            _userManager = userManager;
            _fileSystem = fileSystem;
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var users = _userManager.Users.ToList();

            foreach (var user in users)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await user.RefreshMetadata(new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem)), cancellationToken).ConfigureAwait(false);
            }
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new List<TaskTriggerInfo>
            {
                new TaskTriggerInfo
                {
                    IntervalTicks = TimeSpan.FromDays(1).Ticks,
                    Type = TaskTriggerInfo.TriggerInterval
                }
            };
        }
    }
}
