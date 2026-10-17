using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Extensions;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to refresh CleanName values for all library items and CleanValue values for all item values.
/// </summary>
[JellyfinMigration("2026-06-10T12:00:00", nameof(RefreshCleanNamesAndValues))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class RefreshCleanNamesAndValues : IAsyncMigrationRoutine
{
    private readonly IStartupLogger<RefreshCleanNamesAndValues> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshCleanNamesAndValues"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbProvider">Instance of the <see cref="IDbContextFactory{JellyfinDbContext}"/> interface.</param>
    public RefreshCleanNamesAndValues(
        IStartupLogger<RefreshCleanNamesAndValues> logger,
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _logger = logger;
        _dbProvider = dbProvider;
    }

    /// <inheritdoc />
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        await RefreshCleanNamesAsync(cancellationToken).ConfigureAwait(false);
        await RefreshCleanValuesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task RefreshCleanNamesAsync(CancellationToken cancellationToken)
    {
        const int Limit = 10000;
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
                var newCleanName = string.IsNullOrWhiteSpace(item.Name) ? string.Empty : item.Name.GetCleanValue();
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

    private async Task RefreshCleanValuesAsync(CancellationToken cancellationToken)
    {
        const int Limit = 10000;
        int itemCount = 0;

        var sw = Stopwatch.StartNew();

        using var context = _dbProvider.CreateDbContext();
        var records = context.ItemValues.Count(b => !string.IsNullOrEmpty(b.Value));
        _logger.LogInformation("Refreshing CleanValue for {Count} item values", records);

        var processedInPartition = 0;

        await foreach (var item in context.ItemValues
                          .Where(b => !string.IsNullOrEmpty(b.Value))
                          .OrderBy(e => e.ItemValueId)
                          .WithPartitionProgress((partition) => _logger.LogInformation("Processed: {Offset}/{Total} - Updated: {UpdatedCount} - Time: {Elapsed}", partition * Limit, records, itemCount, sw.Elapsed))
                          .PartitionEagerAsync(Limit, cancellationToken)
                          .WithCancellation(cancellationToken)
                          .ConfigureAwait(false))
        {
            try
            {
                var newCleanValue = string.IsNullOrWhiteSpace(item.Value) ? string.Empty : item.Value.GetCleanValue();
                if (!string.Equals(newCleanValue, item.CleanValue, StringComparison.Ordinal))
                {
                    _logger.LogDebug(
                        "Updating CleanValue for item value {Id}: '{OldValue}' -> '{NewValue}'",
                        item.ItemValueId,
                        item.CleanValue,
                        newCleanValue);
                    item.CleanValue = newCleanValue;
                    itemCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update CleanValue for item value {Id} ({Value})", item.ItemValueId, item.Value);
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
            "Refreshed CleanValue for {UpdatedCount} out of {TotalCount} item values in {Time}",
            itemCount,
            records,
            sw.Elapsed);
    }
}
