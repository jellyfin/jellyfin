using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to remove duplicate Folder items when both a generic Folder and a
/// properly typed item (Series, Season, etc.) exist at the same path. The Folder
/// is removed after re-parenting its children to the typed item. Items without
/// a DateCreated will also get one, based on the Folder/File on disk, or default
/// to current time UTC.
/// </summary>
[JellyfinMigration("2026-02-22T12:00:00", nameof(RemoveDuplicateFolderItems))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class RemoveDuplicateFolderItems : IAsyncMigrationRoutine
{
    private const string FolderTypeName = "MediaBrowser.Controller.Entities.Folder";

    private readonly IStartupLogger<RemoveDuplicateFolderItems> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveDuplicateFolderItems"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbProvider">Instance of the <see cref="IDbContextFactory{JellyfinDbContext}"/> interface.</param>
    public RemoveDuplicateFolderItems(
        IStartupLogger<RemoveDuplicateFolderItems> logger,
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _logger = logger;
        _dbProvider = dbProvider;
    }

    /// <inheritdoc />
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var context = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var sw = Stopwatch.StartNew();

            await DeduplicateFolderItemsAsync(context, sw, cancellationToken).ConfigureAwait(false);
            sw.Reset();
            await FixNullDateCreatedAsync(context, sw, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DeduplicateFolderItemsAsync(JellyfinDbContext context, Stopwatch sw, CancellationToken cancellationToken)
    {
        // Find all paths where a Folder and a non-Folder typed item coexist.
        // We only consider folder-type items (IsFolder == true) to avoid matching file-level items.
        var duplicatePaths = await context.BaseItems
            .Where(b => b.Path != null && b.IsFolder)
            .GroupBy(b => b.Path)
            .Where(g => g.Any(b => b.Type == FolderTypeName) && g.Any(b => b.Type != FolderTypeName))
            .Select(g => g.Key)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (duplicatePaths.Count == 0)
        {
            _logger.LogInformation("No duplicate Folder items found, skipping deduplication.");
            return;
        }

        _logger.LogInformation("Found {Count} paths with duplicate Folder items to clean up.", duplicatePaths.Count);

        sw.Start();
        var processedCount = 0;
        foreach (var path in duplicatePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get the duplicate items at this path.
            var items = await context.BaseItems.AsNoTracking()
                .Where(b => b.Path == path && b.IsFolder)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var folderItem = items.FirstOrDefault(b => b.Type == FolderTypeName);
            var typedItem = items.FirstOrDefault(b => b.Type != FolderTypeName);

            if (folderItem is null || typedItem is null)
            {
                continue;
            }

            _logger.LogDebug(
                "Processing duplicate at path '{Path}': Folder {FolderId} -> {TypedType} {TypedId}",
                path,
                folderItem.Id,
                typedItem.Type,
                typedItem.Id);

            // Copy DateCreated/DateModified from the Folder to the typed item if missing.
            var updateDateCreated = typedItem.DateCreated is null && folderItem.DateCreated is not null;
            var updateDateModified = typedItem.DateModified is null && folderItem.DateModified is not null;
            if (updateDateCreated || updateDateModified)
            {
                var newDateCreated = updateDateCreated ? folderItem.DateCreated : typedItem.DateCreated;
                var newDateModified = updateDateModified ? folderItem.DateModified : typedItem.DateModified;

                await context.BaseItems
                    .Where(b => b.Id.Equals(typedItem.Id))
                    .ExecuteUpdateAsync(
                        s => s
                            .SetProperty(b => b.DateCreated, newDateCreated)
                            .SetProperty(b => b.DateModified, newDateModified),
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            // Re-parent children: move any items whose ParentId points to the Folder.
            await context.BaseItems
                .Where(b => b.ParentId.Equals(folderItem.Id))
                .ExecuteUpdateAsync(
                    s => s.SetProperty(b => b.ParentId, typedItem.Id),
                    cancellationToken)
                .ConfigureAwait(false);

            // Update AncestorIds to point to the typed item instead of the Folder.
            await context.AncestorIds
                .Where(a => a.ParentItemId.Equals(folderItem.Id))
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.ParentItemId, typedItem.Id),
                    cancellationToken)
                .ConfigureAwait(false);

            // Delete the stale Folder item (children are already re-parented, CASCADE is safe).
            await context.BaseItems
                .Where(b => b.Id.Equals(folderItem.Id))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            processedCount++;

            if (processedCount % 100 == 0)
            {
                _logger.LogInformation(
                    "Progress: {Processed}/{Total} duplicates resolved - Time: {Elapsed}",
                    processedCount,
                    duplicatePaths.Count,
                    sw.Elapsed);
            }
        }

        _logger.LogInformation("Removed {Count} duplicate Folder items in {Time}.", processedCount, sw.Elapsed);
    }

    private async Task FixNullDateCreatedAsync(JellyfinDbContext context, Stopwatch sw, CancellationToken cancellationToken)
    {
        // Fix remaining NULL DateCreated as a safety net.
        // Prefer the filesystem creation time when the path exists on disk.
        var nullDateItems = await context.BaseItems
            .Where(b => b.DateCreated == null)
            .Select(b => new { b.Id, b.Path })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (nullDateItems.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Fixing NULL DateCreated for {Count} items.", nullDateItems.Count);

        sw.Start();
        foreach (var item in nullDateItems)
        {
            DateTime dateCreated;
            if (!string.IsNullOrEmpty(item.Path) && (File.Exists(item.Path) || Directory.Exists(item.Path)))
            {
                dateCreated = File.GetCreationTimeUtc(item.Path);
            }
            else
            {
                dateCreated = DateTime.UtcNow;
            }

            await context.BaseItems
                .Where(b => b.Id.Equals(item.Id))
                .ExecuteUpdateAsync(
                    s => s.SetProperty(b => b.DateCreated, dateCreated),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        _logger.LogInformation("Fixed DateCreated for {Count} items in {Time}.", nullDateItems.Count, sw.Elapsed);
    }
}
