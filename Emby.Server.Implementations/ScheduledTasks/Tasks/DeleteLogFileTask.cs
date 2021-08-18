using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks
{
    /// <summary>
    /// Deletes old log files.
    /// </summary>
    public class DeleteLogFileTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly IConfigurationManager _configurationManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILocalizationManager _localization;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteLogFileTask" /> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="localization">The localization manager.</param>
        public DeleteLogFileTask(IConfigurationManager configurationManager, IFileSystem fileSystem, ILocalizationManager localization)
        {
            _configurationManager = configurationManager;
            _fileSystem = fileSystem;
            _localization = localization;
        }

        /// <inheritdoc />
        public string Name => _localization.GetLocalizedString("TaskCleanLogs");

        /// <inheritdoc />
        public string Description => string.Format(
            CultureInfo.InvariantCulture,
            _localization.GetLocalizedString("TaskCleanLogsDescription"),
            _configurationManager.CommonConfiguration.LogFileRetentionDays);

        /// <inheritdoc />
        public string Category => _localization.GetLocalizedString("TasksMaintenanceCategory");

        /// <inheritdoc />
        public string Key => "CleanLogFiles";

        /// <inheritdoc />
        public bool IsHidden => false;

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public bool IsLogged => true;

        /// <summary>
        /// Creates the triggers that define when the task will run.
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(24).Ticks }
            };
        }

        /// <summary>
        /// Returns the task to be executed.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            // Delete log files more than n days old
            var minDateModified = DateTime.UtcNow.AddDays(-_configurationManager.CommonConfiguration.LogFileRetentionDays);

            // Only delete files that serilog doesn't manage (anything that doesn't start with 'log_'
            var filesToDelete = _fileSystem.GetFiles(_configurationManager.CommonApplicationPaths.LogDirectoryPath, true)
                .Where(f => !f.Name.StartsWith("log_", StringComparison.Ordinal)
                            && _fileSystem.GetLastWriteTimeUtc(f) < minDateModified)
                .ToList();

            var index = 0;

            foreach (var file in filesToDelete)
            {
                double percent = index / (double)filesToDelete.Count;

                progress.Report(100 * percent);

                cancellationToken.ThrowIfCancellationRequested();

                _fileSystem.DeleteFile(file.FullName);

                index++;
            }

            progress.Report(100);

            return Task.CompletedTask;
        }
    }
}
