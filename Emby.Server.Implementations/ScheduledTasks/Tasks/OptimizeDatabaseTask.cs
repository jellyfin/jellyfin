using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks
{
    /// <summary>
    /// Optimizes Jellyfin's database by issuing a VACUUM command.
    /// </summary>
    public class OptimizeDatabaseTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILogger<OptimizeDatabaseTask> _logger;
        private readonly ILocalizationManager _localization;
        private readonly JellyfinDbProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptimizeDatabaseTask" /> class.
        /// </summary>
        public OptimizeDatabaseTask(
            ILogger<OptimizeDatabaseTask> logger,
            ILocalizationManager localization,
            JellyfinDbProvider provider)
        {
            _logger = logger;
            _localization = localization;
            _provider = provider;
        }

        /// <inheritdoc />
        public string Name => _localization.GetLocalizedString("TaskOptimizeDatabase");

        /// <inheritdoc />
        public string Description => _localization.GetLocalizedString("TaskOptimizeDatabaseDescription");

        /// <inheritdoc />
        public string Category => _localization.GetLocalizedString("TasksMaintenanceCategory");

        /// <inheritdoc />
        public string Key => "OptimizeDatabaseTask";

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
                // Every so often
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
            _logger.LogInformation("Optimizing and vacuuming jellyfin.db...");

            try
            {
                using var context = _provider.CreateContext();
                if (context.Database.IsSqlite())
                {
                    context.Database.ExecuteSqlRaw("PRAGMA optimize");
                    context.Database.ExecuteSqlRaw("VACUUM");
                    _logger.LogInformation("jellyfin.db optimized successfully!");
                }
                else
                {
                    _logger.LogInformation("This database doesn't support optimization");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while optimizing jellyfin.db");
            }

            return Task.CompletedTask;
        }
    }
}
