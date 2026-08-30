using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to populate the pre-transliteration sort-name initial for existing library items.
/// </summary>
[JellyfinMigration("2026-08-29T19:00:00", nameof(RefreshSortNameInitials))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class RefreshSortNameInitials : IAsyncMigrationRoutine
{
    private readonly IStartupLogger<RefreshSortNameInitials> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IServerConfigurationManager _configurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshSortNameInitials"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbProvider">The database context factory.</param>
    /// <param name="configurationManager">The server configuration manager.</param>
    public RefreshSortNameInitials(
        IStartupLogger<RefreshSortNameInitials> logger,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IServerConfigurationManager configurationManager)
    {
        _logger = logger;
        _dbProvider = dbProvider;
        _configurationManager = configurationManager;
    }

    /// <inheritdoc />
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        const int BatchSize = 10000;
        var configuration = _configurationManager.Configuration;
        var personType = typeof(Person).ToString();
        var stopwatch = Stopwatch.StartNew();
        var updatedCount = 0;
        var totalCount = 0;

        var context = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            totalCount = await context.BaseItems.CountAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Refreshing SortNameInitial for {Count} library items", totalCount);

            for (var offset = 0; offset < totalCount; offset += BatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var items = await context.BaseItems
                    .OrderBy(item => item.Id)
                    .Skip(offset)
                    .Take(BatchSize)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (items.Count == 0)
                {
                    break;
                }

                foreach (var item in items)
                {
                    var sourceName = string.IsNullOrEmpty(item.ForcedSortName) ? item.Name : item.ForcedSortName;
                    var enableAlphaNumericSorting = !string.Equals(item.Type, personType, StringComparison.Ordinal);
                    var initial = BaseItem.GetSortNameInitial(sourceName, enableAlphaNumericSorting, configuration);
                    if (string.Equals(initial, item.SortNameInitial, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    item.SortNameInitial = initial;
                    updatedCount++;
                }

                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                context.ChangeTracker.Clear();

                _logger.LogInformation(
                    "Processed {Processed}/{Total} library items - Updated: {UpdatedCount} - Time: {Elapsed}",
                    Math.Min(offset + items.Count, totalCount),
                    totalCount,
                    updatedCount,
                    stopwatch.Elapsed);
            }
        }

        _logger.LogInformation(
            "Refreshed SortNameInitial for {UpdatedCount} out of {TotalCount} items in {Time}",
            updatedCount,
            totalCount,
            stopwatch.Elapsed);
    }
}
