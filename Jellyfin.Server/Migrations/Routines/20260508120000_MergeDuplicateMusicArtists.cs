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
/// Merges MusicArtist records that differ only by Name casing. Prior to the case-insensitive
/// dedup lookup added alongside this migration, the artist validator would create a second
/// MusicArtist whenever a track tagged the artist with a different casing than the
/// resolver-created one (e.g. "Thirty Seconds To Mars" vs. "Thirty Seconds to Mars").
/// </summary>
[JellyfinMigration("2026-05-08T12:00:00", nameof(MergeDuplicateMusicArtists))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class MergeDuplicateMusicArtists : IAsyncMigrationRoutine
{
    private const string MusicArtistType = "MediaBrowser.Controller.Entities.Audio.MusicArtist";

    private readonly IStartupLogger<MergeDuplicateMusicArtists> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;
    private readonly ILibraryManager _libraryManager;
    private readonly IItemPersistenceService _persistenceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MergeDuplicateMusicArtists"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="persistenceService">The item persistence service.</param>
    public MergeDuplicateMusicArtists(
        IStartupLogger<MergeDuplicateMusicArtists> logger,
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
            var artists = await context.BaseItems
                .Where(b => b.Type == MusicArtistType && b.Name != null)
                .Select(b => new { b.Id, b.Name, b.DateCreated })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var groups = artists
                .GroupBy(a => a.Name!.ToLowerInvariant())
                .Where(g => g.Count() > 1)
                .ToList();

            if (groups.Count == 0)
            {
                _logger.LogInformation("No case-only duplicate MusicArtist records found.");
                return;
            }

            _logger.LogInformation("Found {Count} groups of case-only duplicate MusicArtist records.", groups.Count);

            var idsToDelete = new List<Guid>();
            foreach (var group in groups)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var groupIds = group.Select(g => g.Id).ToArray();

                // Pick the keeper: the artist with the most child references is the "real" one
                // (the resolver-created artist with a filesystem path); the duplicates are usually
                // empty stubs created by the validator's case-sensitive miss.
                var stats = await context.BaseItems
                    .Where(b => groupIds.Contains(b.Id))
                    .Select(b => new
                    {
                        b.Id,
                        b.Name,
                        b.DateCreated,
                        ChildCount = context.BaseItems.Count(c => c.ParentId == b.Id),
                        AncestorCount = context.AncestorIds.Count(a => a.ParentItemId == b.Id),
                        LinkedCount = context.LinkedChildren.Count(l => l.ParentId == b.Id || l.ChildId == b.Id),
                    })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                var keeper = stats
                    .OrderByDescending(s => s.ChildCount)
                    .ThenByDescending(s => s.AncestorCount)
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

                    // UserData has UNIQUE(UserId, CustomDataKey); keep the dup's row only when the
                    // keeper has no equivalent row, otherwise the keeper's value wins.
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
                    "Merged duplicates for '{Name}' into {KeeperId} ({Removed} removed).",
                    keeper.Name,
                    keeper.Id,
                    stats.Count - 1);
            }

            if (idsToDelete.Count == 0)
            {
                return;
            }

            // Resolve via LibraryManager so DeleteItemsUnsafeFast can also remove the
            // %MetadataPath%/artists/<Name> directories that the duplicate stubs left behind.
            // Fall back to the persistence service for any items the LibraryManager can't resolve.
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

            _logger.LogInformation("Removed {Count} duplicate MusicArtist records.", idsToDelete.Count);
        }
    }
}
