using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Clears references that point at a row which no longer exists.
/// </summary>
[JellyfinMigration("2026-09-16T12:00:00", nameof(ClearDanglingItemReferences))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class ClearDanglingItemReferences : IAsyncMigrationRoutine
{
    private readonly IStartupLogger<ClearDanglingItemReferences> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClearDanglingItemReferences"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    public ClearDanglingItemReferences(
        IStartupLogger<ClearDanglingItemReferences> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var total = 0;

            void Report(string relation, int rows)
            {
                if (rows > 0)
                {
                    total += rows;
                    _logger.LogInformation("Cleared {Rows} dangling rows in {Relation}", rows, relation);
                }
            }

            // Detached, not deleted: ParentId cascades, so deleting an item because its parent is missing would
            // take its whole live subtree with it.
            Report(
                "BaseItems.ParentId",
                await context.BaseItems
                    .Where(e => e.ParentId.HasValue && !context.BaseItems.Any(p => p.Id.Equals(e.ParentId!.Value)))
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.ParentId, (Guid?)null), cancellationToken)
                    .ConfigureAwait(false));

            Report(
                "BaseItems.OwnerId",
                await context.BaseItems
                    .Where(e => e.OwnerId.HasValue && !context.BaseItems.Any(p => p.Id.Equals(e.OwnerId!.Value)))
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.OwnerId, (Guid?)null), cancellationToken)
                    .ConfigureAwait(false));

            await ParkOrphanedUserDataAsync(context, Report, cancellationToken).ConfigureAwait(false);

            Report(
                "LinkedChildren.ParentId",
                await context.LinkedChildren
                    .Where(e => !context.BaseItems.Any(b => b.Id.Equals(e.ParentId)))
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false));
            Report(
                "LinkedChildren.ChildId",
                await context.LinkedChildren
                    .Where(e => !context.BaseItems.Any(b => b.Id.Equals(e.ChildId)))
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false));

            Report(
                "AncestorIds.ItemId",
                await context.AncestorIds
                    .Where(e => !context.BaseItems.Any(b => b.Id.Equals(e.ItemId)))
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false));
            Report(
                "AncestorIds.ParentItemId",
                await context.AncestorIds
                    .Where(e => !context.BaseItems.Any(b => b.Id.Equals(e.ParentItemId)))
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false));
            Report(
                "AttachmentStreamInfos.ItemId",
                await context.AttachmentStreamInfos
                    .Where(e => !context.BaseItems.Any(b => b.Id.Equals(e.ItemId)))
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false));
            Report(
                "BaseItemImageInfos.ItemId",
                await context.BaseItemImageInfos
                    .Where(e => !context.BaseItems.Any(b => b.Id.Equals(e.ItemId)))
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false));
            Report(
                "BaseItemMetadataFields.ItemId",
                await context.BaseItemMetadataFields
                    .Where(e => !context.BaseItems.Any(b => b.Id.Equals(e.ItemId)))
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false));
            Report(
                "BaseItemProviders.ItemId",
                await context.BaseItemProviders
                    .Where(e => !context.BaseItems.Any(b => b.Id.Equals(e.ItemId)))
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false));
            Report(
                "BaseItemTrailerTypes.ItemId",
                await context.BaseItemTrailerTypes
                    .Where(e => !context.BaseItems.Any(b => b.Id.Equals(e.ItemId)))
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false));
            Report(
                "Chapters.ItemId",
                await context.Chapters
                    .Where(e => !context.BaseItems.Any(b => b.Id.Equals(e.ItemId)))
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false));
            Report(
                "ItemValuesMap.ItemId",
                await context.ItemValuesMap
                    .Where(e => !context.BaseItems.Any(b => b.Id.Equals(e.ItemId)))
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false));
            Report(
                "KeyframeData.ItemId",
                await context.KeyframeData
                    .Where(e => !context.BaseItems.Any(b => b.Id.Equals(e.ItemId)))
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false));
            Report(
                "MediaStreamInfos.ItemId",
                await context.MediaStreamInfos
                    .Where(e => !context.BaseItems.Any(b => b.Id.Equals(e.ItemId)))
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false));
            Report(
                "PeopleBaseItemMap.ItemId",
                await context.PeopleBaseItemMap
                    .Where(e => !context.BaseItems.Any(b => b.Id.Equals(e.ItemId)))
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false));
            Report(
                "PeopleBaseItemMap.PeopleId",
                await context.PeopleBaseItemMap
                    .Where(e => !context.Peoples.Any(p => p.Id.Equals(e.PeopleId)))
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false));

            // TrickplayInfos and MediaSegments are constrained by the migration that runs before this one, which
            // scrubs them on the way, so there is nothing left here for them to hold.

            // CustomItemDisplayPreferences declares neither of its references. Its ItemId is always a real item -
            // unlike ItemDisplayPreferences, where Guid.Empty means the view itself and is not an item at all.
            Report(
                "CustomItemDisplayPreferences.ItemId",
                await context.CustomItemDisplayPreferences
                    .Where(e => !context.BaseItems.Any(b => b.Id.Equals(e.ItemId)))
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false));
            Report(
                "CustomItemDisplayPreferences.UserId",
                await context.CustomItemDisplayPreferences
                    .Where(e => !context.Users.Any(u => u.Id.Equals(e.UserId)))
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false));
            Report(
                "ItemDisplayPreferences.ItemId",
                await context.ItemDisplayPreferences
                    .Where(e => !e.ItemId.Equals(Guid.Empty) && !context.BaseItems.Any(b => b.Id.Equals(e.ItemId)))
                    .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false));

            if (total == 0)
            {
                _logger.LogInformation("No dangling references found");
            }
            else
            {
                _logger.LogInformation("Cleared {Total} dangling references in total", total);
            }
        }
    }

    private static async Task ParkOrphanedUserDataAsync(JellyfinDbContext context, Action<string, int> report, CancellationToken cancellationToken)
    {
        // Play state outlives its item on purpose: DeleteItem parks it on the placeholder so that re-adding the
        // file restores what the user watched, and CleanupUserDataTask retires it later. Rows orphaned by a rebuild
        // never got that treatment, so give it to them now instead of deleting what the user did.
        var orphaned = context.UserData.Where(e => !context.BaseItems.Any(b => b.Id.Equals(e.ItemId)));

        var keys = await orphaned
            .Select(e => new { e.ItemId, e.UserId, e.CustomDataKey, e.LastPlayedDate, e.PlayCount })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (keys.Count == 0)
        {
            return;
        }

        // The primary key is (ItemId, UserId, CustomDataKey), so everything that would land on the same placeholder
        // key has to be resolved to one row first: the best of the orphans, and no pre-existing placeholder row.
        var superseded = keys
            .GroupBy(e => new { e.UserId, e.CustomDataKey })
            .SelectMany(g => g
                .OrderByDescending(e => e.LastPlayedDate)
                .ThenByDescending(e => e.PlayCount)
                .Skip(1))
            .ToList();

        foreach (var duplicate in superseded)
        {
            await context.UserData
                .Where(e => e.ItemId.Equals(duplicate.ItemId)
                    && e.UserId.Equals(duplicate.UserId)
                    && e.CustomDataKey == duplicate.CustomDataKey)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        report("UserData.ItemId (superseded by a better row)", superseded.Count);

        var placeholderId = BaseItemRepository.PlaceholderId;
        var collisions = await context.UserData
            .Join(
                orphaned,
                placeholder => new { placeholder.UserId, placeholder.CustomDataKey },
                userData => new { userData.UserId, userData.CustomDataKey },
                (placeholder, _) => placeholder)
            .Where(e => e.ItemId.Equals(placeholderId))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        report("UserData.ItemId (replacing a stale placeholder row)", collisions);

        report(
            "UserData.ItemId (parked on the placeholder)",
            await orphaned
                .ExecuteUpdateAsync(
                    s => s.SetProperty(e => e.ItemId, placeholderId).SetProperty(e => e.RetentionDate, DateTime.UtcNow),
                    cancellationToken)
                .ConfigureAwait(false));
    }
}
