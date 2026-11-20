#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Playlists;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BaseItemDto = MediaBrowser.Controller.Entities.BaseItem;
using DbLinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;
using LinkedChildType = MediaBrowser.Controller.Entities.LinkedChildType;

namespace Jellyfin.Server.Implementations.Item;

/// <summary>
/// Handles item persistence operations (save, delete, update).
/// </summary>
public class ItemPersistenceService : IItemPersistenceService
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IServerApplicationHost _appHost;
    private readonly ILogger<ItemPersistenceService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemPersistenceService"/> class.
    /// </summary>
    /// <param name="dbProvider">The database context factory.</param>
    /// <param name="appHost">The application host.</param>
    /// <param name="logger">The logger.</param>
    public ItemPersistenceService(
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IServerApplicationHost appHost,
        ILogger<ItemPersistenceService> logger)
    {
        _dbProvider = dbProvider;
        _appHost = appHost;
        _logger = logger;
    }

    /// <inheritdoc />
    public void DeleteItem(params IReadOnlyList<Guid> ids)
    {
        if (ids is null || ids.Count == 0 || ids.Any(f => f.Equals(BaseItemRepository.PlaceholderId)))
        {
            throw new ArgumentException("Guid can't be empty or the placeholder id.", nameof(ids));
        }

        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();

        var date = (DateTime?)DateTime.UtcNow;

        var descendantIds = DescendantQueryHelper.GetOwnedDescendantIdsBatch(context, ids);
        foreach (var id in ids)
        {
            descendantIds.Add(id);
        }

        // Use WhereOneOrMany instead of a raw HashSet.Contains so large id sets are bound as a
        // single parameter (json_each) rather than one SQL variable per id, which would otherwise
        // overflow SQLite's variable limit when deleting many items at once (e.g. migrations).
        var frontier = descendantIds.ToArray();
        while (frontier.Length > 0)
        {
            var ownedIds = context.BaseItems
                .Where(e => e.OwnerId.HasValue)
                .WhereOneOrMany(frontier, e => e.OwnerId!.Value)
                .Select(e => e.Id)
                .ToArray();

            var childIds = context.BaseItems
                .Where(e => e.ParentId.HasValue)
                .WhereOneOrMany(frontier, e => e.ParentId!.Value)
                .Select(e => e.Id)
                .ToArray();

            // Only ids that were not already known become the next frontier, so ownership cycles
            // terminate instead of looping forever.
            frontier = [.. ownedIds.Concat(childIds).Where(e => descendantIds.Add(e))];
        }

        var relatedItems = descendantIds.ToArray();

        // When batch-deleting, multiple items may have UserData for the same (UserId, CustomDataKey).
        // Moving all of them to PlaceholderId would violate the UNIQUE constraint.
        // Deduplicate by loading keys client-side, keeping the best row per group.
        var batchUserData = context.UserData.WhereOneOrMany(relatedItems, e => e.ItemId);

        var allRows = batchUserData
            .Select(ud => new { ud.ItemId, ud.UserId, ud.CustomDataKey, ud.LastPlayedDate, ud.PlayCount })
            .ToList();

        var duplicateRows = allRows
            .GroupBy(ud => new { ud.UserId, ud.CustomDataKey })
            .Where(g => g.Count() > 1)
            .SelectMany(g => g
                .OrderByDescending(ud => ud.LastPlayedDate)
                .ThenByDescending(ud => ud.PlayCount)
                .Skip(1))
            .ToList();

        foreach (var dup in duplicateRows)
        {
            context.UserData
                .Where(ud => ud.ItemId == dup.ItemId && ud.UserId == dup.UserId && ud.CustomDataKey == dup.CustomDataKey)
                .ExecuteDelete();
        }

        // Delete existing placeholder rows that would conflict with the incoming ones
        context.UserData
            .Join(
                batchUserData,
                placeholder => new { placeholder.UserId, placeholder.CustomDataKey },
                userData => new { userData.UserId, userData.CustomDataKey },
                (placeholder, userData) => placeholder)
            .Where(e => e.ItemId == BaseItemRepository.PlaceholderId)
            .ExecuteDelete();

        batchUserData
            .ExecuteUpdate(e => e
                .SetProperty(f => f.RetentionDate, date)
                .SetProperty(f => f.ItemId, BaseItemRepository.PlaceholderId));

        context.AncestorIds.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.AncestorIds.WhereOneOrMany(relatedItems, e => e.ParentItemId).ExecuteDelete();
        context.AttachmentStreamInfos.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.BaseItemImageInfos.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.BaseItemMetadataFields.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.BaseItemProviders.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.BaseItemTrailerTypes.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.Chapters.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.CustomItemDisplayPreferences.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.ItemDisplayPreferences.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.BaseItemTags.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        // Both ends of the link. The ItemId side is what the foreign key would cascade anyway; the
        // GenreItemId and StudioItemId side has no key behind it, so deleting a genre or studio item
        // without these would leave every link to it pointing at nothing.
        context.BaseItemGenres.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.BaseItemStudios.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.BaseItemGenres.WhereOneOrMany(relatedItems, e => e.GenreItemId).ExecuteDelete();
        context.BaseItemStudios.WhereOneOrMany(relatedItems, e => e.StudioItemId).ExecuteDelete();
        context.LinkedChildren.WhereOneOrMany(relatedItems, e => e.ParentId).ExecuteDelete();
        context.LinkedChildren.WhereOneOrMany(relatedItems, e => e.ChildId).ExecuteDelete();
        var peopleIds = context.PeopleBaseItemMap.WhereOneOrMany(relatedItems, e => e.ItemId).Select(f => f.PeopleId).Distinct().ToArray();
        context.BaseItems.WhereOneOrMany(relatedItems, e => e.Id).ExecuteDelete();
        context.KeyframeData.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.MediaSegments.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.MediaStreamInfos.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.PeopleBaseItemMap.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.Peoples.WhereOneOrMany(peopleIds, e => e.Id).Where(e => !e.BaseItems!.Any()).ExecuteDelete();
        context.TrickplayInfos.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.SaveChanges();
        transaction.Commit();
    }

    /// <inheritdoc />
    public void SaveItems(IReadOnlyList<BaseItemDto> items, CancellationToken cancellationToken)
    {
        UpdateOrInsertItems(items, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveImagesAsync(BaseItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var images = item.ImageInfos.Select(e => BaseItemMapper.MapImageToEntity(item.Id, e)).ToArray();

        var context = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            await context.BaseItemImageInfos
                .Where(e => e.ItemId == item.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            await context.BaseItemImageInfos
                .AddRangeAsync(images, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateException)
            {
                // Checking that the item exists before writing leaves a gap a scan can delete it
                // through, turning the insert into a foreign key violation that fails the whole
                // refresh instead of the no-op intended here. Let the insert be the check: it is the
                // only point at which the answer cannot go stale. Nothing is orphaned by the delete
                // above, because deleting the item cascades to its images anyway.
                if (await context.BaseItems
                    .AnyAsync(bi => bi.Id == item.Id, cancellationToken)
                    .ConfigureAwait(false))
                {
                    throw;
                }

                _logger.LogWarning("Unable to save ImageInfo for non existing BaseItem {ItemId}", item.Id);
            }
        }
    }

    /// <inheritdoc />
    public async Task ReattachUserDataAsync(BaseItemDto item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();

        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await using (dbContext.ConfigureAwait(false))
        {
            var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                var userKeys = item.GetUserDataKeys().Distinct().ToList();

                var detached = await dbContext.UserData
                    .Where(e => e.ItemId == BaseItemRepository.PlaceholderId)
                    .Where(e => userKeys.Contains(e.CustomDataKey))
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (detached.Count > 0)
                {
                    await ReconcileUserDataAsync(dbContext, item, userKeys, detached, cancellationToken).ConfigureAwait(false);
                }

                item.UserData = await dbContext.UserData
                    .AsNoTracking()
                    .Where(e => e.ItemId == item.Id)
                    .ToArrayAsync(cancellationToken)
                    .ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task ReconcileUserDataAsync(
        JellyfinDbContext dbContext,
        BaseItemDto item,
        IReadOnlyList<string> userKeys,
        List<UserData> detached,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.UserData
            .Where(e => e.ItemId == item.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Keys the item no longer reports are carried over: they may only be missing mid-refresh.
        var winners = detached.Concat(existing)
            .GroupBy(e => e.UserId)
            .Select(g => (
                Winner: g
                    .OrderByDescending(e => e.LastPlayedDate)
                    .ThenByDescending(e => e.PlayCount)
                    .ThenByDescending(e => e.PlaybackPositionTicks)
                    .First(),
                Keys: userKeys.Union(g.Select(e => e.CustomDataKey)).ToList()))
            .ToList();

        dbContext.UserData.RemoveRange(detached);
        dbContext.UserData.RemoveRange(existing);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var (winner, keys) in winners)
        {
            foreach (var key in keys)
            {
                dbContext.UserData.Add(new UserData
                {
                    ItemId = item.Id,
                    Item = null,
                    UserId = winner.UserId,
                    User = null,
                    CustomDataKey = key,
                    RetentionDate = null,
                    AudioStreamIndex = winner.AudioStreamIndex,
                    IsFavorite = winner.IsFavorite,
                    LastPlayedDate = winner.LastPlayedDate,
                    Likes = winner.Likes,
                    PlaybackPositionTicks = winner.PlaybackPositionTicks,
                    PlayCount = winner.PlayCount,
                    Played = winner.Played,
                    Rating = winner.Rating,
                    SubtitleStreamIndex = winner.SubtitleStreamIndex
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private void UpdateOrInsertItems(IReadOnlyList<BaseItemDto> items, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        cancellationToken.ThrowIfCancellationRequested();

        var tuples = new List<(BaseItemDto Item, List<Guid>? AncestorIds, BaseItemDto TopParent, IEnumerable<string> UserDataKey)>();
        foreach (var item in items.GroupBy(e => e.Id).Select(e => e.Last()).Where(e => e.Id != BaseItemRepository.PlaceholderId))
        {
            var ancestorIds = item.SupportsAncestors ?
                item.GetAncestorIds().Distinct().ToList() :
                null;

            var topParent = item.GetTopParent();

            var userdataKey = item.GetUserDataKeys();

            tuples.Add((item, ancestorIds, topParent, userdataKey));
        }

        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();

        var ids = tuples.Select(f => f.Item.Id).ToArray();
        var existingItems = context.BaseItems.WhereOneOrMany(ids, e => e.Id).Select(f => f.Id).ToHashSet();

        foreach (var item in tuples)
        {
            var entity = BaseItemMapper.Map(item.Item, _appHost);
            entity.TopParentId = item.TopParent?.Id;

            if (!existingItems.Contains(entity.Id))
            {
                context.BaseItems.Add(entity);
            }
            else
            {
                if (entity.Images is { Count: > 0 })
                {
                    context.BaseItemImageInfos.AddRange(entity.Images);
                }

                if (entity.LockedFields is { Count: > 0 })
                {
                    context.BaseItemMetadataFields.AddRange(entity.LockedFields);
                }

                context.BaseItems.Attach(entity).State = EntityState.Modified;
            }
        }

        UpdateTags(context, tuples, ids);
        UpdateItemByNameLinks(context, tuples);

        var itemsWithAncestors = tuples
            .Where(t => t.Item.SupportsAncestors && t.AncestorIds != null)
            .Select(t => t.Item.Id)
            .ToList();

        var allExistingAncestorIds = itemsWithAncestors.Count > 0
            ? context.AncestorIds
                .Where(e => itemsWithAncestors.Contains(e.ItemId))
                .ToList()
                .GroupBy(e => e.ItemId)
                .ToDictionary(g => g.Key, g => g.ToList())
            : new Dictionary<Guid, List<AncestorId>>();

        var allRequestedAncestorIds = tuples
            .Where(t => t.Item.SupportsAncestors && t.AncestorIds != null)
            .SelectMany(t => t.AncestorIds!)
            .Distinct()
            .ToList();

        var validAncestorIdsSet = allRequestedAncestorIds.Count > 0
            ? context.BaseItems
                .Where(e => allRequestedAncestorIds.Contains(e.Id))
                .Select(f => f.Id)
                .ToHashSet()
            : new HashSet<Guid>();

        foreach (var item in tuples)
        {
            if (item.Item.SupportsAncestors && item.AncestorIds != null)
            {
                var existingAncestorIds = allExistingAncestorIds.GetValueOrDefault(item.Item.Id) ?? new List<AncestorId>();
                var validAncestorIds = item.AncestorIds.Where(id => validAncestorIdsSet.Contains(id)).ToArray();
                foreach (var ancestorId in validAncestorIds)
                {
                    var existingAncestorId = existingAncestorIds.FirstOrDefault(e => e.ParentItemId == ancestorId);
                    if (existingAncestorId is null)
                    {
                        context.AncestorIds.Add(new AncestorId()
                        {
                            ParentItemId = ancestorId,
                            ItemId = item.Item.Id,
                            Item = null!,
                            ParentItem = null!
                        });
                    }
                    else
                    {
                        existingAncestorIds.Remove(existingAncestorId);
                    }
                }

                context.AncestorIds.RemoveRange(existingAncestorIds);
            }
        }

        // Owned rows of updated items are rewritten wholesale; cleared in one statement per table.
        if (existingItems.Count > 0)
        {
            var updatedIds = existingItems.ToArray();
            context.BaseItemProviders.WhereOneOrMany(updatedIds, e => e.ItemId).ExecuteDelete();
            context.BaseItemImageInfos.WhereOneOrMany(updatedIds, e => e.ItemId).ExecuteDelete();
            context.BaseItemMetadataFields.WhereOneOrMany(updatedIds, e => e.ItemId).ExecuteDelete();
        }

        context.SaveChanges();

        var folderIds = tuples
            .Where(t => t.Item is Folder)
            .Select(t => t.Item.Id)
            .ToList();

        var videoIds = tuples
            .Where(t => t.Item is Video)
            .Select(t => t.Item.Id)
            .ToList();

        var allLinkedChildrenByParent = new Dictionary<Guid, List<LinkedChildEntity>>();
        if (folderIds.Count > 0 || videoIds.Count > 0)
        {
            var allParentIds = folderIds.Concat(videoIds).Distinct().ToList();
            var allLinkedChildren = context.LinkedChildren
                .Where(e => allParentIds.Contains(e.ParentId))
                .ToList();

            allLinkedChildrenByParent = allLinkedChildren
                .GroupBy(e => e.ParentId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        foreach (var item in tuples)
        {
            // A container that was never hydrated cannot be used to rewrite its links: its empty
            // array means "unknown", so clearing the stored rows would silently empty the item.
            if (item.Item is Folder { LinkedChildrenLoaded: false })
            {
                continue;
            }

            if (item.Item is Folder or Video
                && allLinkedChildrenByParent.TryGetValue(item.Item.Id, out var existingLinks)
                && existingLinks.Count > 0)
            {
                // A video only owns its alternate version links; any other link on that parent is
                // written by the folder branch below and must survive.
                var staleLinks = item.Item is Folder
                    ? existingLinks
                    : existingLinks
                        .Where(e => e.ChildType is DbLinkedChildType.LocalAlternateVersion or DbLinkedChildType.LinkedAlternateVersion)
                        .ToList();

                if (staleLinks.Count > 0)
                {
                    context.LinkedChildren.RemoveRange(staleLinks);
                }
            }
        }

        context.SaveChanges();

        // A LinkedChild's ItemId is only a cache.
        var cachedChildIds = tuples
            .Select(t => t.Item)
            .OfType<Folder>()
            .Where(f => f.LinkedChildrenLoaded)
            .SelectMany(f => f.LinkedChildren)
            .Where(lc => lc.ItemId.HasValue && !lc.ItemId.Value.IsEmpty())
            .Select(lc => lc.ItemId!.Value)
            .Distinct()
            .ToList();

        var knownChildIds = cachedChildIds.Count > 0
            ? context.BaseItems
                .WhereOneOrMany(cachedChildIds, e => e.Id)
                .Select(e => e.Id)
                .ToHashSet()
            : [];

        foreach (var item in tuples)
        {
            if (item.Item is Folder { LinkedChildrenLoaded: true } folder && folder.LinkedChildren.Length > 0)
            {
#pragma warning disable CS0618 // Type or member is obsolete - legacy path resolution for old data
                var pathsToResolve = folder.LinkedChildren
                    .Where(lc => !string.IsNullOrEmpty(lc.Path)
                        && (!lc.ItemId.HasValue || lc.ItemId.Value.IsEmpty() || !knownChildIds.Contains(lc.ItemId.Value)))
                    .Select(lc => lc.Path)
                    .Distinct()
                    .ToList();

                var pathToIdMap = pathsToResolve.Count > 0
                    ? context.BaseItems
                        .Where(e => e.Path != null && pathsToResolve.Contains(e.Path))
                        .Select(e => new { e.Path, e.Id })
                        .GroupBy(e => e.Path!)
                        .ToDictionary(g => g.Key, g => g.First().Id)
                    : [];

                var resolvedChildren = new List<(LinkedChild Child, Guid ChildId)>();
                foreach (var linkedChild in folder.LinkedChildren)
                {
                    var childItemId = linkedChild.ItemId;
                    if (!childItemId.HasValue || childItemId.Value.IsEmpty() || !knownChildIds.Contains(childItemId.Value))
                    {
                        if (!string.IsNullOrEmpty(linkedChild.Path) && pathToIdMap.TryGetValue(linkedChild.Path, out var resolvedId))
                        {
                            childItemId = resolvedId;
                        }
                        else if (Guid.TryParse(linkedChild.LibraryItemId, out var libraryItemId) && !libraryItemId.IsEmpty())
                        {
                            childItemId = libraryItemId;
                        }
                    }
#pragma warning restore CS0618

                    if (childItemId.HasValue && !childItemId.Value.IsEmpty())
                    {
                        resolvedChildren.Add((linkedChild, childItemId.Value));
                    }
                }

                // Playlists may legitimately contain the same item multiple times (e.g. a song repeated
                // in an .m3u file). Every other container type keeps a single entry per child.
                var isPlaylist = folder is Playlist;
                if (!isPlaylist)
                {
                    resolvedChildren = resolvedChildren
                        .GroupBy(c => c.ChildId)
                        .Select(g => g.Last())
                        .ToList();
                }

                var childIdsToCheck = resolvedChildren.Select(c => c.ChildId).Distinct().ToList();
                var existingChildIds = childIdsToCheck.Count > 0
                    ? context.BaseItems
                        .WhereOneOrMany(childIdsToCheck, e => e.Id)
                        .Select(e => e.Id)
                        .ToHashSet()
                    : [];

                var sortOrder = 0;
                foreach (var (linkedChild, childId) in resolvedChildren)
                {
                    if (!existingChildIds.Contains(childId))
                    {
#pragma warning disable CS0618 // Type or member is obsolete - legacy path is logged for diagnostics
                        _logger.LogWarning(
                            "Skipping LinkedChild for parent {ParentName} ({ParentId}): child item {ChildId} (path {ChildPath}) does not exist in database",
                            item.Item.Name,
                            item.Item.Id,
                            childId,
                            linkedChild.Path ?? "unknown");
#pragma warning restore CS0618
                        continue;
                    }

                    context.LinkedChildren.Add(new LinkedChildEntity()
                    {
                        ParentId = item.Item.Id,
                        ChildId = childId,
                        ChildType = (DbLinkedChildType)linkedChild.Type,
                        SortOrder = sortOrder
                    });

                    sortOrder++;
                }
            }

            if (item.Item is Video video)
            {
                var newLinkedChildren = new List<(Guid ChildId, LinkedChildType Type)>();

                if (video.LocalAlternateVersions.Length > 0)
                {
                    var pathsToResolve = video.LocalAlternateVersions.Where(p => !string.IsNullOrEmpty(p)).ToList();
                    if (pathsToResolve.Count > 0)
                    {
                        var pathToIdMap = context.BaseItems
                            .Where(e => e.Path != null && pathsToResolve.Contains(e.Path))
                            .Select(e => new { e.Path, e.Id })
                            .GroupBy(e => e.Path!)
                            .ToDictionary(g => g.Key, g => g.First().Id);

                        foreach (var path in pathsToResolve)
                        {
                            if (pathToIdMap.TryGetValue(path, out var childId))
                            {
                                newLinkedChildren.Add((childId, LinkedChildType.LocalAlternateVersion));
                            }
                        }
                    }
                }

                if (video.LinkedAlternateVersions.Length > 0)
                {
                    foreach (var linkedChild in video.LinkedAlternateVersions)
                    {
                        if (linkedChild.ItemId.HasValue && !linkedChild.ItemId.Value.IsEmpty())
                        {
                            newLinkedChildren.Add((linkedChild.ItemId.Value, LinkedChildType.LinkedAlternateVersion));
                        }
                    }
                }

                // Deduplicate; local (file-based) relationships take priority over linked (user-merged)
                // ones, matching the LinkedChildren migration.
                newLinkedChildren = newLinkedChildren
                    .GroupBy(c => c.ChildId)
                    .Select(g => g.OrderBy(c => c.Type == LinkedChildType.LocalAlternateVersion ? 0 : 1).First())
                    .ToList();

                var childIdsToCheck = newLinkedChildren.Select(c => c.ChildId).ToList();
                var existingChildIds = childIdsToCheck.Count > 0
                    ? context.BaseItems
                        .Where(e => childIdsToCheck.Contains(e.Id))
                        .Select(e => e.Id)
                        .ToHashSet()
                    : [];

                var sortOrder = 0;
                foreach (var (childId, childType) in newLinkedChildren)
                {
                    if (!existingChildIds.Contains(childId))
                    {
                        _logger.LogWarning(
                            "Skipping alternate version for video {VideoName} ({VideoId}): child item {ChildId} does not exist in database",
                            video.Name,
                            video.Id,
                            childId);
                        continue;
                    }

                    context.LinkedChildren.Add(new LinkedChildEntity
                    {
                        ParentId = video.Id,
                        ChildId = childId,
                        ChildType = (DbLinkedChildType)childType,
                        SortOrder = sortOrder
                    });

                    sortOrder++;
                }

                var linkedChildIds = newLinkedChildren
                    .Select(c => c.ChildId)
                    // A video listed among its own versions would be pointed at itself.
                    .Where(childId => existingChildIds.Contains(childId) && !childId.Equals(video.Id))
                    .Where(childId => !childId.Equals(video.PrimaryVersionId))
                    .ToList();
                if (linkedChildIds.Count > 0)
                {
                    var demotedChildren = context.BaseItems
                        .Where(e => linkedChildIds.Contains(e.Id)
                            && (e.PrimaryVersionId == null || e.PrimaryVersionId != video.Id))
                        .ToList();

                    foreach (var child in demotedChildren)
                    {
                        child.PrimaryVersionId = video.Id;

                        // Mirrors Video.CreatePresentationUniqueKey, so presentation-key grouping
                        // collapses the version onto its primary as well.
                        child.PresentationUniqueKey = video.Id.ToString("N", CultureInfo.InvariantCulture);
                    }

                    if (demotedChildren.Count > 0)
                    {
                        _logger.LogInformation(
                            "Set PrimaryVersionId on {Count} alternate versions of video {VideoName} ({VideoId})",
                            demotedChildren.Count,
                            video.Name,
                            video.Id);
                    }
                }

                // A previously-linked LocalAlternateVersion that is no longer present becomes orphaned;
                var previousLinkedChildren = allLinkedChildrenByParent.GetValueOrDefault(video.Id);
                if (previousLinkedChildren is { Count: > 0 })
                {
                    var newChildIds = newLinkedChildren.Select(c => c.ChildId).ToHashSet();
                    var orphanedLocalVersionIds = previousLinkedChildren
                        .Where(e => e.ChildType == DbLinkedChildType.LocalAlternateVersion && !newChildIds.Contains(e.ChildId))
                        .Select(e => e.ChildId)
                        .ToList();

                    if (orphanedLocalVersionIds.Count > 0)
                    {
                        var orphanedItems = context.BaseItems
                            .Where(e => orphanedLocalVersionIds.Contains(e.Id) && e.OwnerId == video.Id)
                            .ToList();

                        if (orphanedItems.Count > 0)
                        {
                            _logger.LogInformation(
                                "Deleting {Count} orphaned LocalAlternateVersion items for video {VideoName} ({VideoId})",
                                orphanedItems.Count,
                                video.Name,
                                video.Id);
                            context.BaseItems.RemoveRange(orphanedItems);
                        }
                    }
                }
            }
        }

        context.SaveChanges();
        transaction.Commit();
    }

    private static void UpdateTags(
        JellyfinDbContext context,
        List<(BaseItemDto Item, List<Guid>? AncestorIds, BaseItemDto TopParent, IEnumerable<string> UserDataKey)> tuples,
        Guid[] ids)
    {
        var existingTags = context.BaseItemTags.Where(e => ids.Contains(e.ItemId)).ToList();

        foreach (var (item, _, _, _) in tuples)
        {
            var itemTags = existingTags.Where(e => e.ItemId == item.Id).ToList();

            foreach (var tag in item.Tags.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.Ordinal))
            {
                var existingTag = itemTags.FirstOrDefault(e => string.Equals(e.Value, tag, StringComparison.Ordinal));
                if (existingTag is null)
                {
                    context.BaseItemTags.Add(new BaseItemTag()
                    {
                        Item = null!,
                        ItemId = item.Id,
                        Value = tag,
                        CleanValue = tag.GetCleanValue()
                    });
                }
                else
                {
                    itemTags.Remove(existingTag);
                }
            }

            context.BaseItemTags.RemoveRange(itemTags);
        }
    }

    // Ids that were never resolved mean unknown, not none, so those links are left alone.
    private static void UpdateItemByNameLinks(
        JellyfinDbContext context,
        List<(BaseItemDto Item, List<Guid>? AncestorIds, BaseItemDto TopParent, IEnumerable<string> UserDataKey)> tuples)
    {
        var withGenres = tuples.Select(e => e.Item).Where(e => e.GenreItemIds is not null).Select(e => (e.Id, GenreItemIds: e.GenreItemIds!)).ToArray();
        if (withGenres.Length > 0)
        {
            var itemIds = withGenres.Select(e => e.Id).ToArray();
            var existingGenres = context.BaseItemGenres.WhereOneOrMany(itemIds, e => e.ItemId).ToLookup(e => e.ItemId);

            foreach (var item in withGenres)
            {
                var itemGenres = existingGenres[item.Id].ToList();
                foreach (var genreId in item.GenreItemIds)
                {
                    var existingLink = itemGenres.FirstOrDefault(e => e.GenreItemId == genreId);
                    if (existingLink is null)
                    {
                        context.BaseItemGenres.Add(new BaseItemGenre()
                        {
                            Item = null!,
                            ItemId = item.Id,
                            GenreItemId = genreId
                        });
                    }
                    else
                    {
                        itemGenres.Remove(existingLink);
                    }
                }

                context.BaseItemGenres.RemoveRange(itemGenres);
            }
        }

        var withStudios = tuples.Select(e => e.Item).Where(e => e.StudioItemIds is not null).Select(e => (e.Id, StudioItemIds: e.StudioItemIds!)).ToArray();
        if (withStudios.Length > 0)
        {
            var itemIds = withStudios.Select(e => e.Id).ToArray();
            var existingStudios = context.BaseItemStudios.WhereOneOrMany(itemIds, e => e.ItemId).ToLookup(e => e.ItemId);

            foreach (var item in withStudios)
            {
                var itemStudios = existingStudios[item.Id].ToList();
                foreach (var studioId in item.StudioItemIds)
                {
                    var existingLink = itemStudios.FirstOrDefault(e => e.StudioItemId == studioId);
                    if (existingLink is null)
                    {
                        context.BaseItemStudios.Add(new BaseItemStudio()
                        {
                            Item = null!,
                            ItemId = item.Id,
                            StudioItemId = studioId
                        });
                    }
                    else
                    {
                        itemStudios.Remove(existingLink);
                    }
                }

                context.BaseItemStudios.RemoveRange(itemStudios);
            }
        }
    }
}
