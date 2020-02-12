using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;

namespace Emby.Server.Implementations.ScheduledTasks
{
    /// <summary>
    /// Class RefreshMediaLibraryTask.
    /// </summary>
    public class RefreshMediaLibraryTask : IScheduledTask
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshMediaLibraryTask" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        public RefreshMediaLibraryTask(ILibraryManager libraryManager, IServerConfigurationManager config)
        {
            _libraryManager = libraryManager;
            _config = config;
        }

        /// <summary>
        /// Creates the triggers that define when the task will run.
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(12).Ticks
            };
        }

        /// <summary>
        /// Executes the internal.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(0);

            return ((LibraryManager)_libraryManager).ValidateMediaLibraryInternal(progress, cancellationToken);
        }

        public string Name => "Scan Media Library";

        public string Description => "Scans your media library for new files and refreshes metadata.";

        public string Category => "Library";

        public string Key => "RefreshLibrary";

        public bool IsHidden => false;

        public bool IsEnabled => true;

        public bool IsLogged => true;
    }
}
