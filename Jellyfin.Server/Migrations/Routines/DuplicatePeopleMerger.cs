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
/// Merges people that are the same person under a given identity key.
/// </summary>
/// <remarks>
/// Shared by the migrations that widened that key: first casing, then the clean name. Both passes
/// exist because a person is linked to its credits by name alone, so two spellings split a
/// filmography across two person pages.
/// </remarks>
public sealed class DuplicatePeopleMerger
{
    private const string PersonType = "MediaBrowser.Controller.Entities.Person";

    // Well under SQLite's variable limit, so a candidate set of any size still costs a handful of queries.
    private const int ParameterChunkSize = 500;

    private readonly ILogger _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IItemPersistenceService _persistenceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicatePeopleMerger"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="persistenceService">The item persistence service.</param>
    public DuplicatePeopleMerger(ILogger logger, ILibraryManager libraryManager, IItemPersistenceService persistenceService)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _persistenceService = persistenceService;
    }

    /// <summary>
    /// Merges Person BaseItems whose names collapse to the same key.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="keySelector">Projects a name onto the identity key.</param>
    /// <param name="duplicateKind">How to describe the duplicates in the log.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public async Task MergePersonBaseItemsAsync(
        JellyfinDbContext context,
        Func<string, string> keySelector,
        string duplicateKind,
        CancellationToken cancellationToken)
    {
        var persons = await context.BaseItems
            .Where(b => b.Type == PersonType && b.Name != null)
            .Select(b => new { b.Id, b.Name, b.DateCreated })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var groups = persons
            .GroupBy(p => keySelector(p.Name!))
            .Where(g => g.Count() > 1)
            .ToList();

        if (groups.Count == 0)
        {
            _logger.LogInformation("No {Kind} duplicate Person BaseItems found.", duplicateKind);
            return;
        }

        _logger.LogInformation("Found {Count} groups of {Kind} duplicate Person BaseItems.", groups.Count, duplicateKind);

        // Counted for every candidate up front: asking per group turns a few queries into one per
        // duplicate, which on a large library is where all the time goes.
        var candidateIds = groups.SelectMany(g => g.Select(p => p.Id)).ToList();
        var userDataCounts = await CountByItemAsync(candidateIds, ids => context.UserData.Where(u => ids.Contains(u.ItemId)).Select(u => u.ItemId), cancellationToken).ConfigureAwait(false);
        var asParentCounts = await CountByItemAsync(candidateIds, ids => context.LinkedChildren.Where(l => ids.Contains(l.ParentId)).Select(l => l.ParentId), cancellationToken).ConfigureAwait(false);
        var asChildCounts = await CountByItemAsync(candidateIds, ids => context.LinkedChildren.Where(l => ids.Contains(l.ChildId)).Select(l => l.ChildId), cancellationToken).ConfigureAwait(false);

        var idsToDelete = new List<Guid>();
        foreach (var group in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Pick the keeper: the Person with the most UserData rows (favorites, image
            // refresh state) is the one users have actually interacted with.
            var stats = group
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.DateCreated,
                    UserDataCount = userDataCounts.GetValueOrDefault(p.Id),
                    LinkedCount = asParentCounts.GetValueOrDefault(p.Id) + asChildCounts.GetValueOrDefault(p.Id),
                })
                .ToList();

            var keeper = stats
                .OrderByDescending(s => s.UserDataCount)
                .ThenByDescending(s => s.LinkedCount)
                .ThenBy(s => s.DateCreated)
                .First();

            foreach (var dup in stats.Where(s => s.Id != keeper.Id))
            {
                var keeperId = keeper.Id;
                var dupId = dup.Id;

                await context.BaseItems
                    .Where(b => b.ParentId == dupId)
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.ParentId, keeperId), cancellationToken)
                    .ConfigureAwait(false);

                await context.BaseItems
                    .Where(b => b.OwnerId == dupId)
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.OwnerId, keeperId), cancellationToken)
                    .ConfigureAwait(false);

                await context.AncestorIds
                    .Where(a => a.ParentItemId == dupId
                        && context.AncestorIds.Any(k => k.ParentItemId == keeperId && k.ItemId == a.ItemId))
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);
                await context.AncestorIds
                    .Where(a => a.ParentItemId == dupId)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.ParentItemId, keeperId), cancellationToken)
                    .ConfigureAwait(false);

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

                await context.UserData
                    .Where(u => u.ItemId == dupId
                        && context.UserData.Any(k => k.ItemId == keeperId && k.UserId == u.UserId && k.CustomDataKey == u.CustomDataKey))
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);
                await context.UserData
                    .Where(u => u.ItemId == dupId)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.ItemId, keeperId), cancellationToken)
                    .ConfigureAwait(false);

                idsToDelete.Add(dupId);
            }

            _logger.LogDebug(
                "Merged Person BaseItems for '{Name}' into {KeeperId} ({Removed} removed).",
                keeper.Name,
                keeper.Id,
                stats.Count - 1);
        }

        if (idsToDelete.Count == 0)
        {
            return;
        }

        // Resolve via LibraryManager so DeleteItemsUnsafeFast can also remove the
        // %MetadataPath%/People/<Letter>/<Name> directories the duplicate stubs left behind.
        // Delete in batches so we never issue one massive delete transaction and progress stays visible.
        _logger.LogInformation("Deleting {Count} duplicate Person BaseItems...", idsToDelete.Count);
        const int DeleteBatchSize = 500;
        var deletedSoFar = 0;
        for (var offset = 0; offset < idsToDelete.Count; offset += DeleteBatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batchIds = idsToDelete.GetRange(offset, Math.Min(DeleteBatchSize, idsToDelete.Count - offset));

            var itemsToDelete = batchIds
                .Select(id => _libraryManager.GetItemById(id))
                .Where(item => item is not null)
                .ToList();
            if (itemsToDelete.Count > 0)
            {
                _libraryManager.DeleteItemsUnsafeFast(itemsToDelete!);
            }

            var deletedIds = itemsToDelete.Select(i => i!.Id).ToHashSet();
            var unresolvedIds = batchIds.Where(id => !deletedIds.Contains(id)).ToList();
            if (unresolvedIds.Count > 0)
            {
                _persistenceService.DeleteItem(unresolvedIds);
            }

            deletedSoFar += batchIds.Count;
            _logger.LogInformation("Deleting duplicate Person BaseItems: {Deleted}/{Total}", deletedSoFar, idsToDelete.Count);
        }
    }

    /// <summary>
    /// Counts the rows each candidate id is referenced by, in chunks small enough for the parameter limit.
    /// </summary>
    private static async Task<Dictionary<Guid, int>> CountByItemAsync(
        IReadOnlyList<Guid> candidateIds,
        Func<Guid[], IQueryable<Guid>> referencesOf,
        CancellationToken cancellationToken)
    {
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

    /// <summary>
    /// Merges Peoples lookup rows whose names collapse to the same key within one person type.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="keySelector">Projects a name onto the identity key.</param>
    /// <param name="duplicateKind">How to describe the duplicates in the log.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public async Task MergePeoplesRowsAsync(
        JellyfinDbContext context,
        Func<string, string> keySelector,
        string duplicateKind,
        CancellationToken cancellationToken)
    {
        var people = await context.Peoples
            .Select(p => new { p.Id, p.Name, p.PersonType })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var groups = people
            .GroupBy(p => (Name: keySelector(p.Name), p.PersonType))
            .Where(g => g.Count() > 1)
            .ToList();

        if (groups.Count == 0)
        {
            _logger.LogInformation("No {Kind} duplicate Peoples rows found.", duplicateKind);
            return;
        }

        _logger.LogInformation("Found {Count} groups of {Kind} duplicate Peoples rows.", groups.Count, duplicateKind);

        var candidateIds = groups.SelectMany(g => g.Select(p => p.Id)).ToList();
        var mapCounts = await CountByItemAsync(
            candidateIds,
            ids => context.PeopleBaseItemMap.Where(m => ids.Contains(m.PeopleId)).Select(m => m.PeopleId),
            cancellationToken).ConfigureAwait(false);

        var idsToDelete = new List<Guid>();
        foreach (var group in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Pick the keeper: the row referenced by the most BaseItems is the one most
            // tracks/movies already point at; the duplicates are usually orphan stubs left
            // by a mismatched insert.
            var stats = group
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    MapCount = mapCounts.GetValueOrDefault(p.Id),
                })
                .ToList();

            var keeper = stats
                .OrderByDescending(s => s.MapCount)
                .ThenBy(s => s.Id)
                .First();

            foreach (var dup in stats.Where(s => s.Id != keeper.Id))
            {
                var keeperId = keeper.Id;
                var dupId = dup.Id;

                // PeopleBaseItemMap PK is (ItemId, PeopleId, Role); drop dup rows that would
                // collide on (ItemId, Role) before redirecting PeopleId. Role is nullable, so
                // match nulls explicitly.
                await context.PeopleBaseItemMap
                    .Where(m => m.PeopleId == dupId
                        && context.PeopleBaseItemMap.Any(k => k.PeopleId == keeperId
                            && k.ItemId == m.ItemId
                            && (k.Role == m.Role || (k.Role == null && m.Role == null))))
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);
                await context.PeopleBaseItemMap
                    .Where(m => m.PeopleId == dupId)
                    .ExecuteUpdateAsync(s => s.SetProperty(m => m.PeopleId, keeperId), cancellationToken)
                    .ConfigureAwait(false);

                idsToDelete.Add(dupId);
            }

            _logger.LogDebug(
                "Merged Peoples rows for '{Name}' into {KeeperId} ({Removed} removed).",
                keeper.Name,
                keeper.Id,
                stats.Count - 1);
        }

        if (idsToDelete.Count == 0)
        {
            return;
        }

        var idx = 0;
        foreach (var item in idsToDelete.Chunk(200))
        {
            idx++; // humans count at one
            _logger.LogInformation("Remove batch {BatchNo}/{MaxBatches} duplicate Peoples.", idx, (idsToDelete.Count / 200) + 1);
            await context.Peoples
                .Where(p => item.Contains(p.Id))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        _logger.LogInformation("Removed {Count} duplicate Peoples rows.", idsToDelete.Count);
    }
}
