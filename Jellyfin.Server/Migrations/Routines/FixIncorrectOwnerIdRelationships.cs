using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Fixes incorrect OwnerId relationships where video/movie items are children of other video/movie items.
/// These are alternate versions (4K vs 1080p) that were incorrectly linked as parent-child relationships
/// by the auto-merge logic. Only legitimate extras (trailers, behind-the-scenes) should have OwnerId set.
/// Also removes duplicate database entries for the same file path.
/// </summary>
[JellyfinMigration("2026-01-15T12:00:00", nameof(FixIncorrectOwnerIdRelationships))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class FixIncorrectOwnerIdRelationships : IAsyncMigrationRoutine
{
    private readonly IStartupLogger<FixIncorrectOwnerIdRelationships> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;
    private readonly ILibraryManager _libraryManager;
    private readonly IItemPersistenceService _persistenceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixIncorrectOwnerIdRelationships"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="persistenceService">The item persistence service.</param>
    public FixIncorrectOwnerIdRelationships(
        IStartupLogger<FixIncorrectOwnerIdRelationships> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory,
        ILibraryManager libraryManager,
        IItemPersistenceService persistenceService)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _libraryManager = libraryManager;
        _persistenceService = persistenceService;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            // Step 1: Find and remove duplicate database entries (same Path, different IDs)
            await RemoveDuplicateItemsAsync(context, cancellationToken).ConfigureAwait(false);

            // Step 2: Clear incorrect OwnerId for video/movie items that are children of other video/movie items
            await ClearIncorrectOwnerIdsAsync(context, cancellationToken).ConfigureAwait(false);

            // Step 3: Reassign orphaned extras to correct parents
            await ReassignOrphanedExtrasAsync(context, cancellationToken).ConfigureAwait(false);

            // Step 4: Populate PrimaryVersionId for alternate version children
            await PopulatePrimaryVersionIdAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RemoveDuplicateItemsAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        // Find all paths that have duplicate entries
        var duplicatePaths = await context.BaseItems
            .Where(b => b.Path != null)
            .GroupBy(b => b.Path)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (duplicatePaths.Count == 0)
        {
            _logger.LogInformation("No duplicate items found, skipping duplicate removal.");
            return;
        }

        _logger.LogInformation("Found {Count} paths with duplicate database entries", duplicatePaths.Count);

        // Collect all duplicate IDs to delete in one batch
        var allIdsToDelete = new List<Guid>();
        foreach (var path in duplicatePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get all items with this path
            var itemsWithPath = await context.BaseItems
                .Where(b => b.Path == path)
                .Select(b => new
                {
                    b.Id,
                    b.Type,
                    b.DateCreated,
                    HasOwnedExtras = context.BaseItems.Any(c => c.OwnerId.HasValue && c.OwnerId.Value.Equals(b.Id)),
                    HasDirectChildren = context.BaseItems.Any(c => c.ParentId.HasValue && c.ParentId.Value.Equals(b.Id))
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (itemsWithPath.Count <= 1)
            {
                continue;
            }

            // Keep the item that has direct children, then owned extras, then prefer non-Folder types, then newest
            var itemToKeep = itemsWithPath
                .OrderByDescending(i => i.HasDirectChildren)
                .ThenByDescending(i => i.HasOwnedExtras)
                .ThenByDescending(i => i.Type != "MediaBrowser.Controller.Entities.Folder")
                .ThenByDescending(i => i.DateCreated)
                .First();
            if (itemToKeep is null)
            {
                continue;
            }

            allIdsToDelete.AddRange(itemsWithPath.Where(i => !i.Id.Equals(itemToKeep.Id)).Select(i => i.Id));
        }

        if (allIdsToDelete.Count > 0)
        {
            // Batch-resolve items for metadata path cleanup, then delete all at once
            var itemsToDelete = allIdsToDelete
                .Select(id => _libraryManager.GetItemById(id))
                .Where(item => item is not null)
                .ToList();
            _libraryManager.DeleteItemsUnsafeFast(itemsToDelete!);

            // Fall back to direct DB deletion for any items that couldn't be resolved via LibraryManager
            var deletedIds = itemsToDelete.Select(i => i!.Id).ToHashSet();
            var unresolvedIds = allIdsToDelete.Where(id => !deletedIds.Contains(id)).ToList();
            if (unresolvedIds.Count > 0)
            {
                _persistenceService.DeleteItem(unresolvedIds);
            }
        }

        _logger.LogInformation("Successfully removed {Count} duplicate database entries", allIdsToDelete.Count);
    }

    private async Task ClearIncorrectOwnerIdsAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        // Find video/movie items with incorrect OwnerId (ExtraType is NULL or 0, pointing to another video/movie)
        var incorrectChildrenWithParent = await context.BaseItems
            .Where(b => b.OwnerId.HasValue
                && (b.ExtraType == null || b.ExtraType == 0)
                && (b.Type == "MediaBrowser.Controller.Entities.Video" || b.Type == "MediaBrowser.Controller.Entities.Movies.Movie"))
            .Where(b => context.BaseItems.Any(parent =>
                parent.Id.Equals(b.OwnerId!.Value)
                && (parent.Type == "MediaBrowser.Controller.Entities.Video" || parent.Type == "MediaBrowser.Controller.Entities.Movies.Movie")))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Also find orphaned items (parent doesn't exist)
        var orphanedChildren = await context.BaseItems
            .Where(b => b.OwnerId.HasValue
                && (b.ExtraType == null || b.ExtraType == 0)
                && (b.Type == "MediaBrowser.Controller.Entities.Video" || b.Type == "MediaBrowser.Controller.Entities.Movies.Movie"))
            .Where(b => !context.BaseItems.Any(parent => parent.Id.Equals(b.OwnerId!.Value)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var totalIncorrect = incorrectChildrenWithParent.Count + orphanedChildren.Count;
        if (totalIncorrect == 0)
        {
            _logger.LogInformation("No items with incorrect OwnerId found, skipping OwnerId cleanup.");
            return;
        }

        _logger.LogInformation(
            "Found {Count} video/movie items with incorrect OwnerId relationships ({WithParent} with parent, {Orphaned} orphaned)",
            totalIncorrect,
            incorrectChildrenWithParent.Count,
            orphanedChildren.Count);

        // Clear OwnerId for all incorrect items
        var allIncorrectItems = incorrectChildrenWithParent.Concat(orphanedChildren).ToList();
        foreach (var item in allIncorrectItems)
        {
            item.OwnerId = null;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Successfully cleared OwnerId for {Count} items", totalIncorrect);
    }

    private async Task ReassignOrphanedExtrasAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        // Find extras whose parent was deleted during duplicate removal
        var orphanedExtras = await context.BaseItems
            .Where(b => b.ExtraType != null && b.ExtraType != 0 && b.OwnerId.HasValue)
            .Where(b => !context.BaseItems.Any(parent => parent.Id.Equals(b.OwnerId!.Value)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (orphanedExtras.Count == 0)
        {
            _logger.LogInformation("No orphaned extras found, skipping reassignment.");
            return;
        }

        _logger.LogInformation("Found {Count} orphaned extras to reassign", orphanedExtras.Count);

        // Build a lookup of directory -> first video/movie item for parent resolution
        var extraDirectories = orphanedExtras
            .Where(e => !string.IsNullOrEmpty(e.Path))
            .Select(e => System.IO.Path.GetDirectoryName(e.Path))
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct()
            .ToList();

        // Load all potential parent video/movies with paths in one query
        var videoTypes = new[]
        {
            "MediaBrowser.Controller.Entities.Video",
            "MediaBrowser.Controller.Entities.Movies.Movie"
        };
        var potentialParents = await context.BaseItems
            .Where(b => b.Path != null && videoTypes.Contains(b.Type))
            .Select(b => new { b.Id, b.Path })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Build directory -> parent ID mapping
        var dirToParent = new Dictionary<string, Guid>();
        foreach (var dir in extraDirectories)
        {
            var parent = potentialParents
                .Where(p => p.Path!.StartsWith(dir!, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Id)
                .FirstOrDefault();
            if (parent is not null)
            {
                dirToParent[dir!] = parent.Id;
            }
        }

        var reassignedCount = 0;
        foreach (var extra in orphanedExtras)
        {
            if (string.IsNullOrEmpty(extra.Path))
            {
                continue;
            }

            var extraDirectory = System.IO.Path.GetDirectoryName(extra.Path);
            if (!string.IsNullOrEmpty(extraDirectory) && dirToParent.TryGetValue(extraDirectory, out var parentId))
            {
                extra.OwnerId = parentId;
                reassignedCount++;
            }
            else
            {
                extra.OwnerId = null;
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Successfully reassigned {Count} orphaned extras", reassignedCount);
    }

    private async Task PopulatePrimaryVersionIdAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        // Find all alternate version relationships where child's PrimaryVersionId is not set
        // ChildType 2 = LocalAlternateVersion, ChildType 3 = LinkedAlternateVersion
        var alternateVersionLinks = await context.LinkedChildren
            .Where(lc => (lc.ChildType == Jellyfin.Database.Implementations.Entities.LinkedChildType.LocalAlternateVersion
                       || lc.ChildType == Jellyfin.Database.Implementations.Entities.LinkedChildType.LinkedAlternateVersion))
            .Join(
                context.BaseItems,
                lc => lc.ChildId,
                item => item.Id,
                (lc, item) => new { lc.ParentId, lc.ChildId, item.PrimaryVersionId })
            .Where(x => !x.PrimaryVersionId.HasValue || !x.PrimaryVersionId.Value.Equals(x.ParentId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (alternateVersionLinks.Count == 0)
        {
            _logger.LogInformation("No alternate version items need PrimaryVersionId population, skipping.");
            return;
        }

        _logger.LogInformation("Found {Count} alternate version items that need PrimaryVersionId populated", alternateVersionLinks.Count);

        // Batch-load all child items in a single query
        var childIds = alternateVersionLinks.Select(l => l.ChildId).Distinct().ToList();
        var childItems = await context.BaseItems
            .Where(b => childIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, cancellationToken)
            .ConfigureAwait(false);

        var updatedCount = 0;
        foreach (var link in alternateVersionLinks)
        {
            if (childItems.TryGetValue(link.ChildId, out var childItem))
            {
                childItem.PrimaryVersionId = link.ParentId;
                updatedCount++;
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Successfully populated PrimaryVersionId for {Count} alternate version items", updatedCount);
    }
}
