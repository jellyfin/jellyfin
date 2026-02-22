#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks;

/// <summary>
/// Task to clean up any detached userdata from the database.
/// </summary>
public class CleanupUserDataTask : IScheduledTask
{
    private readonly ILocalizationManager _localization;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly ILogger<CleanupUserDataTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanupUserDataTask"/> class.
    /// </summary>
    /// <param name="localization">The localisation Provider.</param>
    /// <param name="dbProvider">The DB context factory.</param>
    /// <param name="logger">A logger.</param>
    public CleanupUserDataTask(ILocalizationManager localization, IDbContextFactory<JellyfinDbContext> dbProvider, ILogger<CleanupUserDataTask> logger)
    {
        _localization = localization;
        _dbProvider = dbProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => _localization.GetLocalizedString("CleanupUserDataTask");

    /// <inheritdoc />
    public string Description => _localization.GetLocalizedString("CleanupUserDataTaskDescription");

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksMaintenanceCategory");

    /// <inheritdoc />
    public string Key => nameof(CleanupUserDataTask);

    /// <inheritdoc/>
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        const int LimitDays = 90;
        var userDataDate = DateTime.UtcNow.AddDays(LimitDays * -1);
        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var detachedUserData = dbContext.UserData.Where(e => e.ItemId == BaseItemRepository.PlaceholderId);
            _logger.LogInformation("There are {NoDetached} detached UserData entries.", detachedUserData.Count());

            detachedUserData = detachedUserData.Where(e => e.RetentionDate < userDataDate);

            _logger.LogInformation("{NoDetached} are older then {Limit} days.", detachedUserData.Count(), LimitDays);

            await detachedUserData.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        }

        progress.Report(100);
    }

    /// <inheritdoc/>
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield break;
    }
}
