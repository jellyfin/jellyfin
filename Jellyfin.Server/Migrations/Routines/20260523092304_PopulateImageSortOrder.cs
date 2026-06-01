using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Populates SortOrder for all BaseItemImageInfo records using filename-based priority and numeric ordering.
/// </summary>
[JellyfinMigration("2026-05-23T09:23:04", nameof(PopulateImageSortOrder))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class PopulateImageSortOrder : IAsyncMigrationRoutine
{
    private const int PageSize = 5000;

    private readonly IStartupLogger<PopulateImageSortOrder> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PopulateImageSortOrder"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbProvider">The database context factory.</param>
    public PopulateImageSortOrder(
        IStartupLogger<PopulateImageSortOrder> logger,
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _logger = logger;
        _dbProvider = dbProvider;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var context = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var itemIds = await context.BaseItemImageInfos
                .Select(i => i.ItemId)
                .Distinct()
                .OrderBy(id => id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (itemIds.Count == 0)
            {
                _logger.LogInformation("No image records found. Skipping SortOrder population.");
                return;
            }

            _logger.LogInformation("Populating SortOrder for images of {Count} items", itemIds.Count);

            int totalUpdated = 0;

            for (int batch = 0; batch < itemIds.Count; batch += PageSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batchIds = itemIds.Skip(batch).Take(PageSize).ToHashSet();

                var itemPaths = await context.BaseItems
                    .Where(b => batchIds.Contains(b.Id))
                    .Select(b => new { b.Id, b.Path })
                    .ToDictionaryAsync(b => b.Id, b => b.Path, cancellationToken)
                    .ConfigureAwait(false);

                var images = await context.BaseItemImageInfos
                    .Where(i => batchIds.Contains(i.ItemId))
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                int updated = AssignSortOrderToGroups(images, itemPaths);

                if (updated > 0)
                {
                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                context.ChangeTracker.Clear();
                totalUpdated += updated;

                _logger.LogInformation(
                    "Populated SortOrder for items {Start}-{End} of {Total} ({Updated} images updated)",
                    batch + 1,
                    Math.Min(batch + PageSize, itemIds.Count),
                    itemIds.Count,
                    updated);
            }

            _logger.LogInformation(
                "Finished populating SortOrder. {TotalUpdated} image records updated across {TotalItems} items.",
                totalUpdated,
                itemIds.Count);
        }
    }

    private static int AssignSortOrderToGroups(
        List<BaseItemImageInfo> images,
        IReadOnlyDictionary<Guid, string?> itemPaths)
    {
        int updated = 0;
        foreach (var group in images.GroupBy(i => new { i.ItemId, i.ImageType }))
        {
            string? mediaFileName = null;
            if (itemPaths.TryGetValue(group.Key.ItemId, out var p) && !string.IsNullOrEmpty(p))
            {
                mediaFileName = Path.GetFileNameWithoutExtension(p);
            }

            var sorted = group
                .Select(img => new { Image = img, Priority = ImageOrderingUtilities.GetImageOrderPriority(img.Path, mediaFileName) })
                .OrderBy(x => x.Priority)
                .ThenBy(x => ImageOrderingUtilities.GetNumericImageIndex(x.Image.Path))
                .ThenBy(x => x.Image.Path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                if (sorted[i].Image.SortOrder != i)
                {
                    sorted[i].Image.SortOrder = i;
                    updated++;
                }
            }
        }

        return updated;
    }
}
