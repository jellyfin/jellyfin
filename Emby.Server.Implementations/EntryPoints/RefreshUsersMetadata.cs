using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;

namespace Emby.Server.Implementations.EntryPoints
{
    /// <summary>
    /// Class RefreshUsersMetadata.
    /// </summary>
    public class RefreshUsersMetadata : IScheduledTask, IConfigurableScheduledTask
    {
        /// <summary>
        /// The user manager.
        /// </summary>
        private readonly IUserManager _userManager;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshUsersMetadata" /> class.
        /// </summary>
        public RefreshUsersMetadata(IUserManager userManager, IFileSystem fileSystem)
        {
            _userManager = userManager;
            _fileSystem = fileSystem;
        }

        /// <inheritdoc />
        public string Name => "Refresh Users";

        /// <inheritdoc />
        public string Key => "RefreshUsers";

        /// <inheritdoc />
        public string Description => "Refresh user infos";

        /// <inheritdoc />
        public string Category => "Library";

        /// <inheritdoc />
        public bool IsHidden => true;

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public bool IsLogged => true;

        /// <inheritdoc />
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            foreach (var user in _userManager.Users)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await user.RefreshMetadata(new MetadataRefreshOptions(new DirectoryService(_fileSystem)), cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
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
