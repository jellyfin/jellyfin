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
/// Shared by the migrations that widened that key: first casing, then the clean name.
/// </remarks>
public sealed class DuplicatePeopleMerger
{
    private const string PersonType = "MediaBrowser.Controller.Entities.Person";

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

        // Counted up front: asking per group turns a few queries into one per duplicate.
        var candidateIds = groups.SelectMany(g => g.Select(p => p.Id)).ToList();
        var userDataCounts = await DuplicateItemMerge.CountReferencesAsync(candidateIds, ids => context.UserData.Where(u => ids.Contains(u.ItemId)).Select(u => u.ItemId), cancellationToken).ConfigureAwait(false);
        var asParentCounts = await DuplicateItemMerge.CountReferencesAsync(candidateIds, ids => context.LinkedChildren.Where(l => ids.Contains(l.ParentId)).Select(l => l.ParentId), cancellationToken).ConfigureAwait(false);
        var asChildCounts = await DuplicateItemMerge.CountReferencesAsync(candidateIds, ids => context.LinkedChildren.Where(l => ids.Contains(l.ChildId)).Select(l => l.ChildId), cancellationToken).ConfigureAwait(false);

        var idsToDelete = new List<Guid>();
        foreach (var group in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // The Person with the most UserData rows is the one users have interacted with.
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
                await DuplicateItemMerge.RedirectReferencesAsync(context, dup.Id, keeper.Id, cancellationToken).ConfigureAwait(false);
                idsToDelete.Add(dup.Id);
            }

            _logger.LogDebug(
                "Merged Person BaseItems for '{Name}' into {KeeperId} ({Removed} removed).",
                keeper.Name,
                keeper.Id,
                stats.Count - 1);
        }

        await DuplicateItemMerge
            .DeleteMergedItemsAsync(idsToDelete, "Person BaseItems", _logger, _libraryManager, _persistenceService, cancellationToken)
            .ConfigureAwait(false);
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
            .Select(p => new { p.Id, p.Name, p.PersonType, p.ItemId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Rows pointing at different people are different humans. A row with no person yet folds in
        // only when there is a single candidate.
        var groups = new List<List<Guid>>();
        foreach (var named in people.GroupBy(p => (Name: keySelector(p.Name), p.PersonType)))
        {
            var unlinked = named.Where(p => p.ItemId == Guid.Empty).Select(p => p.Id).ToList();
            var byPerson = named.Where(p => p.ItemId != Guid.Empty).GroupBy(p => p.ItemId).ToList();
            if (byPerson.Count == 0)
            {
                groups.Add(unlinked);
                continue;
            }

            foreach (var person in byPerson)
            {
                var ids = person.Select(p => p.Id).ToList();
                if (byPerson.Count == 1)
                {
                    ids.AddRange(unlinked);
                }

                groups.Add(ids);
            }
        }

        groups = groups.Where(g => g.Count > 1).ToList();
        var namesById = people.ToDictionary(p => p.Id, p => p.Name);

        if (groups.Count == 0)
        {
            _logger.LogInformation("No {Kind} duplicate Peoples rows found.", duplicateKind);
            return;
        }

        _logger.LogInformation("Found {Count} groups of {Kind} duplicate Peoples rows.", groups.Count, duplicateKind);

        var candidateIds = groups.SelectMany(g => g).ToList();
        var mapCounts = await DuplicateItemMerge.CountReferencesAsync(
            candidateIds,
            ids => context.PeopleBaseItemMap.Where(m => ids.Contains(m.PeopleId)).Select(m => m.PeopleId),
            cancellationToken).ConfigureAwait(false);

        var idsToDelete = new List<Guid>();
        foreach (var group in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // The row most items already point at; the duplicates are usually orphan stubs.
            var stats = group
                .Select(id => new
                {
                    Id = id,
                    Name = namesById[id],
                    MapCount = mapCounts.GetValueOrDefault(id),
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

                // PeopleBaseItemMap PK is (ItemId, PeopleId, Role); drop rows that would collide on
                // (ItemId, Role) before redirecting PeopleId.
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
