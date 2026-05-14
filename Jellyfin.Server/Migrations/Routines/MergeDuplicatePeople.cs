#pragma warning disable RS0030 // Do not use banned APIs

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
/// Merges case-only duplicate people. Two passes:
/// 1) Person BaseItems whose Name differs only by casing — Person.GetPath hashes the name
///    verbatim, so two casings produce two distinct Person rows in BaseItems.
/// 2) Peoples lookup rows whose Name differs only by casing within the same PersonType —
///    UpdatePeople used to insert a second Peoples row when a metadata provider returned
///    a different casing than the row already in the table.
/// Both bugs cause the /Persons endpoint to list the same person twice.
/// </summary>
[JellyfinMigration("2026-05-08T13:00:00", nameof(MergeDuplicatePeople))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class MergeDuplicatePeople : IAsyncMigrationRoutine
{
    private const string PersonType = "MediaBrowser.Controller.Entities.Person";

    private readonly IStartupLogger<MergeDuplicatePeople> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;
    private readonly ILibraryManager _libraryManager;
    private readonly IItemPersistenceService _persistenceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MergeDuplicatePeople"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="persistenceService">The item persistence service.</param>
    public MergeDuplicatePeople(
        IStartupLogger<MergeDuplicatePeople> logger,
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
            await MergePersonBaseItemsAsync(context, cancellationToken).ConfigureAwait(false);
            await MergePeoplesRowsAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task MergePersonBaseItemsAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        var persons = await context.BaseItems
            .Where(b => b.Type == PersonType && b.Name != null)
            .Select(b => new { b.Id, b.Name, b.DateCreated })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var groups = persons
            .GroupBy(p => p.Name!.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .ToList();

        if (groups.Count == 0)
        {
            _logger.LogInformation("No case-only duplicate Person BaseItems found.");
            return;
        }

        _logger.LogInformation("Found {Count} groups of case-only duplicate Person BaseItems.", groups.Count);

        var idsToDelete = new List<Guid>();
        foreach (var group in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var groupIds = group.Select(g => g.Id).ToArray();

            // Pick the keeper: the Person with the most UserData rows (favorites, image
            // refresh state) is the one users have actually interacted with.
            var stats = await context.BaseItems
                .Where(b => groupIds.Contains(b.Id))
                .Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.DateCreated,
                    UserDataCount = context.UserData.Count(u => u.ItemId == b.Id),
                    LinkedCount = context.LinkedChildren.Count(l => l.ParentId == b.Id || l.ChildId == b.Id),
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

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
        var itemsToDelete = idsToDelete
            .Select(id => _libraryManager.GetItemById(id))
            .Where(item => item is not null)
            .ToList();
        if (itemsToDelete.Count > 0)
        {
            _libraryManager.DeleteItemsUnsafeFast(itemsToDelete!);
        }

        var deletedIds = itemsToDelete.Select(i => i!.Id).ToHashSet();
        var unresolvedIds = idsToDelete.Where(id => !deletedIds.Contains(id)).ToList();
        if (unresolvedIds.Count > 0)
        {
            _persistenceService.DeleteItem(unresolvedIds);
        }

        _logger.LogInformation("Removed {Count} duplicate Person BaseItems.", idsToDelete.Count);
    }

    private async Task MergePeoplesRowsAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        var people = await context.Peoples
            .Select(p => new { p.Id, p.Name, p.PersonType })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var groups = people
            .GroupBy(p => (Name: p.Name.ToLowerInvariant(), p.PersonType))
            .Where(g => g.Count() > 1)
            .ToList();

        if (groups.Count == 0)
        {
            _logger.LogInformation("No case-only duplicate Peoples rows found.");
            return;
        }

        _logger.LogInformation("Found {Count} groups of case-only duplicate Peoples rows.", groups.Count);

        var idsToDelete = new List<Guid>();
        foreach (var group in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var groupIds = group.Select(g => g.Id).ToArray();

            // Pick the keeper: the row referenced by the most BaseItems is the one most
            // tracks/movies already point at; the duplicates are usually orphan stubs left
            // by a casing-mismatched insert.
            var stats = await context.Peoples
                .Where(p => groupIds.Contains(p.Id))
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    MapCount = context.PeopleBaseItemMap.Count(m => m.PeopleId == p.Id),
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

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

        await context.Peoples
            .Where(p => idsToDelete.Contains(p.Id))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Removed {Count} duplicate Peoples rows.", idsToDelete.Count);
    }
}
