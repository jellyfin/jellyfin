#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Repoints what references a duplicate BaseItem, then deletes the duplicates.
/// </summary>
internal static class DuplicateItemMerge
{
    // Batched so we never issue one massive delete transaction.
    private const int DeleteBatchSize = 500;

    /// <summary>
    /// Repoints everything that references <paramref name="dupId"/> at <paramref name="keeperId"/>.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="dupId">The item being merged away.</param>
    /// <param name="keeperId">The item it is merged into.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public static async Task RedirectReferencesAsync(
        JellyfinDbContext context,
        Guid dupId,
        Guid keeperId,
        CancellationToken cancellationToken)
    {
        await context.BaseItems
            .Where(b => b.ParentId == dupId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.ParentId, keeperId), cancellationToken)
            .ConfigureAwait(false);

        await context.BaseItems
            .Where(b => b.OwnerId == dupId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.OwnerId, keeperId), cancellationToken)
            .ConfigureAwait(false);

        // AncestorIds PK is (ItemId, ParentItemId); drop rows that would collide before redirecting.
        await context.AncestorIds
            .Where(a => a.ParentItemId == dupId
                && context.AncestorIds.Any(k => k.ParentItemId == keeperId && k.ItemId == a.ItemId))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await context.AncestorIds
            .Where(a => a.ParentItemId == dupId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.ParentItemId, keeperId), cancellationToken)
            .ConfigureAwait(false);

        // LinkedChildren PK is (ParentId, ChildId); drop colliding rows in both directions.
        await context.LinkedChildren
            .Where(l => l.ParentId == dupId
                && context.LinkedChildren.Any(k => k.ParentId == keeperId && k.ChildId == l.ChildId))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await context.LinkedChildren
            .Where(l => l.ParentId == dupId)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.ParentId, keeperId), cancellationToken)
            .ConfigureAwait(false);
        await context.LinkedChildren
            .Where(l => l.ChildId == dupId
                && context.LinkedChildren.Any(k => k.ChildId == keeperId && k.ParentId == l.ParentId))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await context.LinkedChildren
            .Where(l => l.ChildId == dupId)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.ChildId, keeperId), cancellationToken)
            .ConfigureAwait(false);

        // UserData has UNIQUE(UserId, CustomDataKey); the keeper's value wins where both have one.
        await context.UserData
            .Where(u => u.ItemId == dupId
                && context.UserData.Any(k => k.ItemId == keeperId && k.UserId == u.UserId && k.CustomDataKey == u.CustomDataKey))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await context.UserData
            .Where(u => u.ItemId == dupId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.ItemId, keeperId), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes the items that were merged away, in batches.
    /// </summary>
    /// <param name="ids">The ids to delete.</param>
    /// <param name="itemKind">How to describe the items in the log, e.g. "MusicArtist records".</param>
    /// <param name="logger">The logger.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="persistenceService">The item persistence service.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public static async Task DeleteMergedItemsAsync(
        IReadOnlyList<Guid> ids,
        string itemKind,
        ILogger logger,
        ILibraryManager libraryManager,
        IItemPersistenceService persistenceService,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return;
        }

        logger.LogInformation("Deleting {Count} duplicate {Kind}...", ids.Count, itemKind);

        var deletedSoFar = 0;
        for (var offset = 0; offset < ids.Count; offset += DeleteBatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batchIds = ids.Skip(offset).Take(DeleteBatchSize).ToList();

            // Via LibraryManager so DeleteItemsUnsafeFast also removes the by-name metadata folders,
            // falling back to the persistence service for what it cannot resolve.
            var itemsToDelete = batchIds
                .Select(id => libraryManager.GetItemById(id))
                .Where(item => item is not null)
                .ToList();
            if (itemsToDelete.Count > 0)
            {
                libraryManager.DeleteItemsUnsafeFast(itemsToDelete!);
            }

            var deletedIds = itemsToDelete.Select(i => i!.Id).ToHashSet();
            var unresolvedIds = batchIds.Where(id => !deletedIds.Contains(id)).ToList();
            if (unresolvedIds.Count > 0)
            {
                persistenceService.DeleteItem(unresolvedIds);
            }

            deletedSoFar += batchIds.Count;
            logger.LogInformation("Deleting duplicate {Kind}: {Deleted}/{Total}", itemKind, deletedSoFar, ids.Count);
        }
    }

    /// <summary>
    /// Counts the rows each candidate id is referenced by, in chunks small enough for the parameter limit.
    /// </summary>
    /// <param name="candidateIds">The ids to count references for.</param>
    /// <param name="referencesOf">Projects a chunk of ids onto the referencing rows' ids.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The reference count per id, absent when nothing references it.</returns>
    public static async Task<Dictionary<Guid, int>> CountReferencesAsync(
        IReadOnlyList<Guid> candidateIds,
        Func<Guid[], IQueryable<Guid>> referencesOf,
        CancellationToken cancellationToken)
    {
        // Well under SQLite's variable limit.
        const int ParameterChunkSize = 500;

        var counts = new Dictionary<Guid, int>();
        foreach (var chunk in candidateIds.Chunk(ParameterChunkSize))
        {
            var rows = await referencesOf(chunk)
                .GroupBy(id => id)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var row in rows)
            {
                counts[row.Id] = counts.GetValueOrDefault(row.Id) + row.Count;
            }
        }

        return counts;
    }
}
