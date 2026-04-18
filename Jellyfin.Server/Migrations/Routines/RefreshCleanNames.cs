using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to refresh CleanName values for all library items.
/// </summary>
[JellyfinMigration("2025-10-08T12:00:00", nameof(RefreshCleanNames))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class RefreshCleanNames : IAsyncMigrationRoutine
{
    private readonly IStartupLogger<RefreshCleanNames> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshCleanNames"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbProvider">Instance of the <see cref="IDbContextFactory{JellyfinDbContext}"/> interface.</param>
    public RefreshCleanNames(
        IStartupLogger<RefreshCleanNames> logger,
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _logger = logger;
        _dbProvider = dbProvider;
    }

    /// <inheritdoc />
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        const int Limit = 1000;
        int itemCount = 0;

        var sw = Stopwatch.StartNew();

        using var context = _dbProvider.CreateDbContext();
        var records = context.BaseItems.Count(b => !string.IsNullOrEmpty(b.Name));
        _logger.LogInformation("Refreshing CleanName for {Count} library items", records);

        var processedInPartition = 0;

        await foreach (var item in context.BaseItems
                          .Where(b => !string.IsNullOrEmpty(b.Name))
                          .OrderBy(e => e.Id)
                          .WithPartitionProgress((partition) => _logger.LogInformation("Processed: {Offset}/{Total} - Updated: {UpdatedCount} - Time: {Elapsed}", partition * Limit, records, itemCount, sw.Elapsed))
                          .PartitionEagerAsync(Limit, cancellationToken)
                          .WithCancellation(cancellationToken)
                          .ConfigureAwait(false))
        {
            try
            {
                var newCleanName = string.IsNullOrWhiteSpace(item.Name) ? string.Empty : BaseItemRepository.GetCleanValue(item.Name);
                if (!string.Equals(newCleanName, item.CleanName, StringComparison.Ordinal))
                {
                    _logger.LogDebug(
                        "Updating CleanName for item {Id}: '{OldValue}' -> '{NewValue}'",
                        item.Id,
                        item.CleanName,
                        newCleanName);
                    item.CleanName = newCleanName;
                    itemCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update CleanName for item {Id} ({Name})", item.Id, item.Name);
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
            "Refreshed CleanName for {UpdatedCount} out of {TotalCount} items in {Time}",
            itemCount,
            records,
            sw.Elapsed);
    }
}
