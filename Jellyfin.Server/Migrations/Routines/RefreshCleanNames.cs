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
        const int batchSize = 1000;
        int itemCount = 0, offset = 0;

        var sw = Stopwatch.StartNew();

        var context = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var totalRecords = await context.BaseItems.CountAsync(b => !string.IsNullOrEmpty(b.Name), cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Refreshing CleanName for {Count} library items", totalRecords);

            do
            {
                var baseQuery = context.BaseItems
                    .Where(b => !string.IsNullOrEmpty(b.Name))
                    .OrderBy(e => e.Id);

                IQueryable<BaseItemEntity> query = baseQuery;

                if (offset > 0)
                {
                    query = query.Skip(offset);
                }

                var batch = await query.Take(batchSize).ToListAsync(cancellationToken).ConfigureAwait(false);

                if (batch.Count == 0)
                {
                    break;
                }

                foreach (var item in batch)
                {
                    try
                    {
                        var newCleanName = BaseItemRepository.GetCleanValue(item.Name ?? string.Empty);
                        if (newCleanName != item.CleanName)
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
                }

                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                offset += batch.Count;

                _logger.LogInformation(
                    "Processed: {Offset}/{Total} - Updated: {UpdatedCount} - Time: {Elapsed}",
                    offset,
                    totalRecords,
                    itemCount,
                    sw.Elapsed);
            } while (offset < totalRecords);

            _logger.LogInformation(
                "Refreshed CleanName for {UpdatedCount} out of {TotalCount} items in {Time}",
                itemCount,
                totalRecords,
                sw.Elapsed);
        }
    }
}
