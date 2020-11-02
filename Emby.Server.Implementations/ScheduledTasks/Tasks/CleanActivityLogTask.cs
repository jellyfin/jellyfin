using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
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
        private readonly IServerConfigurationManager _serverConfigurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CleanActivityLogTask"/> class.
        /// </summary>
        /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        /// <param name="activityManager">Instance of the <see cref="IActivityManager"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        public CleanActivityLogTask(
            ILocalizationManager localization,
            IActivityManager activityManager,
            IServerConfigurationManager serverConfigurationManager)
        {
            _localization = localization;
            _activityManager = activityManager;
            _serverConfigurationManager = serverConfigurationManager;
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
            var retentionDays = _serverConfigurationManager.Configuration.ActivityLogRetentionDays;
            if (!retentionDays.HasValue || retentionDays <= 0)
            {
                throw new Exception($"Activity Log Retention days must be at least 0. Currently: {retentionDays}");
            }

            var startDate = DateTime.UtcNow.AddDays(retentionDays.Value * -1);
            return _activityManager.CleanAsync(startDate);
        }

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return Enumerable.Empty<TaskTriggerInfo>();
        }
    }
}