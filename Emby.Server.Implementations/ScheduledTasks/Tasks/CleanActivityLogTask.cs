using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks
{
    /// <summary>
    /// Deletes old activity log entries.
    /// </summary>
    public class CleanActivityLogTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILocalizationManager _localization;
        private readonly IActivityManager _activityManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CleanActivityLogTask"/> class.
        /// </summary>
        /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        /// <param name="activityManager">Instance of the <see cref="IActivityManager"/> interface.</param>
        public CleanActivityLogTask(
            ILocalizationManager localization,
            IActivityManager activityManager)
        {
            _localization = localization;
            _activityManager = activityManager;
        }

        /// <inheritdoc />
        public string Name => _localization.GetLocalizedString("TaskCleanActivityLog");

        /// <inheritdoc />
        public string Key => "CleanActivityLog";

        /// <inheritdoc />
        public string Description => _localization.GetLocalizedString("TaskCleanActivityLogDescription");

        /// <inheritdoc />
        public string Category => _localization.GetLocalizedString("TasksMaintenanceCategory");

        /// <inheritdoc />
        public bool IsHidden => false;

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public bool IsLogged => true;

        /// <inheritdoc />
        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            // TODO allow configure
            var startDate = DateTime.UtcNow.AddDays(-30);
            return _activityManager.CleanAsync(startDate);
        }

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return Enumerable.Empty<TaskTriggerInfo>();
        }
    }
}