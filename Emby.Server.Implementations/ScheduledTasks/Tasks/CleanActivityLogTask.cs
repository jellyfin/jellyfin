using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Options;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks;

/// <summary>
/// Deletes old activity log entries.
/// </summary>
public class CleanActivityLogTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly ILocalizationManager _localization;
    private readonly IActivityManager _activityManager;
    private readonly IOptions<ServerConfiguration> _serverConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanActivityLogTask"/> class.
    /// </summary>
    /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="activityManager">Instance of the <see cref="IActivityManager"/> interface.</param>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IOptions{ServerConfiguration}"/> interface.</param>
    public CleanActivityLogTask(
        ILocalizationManager localization,
        IActivityManager activityManager,
        IOptions<ServerConfiguration> serverConfigurationManager)
    {
        _localization = localization;
        _activityManager = activityManager;
        _serverConfig = serverConfigurationManager;
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
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var retentionDays = _serverConfig.Value.ActivityLogRetentionDays;
        if (!retentionDays.HasValue || retentionDays < 0)
        {
            throw new InvalidOperationException($"Activity Log Retention days must be at least 0. Currently: {retentionDays}");
        }

        var startDate = DateTime.UtcNow.AddDays(-retentionDays.Value);
        return _activityManager.CleanAsync(startDate);
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return [];
    }
}
