using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Corrects SortOrder to match LocalImageProvider discovery priority.
/// The EF migration populated SortOrder based on DateModified, but this may not match
/// the actual discovery order (e.g., {mediaFileName}-fanart should be first).
/// This migration fixes the ordering to match LocalImageProvider.PopulateBackdrops priority.
/// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
[JellyfinMigration("2025-10-23T02:30:00", nameof(PopulateImageSortOrder), "A7B3F8E9-4C2D-4E1A-9B5F-6D8E3F2A1C9B")]
[JellyfinMigrationBackup(JellyfinDb = true)]
internal class PopulateImageSortOrder : IAsyncMigrationRoutine
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IStartupLogger<PopulateImageSortOrder> _logger;

    public PopulateImageSortOrder(
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IStartupLogger<PopulateImageSortOrder> logger)
    {
        _dbProvider = dbProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Correcting SortOrder to match LocalImageProvider discovery priority");

        using var context = _dbProvider.CreateDbContext();

        // Count total items for progress logging
        var totalItemCount = await context.BaseItemImageInfos
            .Select(i => i.ItemId)
            .Distinct()
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        if (totalItemCount == 0)
        {
            _logger.LogInformation("No items with images found, skipping SortOrder correction");
            return;
        }

        _logger.LogInformation("Processing {Count} items with images", totalItemCount);

        // Batch processing configuration
        const int batchSize = 500;
        int processedCount = 0;
        int errorCount = 0;
        var batchNumber = 0;

        var sw = Stopwatch.StartNew();

        // Process items in batches using PartitionHelper to avoid loading all data into memory
        await foreach (var itemId in context.BaseItemImageInfos
            .Select(i => i.ItemId)
            .Distinct()
            .OrderBy(id => id)
            .WithPartitionProgress(
                (partition) =>
                {
                    batchNumber = partition;
                    _logger.LogInformation(
                        "Processing batch {BatchNumber} - ({ProcessedSoFar}/{TotalRecords}) - Time: {Time}",
                        partition + 1,
                        Math.Min((partition + 1) * batchSize, totalItemCount),
                        totalItemCount,
                        sw.Elapsed);
                })
            .PartitionEagerAsync(batchSize, cancellationToken)
            .ConfigureAwait(false))
        {
            try
            {
                // Load the item's path for this specific item
                var itemPath = await context.BaseItems
                    .Where(b => b.Id.Equals(itemId))
                    .Select(b => b.Path)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                var mediaFileName = !string.IsNullOrEmpty(itemPath)
                    ? Path.GetFileNameWithoutExtension(itemPath)
                    : null;

                // Load images for this specific item
                var images = await context.BaseItemImageInfos
                    .Where(i => i.ItemId.Equals(itemId))
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                // Group images by type and assign SortOrder per type
                var imagesByType = images.GroupBy(img => img.ImageType);

                foreach (var typeGroup in imagesByType)
                {
                    // Calculate priority and sort order for each image within this type
                    var sortedImages = typeGroup
                        .Select(img => new
                        {
                            Image = img,
                            Priority = ImageOrderingUtilities.GetImageOrderPriority(img.Path, mediaFileName),
                            NumericIndex = ImageOrderingUtilities.GetNumericImageIndex(img.Path)
                        })
                        .OrderBy(x => x.Priority)
                        .ThenBy(x => x.NumericIndex)
                        .ThenBy(x => x.Image.Path, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    // Update SortOrder for each image within this type (0, 1, 2, ...)
                    for (int i = 0; i < sortedImages.Count; i++)
                    {
                        sortedImages[i].Image.SortOrder = i;
                    }
                }

                processedCount++;

                // Save changes every batch
                if (processedCount % batchSize == 0)
                {
                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    context.ChangeTracker.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing images for item {ItemId}", itemId);
                errorCount++;
                context.ChangeTracker.Clear();
            }
        }

        // Save any remaining changes
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            context.ChangeTracker.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving final batch");
        }

        // Reindex the 3-column index after population for optimal query performance
        // This rebuilds the index with the actual SortOrder values (not all 0s from migration)
        try
        {
            _logger.LogInformation("Reindexing 3-column index (ItemId, ImageType, SortOrder) after population");

            // Reindex to optimize the index structure with the new SortOrder values
            await context.Database.ExecuteSqlRawAsync(
                "REINDEX IX_BaseItemImageInfos_ItemId_ImageType_SortOrder",
                cancellationToken).ConfigureAwait(false);

            // Update query planner statistics
            await context.Database.ExecuteSqlRawAsync(
                "ANALYZE BaseItemImageInfos",
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Reindexing completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reindexing");
        }

        _logger.LogInformation(
            "SortOrder correction completed. Processed: {Processed}, Errors: {Errors}, Total: {Total}",
            processedCount,
            errorCount,
            totalItemCount);
    }
}
