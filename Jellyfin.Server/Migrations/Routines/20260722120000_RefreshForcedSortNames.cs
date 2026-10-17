using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Extensions;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to recompute the SortName of all items that have a forced sort name.
/// </summary>
[JellyfinMigration("2026-07-22T12:00:00", nameof(RefreshForcedSortNames))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class RefreshForcedSortNames : IAsyncMigrationRoutine
{
    private readonly IStartupLogger<RefreshForcedSortNames> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IServerConfigurationManager _configurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshForcedSortNames"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbProvider">Instance of the <see cref="IDbContextFactory{JellyfinDbContext}"/> interface.</param>
    /// <param name="configurationManager">The server configuration manager providing the sort rules.</param>
    public RefreshForcedSortNames(
        IStartupLogger<RefreshForcedSortNames> logger,
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
        const int Limit = 10000;
        int itemCount = 0;

        var configuration = _configurationManager.Configuration;
        // Only the Person type disables alphanumeric sorting; everything else uses the cleaning rules.
        var personType = typeof(Person).ToString();

        var sw = Stopwatch.StartNew();

        using var context = _dbProvider.CreateDbContext();
        var records = context.BaseItems.Count(b => !string.IsNullOrEmpty(b.ForcedSortName));
        _logger.LogInformation("Refreshing SortName for {Count} library items with a forced sort name", records);

        var processedInPartition = 0;

        await foreach (var item in context.BaseItems
                          .Where(b => !string.IsNullOrEmpty(b.ForcedSortName))
                          .OrderBy(e => e.Id)
                          .WithPartitionProgress((partition) => _logger.LogInformation("Processed: {Offset}/{Total} - Updated: {UpdatedCount} - Time: {Elapsed}", partition * Limit, records, itemCount, sw.Elapsed))
                          .PartitionEagerAsync(Limit, cancellationToken)
                          .WithCancellation(cancellationToken)
                          .ConfigureAwait(false))
        {
            try
            {
                var enableAlphaNumericSorting = !string.Equals(item.Type, personType, StringComparison.Ordinal);
                var newSortName = BaseItem.GetSortName(item.ForcedSortName!, enableAlphaNumericSorting, configuration);
                if (!string.Equals(newSortName, item.SortName, StringComparison.Ordinal))
                {
                    _logger.LogDebug(
                        "Updating SortName for item {Id}: '{OldValue}' -> '{NewValue}'",
                        item.Id,
                        item.SortName,
                        newSortName);
                    item.SortName = newSortName;
                    itemCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update SortName for item {Id} ({Name})", item.Id, item.Name);
            }

            processedInPartition++;

            if (processedInPartition >= Limit)
            {
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                // Clear tracked entities to avoid memory growth across partitions
                context.ChangeTracker.Clear();
                processedInPartition = 0;
            }
        }

        // Save any remaining changes after the loop
        if (processedInPartition > 0)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            context.ChangeTracker.Clear();
        }

        _logger.LogInformation(
            "Refreshed SortName for {UpdatedCount} out of {TotalCount} items in {Time}",
            itemCount,
            records,
            sw.Elapsed);
    }
}
