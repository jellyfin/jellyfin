using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks;

/// <summary>
/// Optimizes Jellyfin's database by issuing a VACUUM command.
/// </summary>
public class OptimizeDatabaseTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly ILogger<OptimizeDatabaseTask> _logger;
    private readonly ILocalizationManager _localization;
    private readonly IJellyfinDatabaseProvider _jellyfinDatabaseProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptimizeDatabaseTask" /> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="jellyfinDatabaseProvider">Instance of the JellyfinDatabaseProvider that can be used for provider specific operations.</param>
    public OptimizeDatabaseTask(
        ILogger<OptimizeDatabaseTask> logger,
        ILocalizationManager localization,
        IJellyfinDatabaseProvider jellyfinDatabaseProvider)
    {
        _logger = logger;
        _localization = localization;
        _jellyfinDatabaseProvider = jellyfinDatabaseProvider;
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
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.IntervalTrigger,
            IntervalTicks = TimeSpan.FromHours(6).Ticks
        };
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Optimizing and vacuuming jellyfin.db...");

        try
        {
            await _jellyfinDatabaseProvider.RunScheduledOptimisation(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while optimizing jellyfin.db");
        }
    }
}
