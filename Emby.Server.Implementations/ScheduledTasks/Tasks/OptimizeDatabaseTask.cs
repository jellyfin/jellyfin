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
        private readonly IDbContextFactory<JellyfinDbContext> _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptimizeDatabaseTask" /> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        /// <param name="provider">Instance of the <see cref="IDbContextFactory{JellyfinDbContext}"/> interface.</param>
        public OptimizeDatabaseTask(
            ILogger<OptimizeDatabaseTask> logger,
            ILocalizationManager localization,
            IDbContextFactory<JellyfinDbContext> provider)
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

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return
            [
                // Every so often
                new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(24).Ticks }
            ];
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Performing quick maintanance on jellyfin.db.");

            try
            {
                var context = await _provider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                await using (context.ConfigureAwait(false))
                {
                    if (context.Database.IsSqlite())
                    {
                        await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(RESTART)", cancellationToken).ConfigureAwait(false);
                        await context.Database.ExecuteSqlRawAsync("PRAGMA quick_check(1)", cancellationToken).ConfigureAwait(false);
                        await context.Database.ExecuteSqlRawAsync("PRAGMA analysis_limit=1024", cancellationToken).ConfigureAwait(false);
                        await context.Database.ExecuteSqlRawAsync("PRAGMA optimize", cancellationToken).ConfigureAwait(false);
                        await context.Database.ExecuteSqlRawAsync("REINDEX", cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation("Quick jellyfin.db optimization complete!");
                    }
                    else
                    {
                        _logger.LogInformation("This database doesn't support optimization");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while doing quick jellyfin.db optimization");
            }
        }
    }
    public class OptimizeWeeklyDatabaseTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILogger<OptimizeWeeklyDatabaseTask> _logger;
        private readonly ILocalizationManager _localization;
        private readonly IDbContextFactory<JellyfinDbContext> _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptimizeWeeklyDatabaseTask" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localization">The localization manager.</param>
        /// <param name="provider">The jellyfin DB context provider.</param>
        public OptimizeWeeklyDatabaseTask(
            ILogger<OptimizeWeeklyDatabaseTask> logger,
            ILocalizationManager localization,
            IDbContextFactory<JellyfinDbContext> provider)
        {
            _logger = logger;
            _localization = localization;
            _provider = provider;
        }

        /// <inheritdoc />
        public string Name => _localization.GetLocalizedString("TaskExtendedOptimizeDatabase");

        /// <inheritdoc />
        public string Description => _localization.GetLocalizedString("TaskExtendedOptimizeDatabaseDescription");

        /// <inheritdoc />
        public string Category => _localization.GetLocalizedString("TasksMaintenanceCategory");

        /// <inheritdoc />
        public string Key => "OptimizeWeeklyDatabaseTask";

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
                new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(168).Ticks }
            };
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Performing extended maintanance on jellyfin.db.");

            try
            {
                var context = await _provider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                await using (context.ConfigureAwait(false))
                {
                    if (context.Database.IsSqlite())
                    {
                        await context.Database.ExecuteSqlRawAsync("VACUUM", cancellationToken).ConfigureAwait(false);
                        await context.Database.ExecuteSqlRawAsync("PRAGMA integrity_check(1)", cancellationToken).ConfigureAwait(false);
                        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_key_check", cancellationToken).ConfigureAwait(false);
                        await context.Database.ExecuteSqlRawAsync("PRAGMA analysis_limit=0", cancellationToken).ConfigureAwait(false);
                        await context.Database.ExecuteSqlRawAsync("PRAGMA optimize", cancellationToken).ConfigureAwait(false);
                        await context.Database.ExecuteSqlRawAsync("REINDEX", cancellationToken).ConfigureAwait(false);
                        _logger.LogInformation("Extended jellyfin.db optimization task complete!");
                    }
                    else
                    {
                        _logger.LogInformation("This database doesn't support optimization");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while performing extended jellyfin.db optimization");
            }
        }
    }
}
