#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.LiveTv;
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
    /// <summary>
    /// The number of items that, saved one at a time after their batch failed, must each fail in the same way before
    /// the error is taken to concern the whole batch rather than single items.
    /// </summary>
    internal const int IdenticalFailuresBeforeGivingUp = 3;

    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IJellyfinDatabaseProvider _databaseProvider;
    private readonly IServerApplicationHost _appHost;
    private readonly ILogger<ItemPersistenceService> _logger;
    private readonly WriteConflictRetry _writeConflictRetry;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemPersistenceService"/> class.
    /// </summary>
    /// <param name="dbProvider">The database context factory.</param>
    /// <param name="databaseProvider">The database provider, which classifies write conflicts and tells the data errors of a failed save apart.</param>
    /// <param name="appHost">The application host.</param>
    /// <param name="logger">The logger.</param>
    public ItemPersistenceService(
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IJellyfinDatabaseProvider databaseProvider,
        IServerApplicationHost appHost,
        ILogger<ItemPersistenceService> logger)
    {
        _dbProvider = dbProvider;
        _databaseProvider = databaseProvider;
        _appHost = appHost;
        _logger = logger;
        _writeConflictRetry = new WriteConflictRetry(databaseProvider, logger);
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
        context.ItemValuesMap.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.LinkedChildren.WhereOneOrMany(relatedItems, e => e.ParentId).ExecuteDelete();
        context.LinkedChildren.WhereOneOrMany(relatedItems, e => e.ChildId).ExecuteDelete();
        var peopleIds = context.PeopleBaseItemMap.WhereOneOrMany(relatedItems, e => e.ItemId).Select(f => f.PeopleId).Distinct().ToArray();

        // Some servers check a foreign key per row rather than at the end of the statement, so an item cannot be deleted
        // while another item that is deleted in the same statement still names it as its owner. They also stop a cascade
        // that is nested more than 15 levels deep, which the ParentId cascade of a deep folder tree reaches, so the rows
        // are cut loose from their parents as well and the delete cascades one level into the tables that hold their data.
        context.BaseItems
            .WhereOneOrMany(relatedItems, e => e.Id)
            .Where(e => e.OwnerId != null || e.ParentId != null)
            .ExecuteUpdate(e => e
                .SetProperty(f => f.OwnerId, (Guid?)null)
                .SetProperty(f => f.ParentId, (Guid?)null));
        context.BaseItems.WhereOneOrMany(relatedItems, e => e.Id).ExecuteDelete();
        context.ItemValues.Where(e => !e.BaseItemsMap!.Any()).ExecuteDelete();
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
    public void UpdateInheritedValues()
    {
        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();

        context.ItemValuesMap.Where(e => e.ItemValue.Type == ItemValueType.InheritedTags).ExecuteDelete();
        context.SaveChanges();

        transaction.Commit();
    }

    /// <inheritdoc />
    public void SaveItems(IReadOnlyList<BaseItemDto> items, CancellationToken cancellationToken)
    {
        UpdateOrInsertItems(items, false, cancellationToken);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<Guid> SaveItemsSkippingFailures(IReadOnlyList<BaseItemDto> items, CancellationToken cancellationToken)
    {
        return UpdateOrInsertItems(items, true, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveImagesAsync(BaseItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        RemoveNulCharacters(item.ImageInfos);
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

        await _writeConflictRetry
            .ExecuteAsync(ct => ReattachUserDataOnceAsync(item, ct), cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task ReattachUserDataOnceAsync(BaseItemDto item, CancellationToken cancellationToken)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await using (dbContext.ConfigureAwait(false))
        {
            var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                var rawKeys = item.GetUserDataKeys().ToList();
                var userKeys = rawKeys.Select(e => KeyNormalizer.NormalizeUserDataKey(e.RemoveNullCharacters())).Distinct().ToList();

                // A row detached before NUL characters were removed is stored under the key as it was reported.
                var lookupKeys = rawKeys.Select(KeyNormalizer.NormalizeUserDataKey).Concat(userKeys).Distinct().ToList();

                var detached = await dbContext.UserData
                    .Where(e => e.ItemId == BaseItemRepository.PlaceholderId)
                    .Where(e => lookupKeys.Contains(e.CustomDataKey))
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
                Keys: userKeys.Union(g.Select(e => KeyNormalizer.NormalizeUserDataKey(e.CustomDataKey.RemoveNullCharacters()))).ToList()))
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

    private IReadOnlyCollection<Guid> UpdateOrInsertItems(IReadOnlyList<BaseItemDto> items, bool skipFailures, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        cancellationToken.ThrowIfCancellationRequested();

        var tuples = new List<(BaseItemDto Item, List<Guid>? AncestorIds, BaseItemDto TopParent, IEnumerable<string> UserDataKey, List<string> InheritedTags)>();
        foreach (var item in items.GroupBy(e => e.Id).Select(e => e.Last()).Where(e => e.Id != BaseItemRepository.PlaceholderId))
        {
            RemoveNulCharacters(item);
            DropOverlongProviderIds(item);

            var ancestorIds = item.SupportsAncestors ?
                item.GetAncestorIds().Distinct().ToList() :
                null;

            var topParent = item.GetTopParent();

            var userdataKey = item.GetUserDataKeys();
            var inheritedTags = item.GetInheritedTags();

            tuples.Add((item, ancestorIds, topParent, userdataKey, inheritedTags));
        }

        try
        {
            _writeConflictRetry.Execute(() => SaveItemTuples(tuples), cancellationToken);
        }
        catch (DbUpdateException ex) when (skipFailures && tuples.Count > 1 && IsDataError(ex))
        {
            return SaveItemTuplesSkippingFailures(tuples, ex, cancellationToken);
        }

        return [];
    }

    private void SaveItemTuples(List<(BaseItemDto Item, List<Guid>? AncestorIds, BaseItemDto TopParent, IEnumerable<string> UserDataKey, List<string> InheritedTags)> tuples)
    {
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

        var itemValueMaps = tuples
            .Select(e => (e.Item, Values: GetItemValuesToSave(e.Item, e.InheritedTags)))
            .ToArray();
        var allListedItemValues = itemValueMaps
            .SelectMany(f => f.Values)
            .Distinct()
            .ToArray();

        var types = allListedItemValues.Select(e => e.MagicNumber).Distinct().ToArray();
        var values = allListedItemValues.Select(e => e.Value).Distinct().ToArray();
        var allListedItemValuesSet = allListedItemValues.ToHashSet();

        var existingValues = context.ItemValues
            .Where(e => types.Contains(e.Type) && values.Contains(e.Value))
            .AsEnumerable()
            .Where(e => allListedItemValuesSet.Contains((e.Type, e.Value)))
            .ToArray();
        var missingItemValues = allListedItemValues.Except(existingValues.Select(f => (MagicNumber: f.Type, f.Value))).Select(f => new ItemValue()
        {
            CleanValue = f.Value.GetCleanValue(),
            ItemValueId = Guid.NewGuid(),
            Type = f.MagicNumber,
            Value = f.Value
        }).ToArray();
        context.ItemValues.AddRange(missingItemValues);

        var itemValuesStore = existingValues
            .Concat(missingItemValues)
            .ToDictionary(e => (e.Type, e.Value));
        var valueMap = itemValueMaps
            .Select(f => (f.Item, Values: f.Values.Select(e => itemValuesStore[(e.MagicNumber, e.Value)]).DistinctBy(e => e.ItemValueId).ToArray()))
            .ToArray();

        var mappedValues = context.ItemValuesMap.WhereOneOrMany(ids, e => e.ItemId).ToList();

        foreach (var item in valueMap)
        {
            var itemMappedValues = mappedValues.Where(e => e.ItemId == item.Item.Id).ToList();
            foreach (var itemValue in item.Values)
            {
                var existingItem = itemMappedValues.FirstOrDefault(f => f.ItemValueId == itemValue.ItemValueId);
                if (existingItem is null)
                {
                    context.ItemValuesMap.Add(new ItemValueMap()
                    {
                        Item = null!,
                        ItemId = item.Item.Id,
                        ItemValue = null!,
                        ItemValueId = itemValue.ItemValueId
                    });
                }
                else
                {
                    itemMappedValues.Remove(existingItem);
                }
            }

            context.ItemValuesMap.RemoveRange(itemMappedValues);
        }

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

    /// <summary>
    /// Saves the items of a batch that failed on the data of its rows again, each with what depends on it in its own
    /// transaction, so that the rows of one item the engine refuses cost that item and not the whole batch.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The items are saved in units: an item together with the items of the batch that refer to it by
    /// <see cref="BaseItemDto.ParentId"/> or <see cref="BaseItemDto.OwnerId"/>, such as a video and the alternate
    /// versions that follow it. A unit is saved as the batch was, so that the links between its items are resolved
    /// against rows written in the same transaction. If a unit of several items fails, its items are saved one at a
    /// time, and an item whose parent or owner in the batch was not saved is skipped. An item saved that way before
    /// the items that belong to it cannot link to them yet, so once they are saved it is saved again, which links it
    /// to the ones that were. Units and items keep the order of the batch; the items of a unit are not sorted by
    /// what they refer to, as the callers already put an item before the items that belong to it.
    /// </para>
    /// <para>
    /// When the first <see cref="IdenticalFailuresBeforeGivingUp"/> items saved fail in the same way, the error is
    /// taken to concern the whole batch rather than single items, and the exception of the batch is thrown again. It
    /// is thrown again as well when no item could be saved at all. A failure that is not a data error, or a
    /// cancellation, ends the attempt at once with its own exception. Either way, the units saved before it stay
    /// saved, while the caller, which only learns of the exception, neither caches nor announces them; a later scan
    /// finds them in the database.
    /// </para>
    /// </remarks>
    /// <param name="tuples">The items of the batch, in its order.</param>
    /// <param name="batchException">The exception the batch failed with.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ids of the items that were not saved.</returns>
    private HashSet<Guid> SaveItemTuplesSkippingFailures(
        List<(BaseItemDto Item, List<Guid>? AncestorIds, BaseItemDto TopParent, IEnumerable<string> UserDataKey, List<string> InheritedTags)> tuples,
        DbUpdateException batchException,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Saving {Count} items together failed with {ExceptionType}, saving them one at a time",
            tuples.Count,
            GetInnermostDbException(batchException)?.GetType().FullName);

        var notSaved = new HashSet<Guid>();
        var savedCount = 0;
        var countedSaves = 0;
        string? firstFailure = null;
        var failuresAlike = true;

        foreach (var unitIndexes in GetSaveUnits(tuples.Select(t => t.Item).ToList()))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var unit = unitIndexes.Select(i => tuples[i]).ToList();

            // A unit that is the whole batch has already failed as such.
            if (unit.Count > 1 && unit.Count < tuples.Count && TrySave(unit, out _))
            {
                savedCount += unit.Count;
                CountSave(null);
                continue;
            }

            var savedInUnit = new List<(BaseItemDto Item, List<Guid>? AncestorIds, BaseItemDto TopParent, IEnumerable<string> UserDataKey, List<string> InheritedTags)>();
            foreach (var tuple in unit)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = tuple.Item;
                var dependency = GetBatchDependencies(item).FirstOrDefault(notSaved.Contains);
                if (!dependency.IsEmpty())
                {
                    notSaved.Add(item.Id);
                    _logger.LogError(
                        "Item {ItemId} ({ItemPath}) is not saved, because item {DependencyId} of the same batch, which it belongs to, could not be saved",
                        item.Id,
                        item.Path,
                        dependency);
                    continue;
                }

                if (TrySave([tuple], out var failure))
                {
                    savedCount++;
                    savedInUnit.Add(tuple);
                    CountSave(null);
                    continue;
                }

                notSaved.Add(item.Id);
                _logger.LogError(
                    failure,
                    "Item {ItemId} ({ItemPath}) could not be saved, and the other items of its batch are saved without it",
                    item.Id,
                    item.Path);
                CountSave(failure);
            }

            // A video saved before its alternate versions could not link to them, so it is saved again now that the
            // ones that could be saved exist.
            foreach (var tuple in savedInUnit.Where(t => savedInUnit.Any(other => GetBatchDependencies(other.Item).Contains(t.Item.Id))))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!TrySave([tuple], out var failure))
                {
                    _logger.LogWarning(
                        failure,
                        "Item {ItemId} ({ItemPath}) is saved, but saving it again to link it to the items of its batch that belong to it failed",
                        tuple.Item.Id,
                        tuple.Item.Path);
                }
            }
        }

        if (savedCount == 0)
        {
            _logger.LogError("None of the {Count} items of the batch could be saved", tuples.Count);
            ExceptionDispatchInfo.Throw(batchException);
        }

        return notSaved;

        bool TrySave(List<(BaseItemDto Item, List<Guid>? AncestorIds, BaseItemDto TopParent, IEnumerable<string> UserDataKey, List<string> InheritedTags)> unit, out DbUpdateException? failure)
        {
            try
            {
                _writeConflictRetry.Execute(() => SaveItemTuples(unit), cancellationToken);
                failure = null;
                return true;
            }
            catch (DbUpdateException ex) when (IsDataError(ex))
            {
                failure = ex;
                return false;
            }
        }

        void CountSave(DbUpdateException? failure)
        {
            if (++countedSaves > IdenticalFailuresBeforeGivingUp)
            {
                return;
            }

            if (failure is null)
            {
                failuresAlike = false;
                return;
            }

            // The failures are compared by the type of the exception and its SQLSTATE, not by the error codes of a
            // provider. A client that reports no SQLSTATE leaves the type alone, so there any failures of the first
            // items count as alike, which errs towards throwing the exception of the batch, as a failed batch did before.
            var inner = GetInnermostDbException(failure);
            var kind = inner?.GetType().FullName + "/" + inner?.SqlState;
            firstFailure ??= kind;
            failuresAlike &= string.Equals(kind, firstFailure, StringComparison.Ordinal);
            if (failuresAlike && countedSaves == IdenticalFailuresBeforeGivingUp)
            {
                _logger.LogError(
                    "The first {Count} items of the batch failed alike, so the error is taken to concern the whole batch",
                    countedSaves);
                ExceptionDispatchInfo.Throw(batchException);
            }
        }
    }

    /// <summary>
    /// Determines whether a failed save was refused for the data of its rows, as opposed to failing for the
    /// connection, the storage, a time-out or a cancellation, which no item can be saved through either.
    /// </summary>
    /// <param name="exception">The exception the save failed with.</param>
    /// <returns><c>true</c> if the engine refused the data of the rows; otherwise <c>false</c>.</returns>
    private bool IsDataError(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is OperationCanceledException or TimeoutException or IOException or SocketException)
            {
                return false;
            }
        }

        // A write conflict the retry policy gave up on stays the failure of the batch, as before: it says nothing about
        // the data of a single item, even where the provider also counts it a data error.
        return GetInnermostDbException(exception) is { IsTransient: false }
            && !_databaseProvider.IsRetryableWriteConflict(exception)
            && _databaseProvider.IsDataError(exception);
    }

    private static DbException? GetInnermostDbException(Exception exception)
    {
        DbException? innermost = null;
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbException dbException)
            {
                innermost = dbException;
            }
        }

        return innermost;
    }

    /// <summary>
    /// Splits a batch into the units that are saved together when the batch fails: each item with the items of the
    /// batch that it refers to, or that refer to it, by <see cref="BaseItemDto.ParentId"/> or
    /// <see cref="BaseItemDto.OwnerId"/>, in the order of the batch.
    /// </summary>
    /// <param name="items">The items of the batch, which have distinct ids.</param>
    /// <returns>
    /// The units as the indexes of their items in the batch, ordered by their first item, each in the order of the batch.
    /// </returns>
    internal static List<List<int>> GetSaveUnits(IReadOnlyList<BaseItemDto> items)
    {
        var indexById = new Dictionary<Guid, int>();
        for (var i = 0; i < items.Count; i++)
        {
            indexById[items[i].Id] = i;
        }

        // Each item points at the first item of its unit.
        var first = Enumerable.Range(0, items.Count).ToArray();
        int Find(int index)
        {
            while (first[index] != index)
            {
                first[index] = first[first[index]];
                index = first[index];
            }

            return index;
        }

        for (var i = 0; i < items.Count; i++)
        {
            foreach (var dependency in GetBatchDependencies(items[i]))
            {
                if (indexById.TryGetValue(dependency, out var j))
                {
                    var (a, b) = (Find(i), Find(j));
                    first[Math.Max(a, b)] = Math.Min(a, b);
                }
            }
        }

        var units = new SortedDictionary<int, List<int>>();
        for (var i = 0; i < items.Count; i++)
        {
            var key = Find(i);
            if (!units.TryGetValue(key, out var unit))
            {
                units[key] = unit = [];
            }

            unit.Add(i);
        }

        return [.. units.Values];
    }

    /// <summary>
    /// Gets the items an item refers to by a foreign key, which have to be saved before it.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>Its parent and its owner, where it has them.</returns>
    private static IEnumerable<Guid> GetBatchDependencies(BaseItemDto item)
    {
        if (!item.ParentId.IsEmpty())
        {
            yield return item.ParentId;
        }

        if (!item.OwnerId.IsEmpty())
        {
            yield return item.OwnerId;
        }
    }

    /// <summary>
    /// Removes NUL characters from the item itself, from every string that <see cref="BaseItemMapper"/> copies into a column.
    /// Saving cleans the mapped copy, but the item is what stays in memory, and what is later matched against
    /// the stored rows by its keys: a series finds its seasons and episodes by its presentation key, and the
    /// external ids are looked up as they are. If only the copy was cleaned, the item would no longer find the rows it saved.
    /// </summary>
    /// <param name="item">The item about to be mapped.</param>
    private static void RemoveNulCharacters(BaseItemDto item)
    {
        // The name and the forced sort name reset the sort name, so they are cleaned before it is read.
        if (HasNulCharacter(item.Name))
        {
            item.Name = item.Name.RemoveNullCharacters();
        }

        if (HasNulCharacter(item.ForcedSortName))
        {
            item.ForcedSortName = item.ForcedSortName.RemoveNullCharacters();
        }

        if (HasNulCharacter(item.SortName))
        {
            item.SortName = item.SortName.RemoveNullCharacters();
        }

        if (HasNulCharacter(item.Path))
        {
            item.Path = item.Path.RemoveNullCharacters();
        }

        // The setter looks the language up, so it is only called for a value that has to change.
        if (HasNulCharacter(item.OriginalLanguage))
        {
            item.OriginalLanguage = item.OriginalLanguage.RemoveNullCharacters();
        }

        item.CustomRating = item.CustomRating?.RemoveNullCharacters();
        item.OfficialRating = item.OfficialRating?.RemoveNullCharacters();
        item.Overview = item.Overview?.RemoveNullCharacters();
        item.PreferredMetadataLanguage = item.PreferredMetadataLanguage?.RemoveNullCharacters();
        item.PreferredMetadataCountryCode = item.PreferredMetadataCountryCode?.RemoveNullCharacters();
        item.PresentationUniqueKey = item.PresentationUniqueKey?.RemoveNullCharacters();
        item.OriginalTitle = item.OriginalTitle?.RemoveNullCharacters();
        item.Album = item.Album?.RemoveNullCharacters();
        item.ExternalSeriesId = item.ExternalSeriesId?.RemoveNullCharacters();
        item.Tagline = item.Tagline?.RemoveNullCharacters();
        item.ExternalId = item.ExternalId?.RemoveNullCharacters();

        // Stored in one column each, joined with a separator.
        if (HasNulCharacter(item.Genres))
        {
            item.Genres = RemoveNulCharacters(item.Genres);
        }

        if (HasNulCharacter(item.ProductionLocations))
        {
            item.ProductionLocations = RemoveNulCharacters(item.ProductionLocations);
        }

        if (HasNulCharacter(item.Studios))
        {
            item.Studios = RemoveNulCharacters(item.Studios);
        }

        if (HasNulCharacter(item.Tags))
        {
            item.Tags = RemoveNulCharacters(item.Tags);
        }

        if (item.ProviderIds is { } providerIds)
        {
            // Only the entries that change are touched, so the ids of an item other threads may be reading
            // are never cleared and refilled.
            foreach (var (key, value) in providerIds.ToList())
            {
                if (!HasNulCharacter(key) && !HasNulCharacter(value))
                {
                    continue;
                }

                var cleanKey = key.RemoveNullCharacters();
                var cleanValue = value?.RemoveNullCharacters();

                // An id that held nothing but NUL characters names nothing once they are gone, so it is dropped.
                var drop = cleanKey.Length == 0 || (HasNulCharacter(value) && cleanValue?.Length == 0);
                if (drop)
                {
                    providerIds.Remove(key);
                }
                else if (string.Equals(key, cleanKey, StringComparison.Ordinal))
                {
                    providerIds[key] = cleanValue!;
                }
                else
                {
                    // An id stored under the clean key already wins over one that only differs by a NUL.
                    providerIds.TryAdd(cleanKey, cleanValue!);
                    providerIds.Remove(key);
                }
            }
        }

        RemoveNulCharacters(item.ImageInfos);

        if (item is IHasProgramAttributes hasProgramAttributes)
        {
            hasProgramAttributes.EpisodeTitle = hasProgramAttributes.EpisodeTitle?.RemoveNullCharacters();
        }

        if (item is LiveTvChannel liveTvChannel)
        {
            liveTvChannel.ServiceName = liveTvChannel.ServiceName?.RemoveNullCharacters();
        }

        if (item is IHasSeries hasSeries)
        {
            hasSeries.SeriesName = hasSeries.SeriesName?.RemoveNullCharacters();
            hasSeries.SeriesPresentationUniqueKey = hasSeries.SeriesPresentationUniqueKey?.RemoveNullCharacters();
        }

        if (item is Episode episode)
        {
            episode.SeasonName = episode.SeasonName?.RemoveNullCharacters();
        }

        if (item is LiveTvProgram program)
        {
            program.ShowId = program.ShowId?.RemoveNullCharacters();
        }

        if (item is IHasArtist hasArtist && HasNulCharacter(hasArtist.Artists))
        {
            hasArtist.Artists = RemoveNulCharacters(hasArtist.Artists);
        }

        if (item is IHasAlbumArtist hasAlbumArtist && HasNulCharacter(hasAlbumArtist.AlbumArtists))
        {
            hasAlbumArtist.AlbumArtists = RemoveNulCharacters(hasAlbumArtist.AlbumArtists);
        }
    }

    // The name of a provider is part of the key of its row, so an id with a longer name cannot be stored. It is
    // removed from the item, so that it does not fail the save of the items saved with it.
    private void DropOverlongProviderIds(BaseItemDto item)
    {
        if (item.ProviderIds is not { } providerIds)
        {
            return;
        }

        foreach (var key in providerIds.Keys.Where(e => e.Length > KeyNormalizer.ProviderIdMaxLength).ToList())
        {
            _logger.LogWarning(
                "Dropped a provider id of item {ItemId} whose name is {Length} characters long, the maximum is {MaxLength}",
                item.Id,
                key.Length,
                KeyNormalizer.ProviderIdMaxLength);
            providerIds.Remove(key);
        }
    }

    private static void RemoveNulCharacters(ItemImageInfo[]? images)
    {
        if (images is null)
        {
            return;
        }

        foreach (var image in images)
        {
            if (HasNulCharacter(image.Path))
            {
                image.Path = image.Path.RemoveNullCharacters();
            }
        }
    }

    private static string[] RemoveNulCharacters(IEnumerable<string> values)
    {
        // An entry that held nothing but NUL characters names nothing once they are gone, so it is dropped.
        // Entries that become equal are kept once, as the first one: the columns de-duplicate them when saving.
        var cleaned = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (value is null)
            {
                continue;
            }

            var clean = value.RemoveNullCharacters();
            if ((!HasNulCharacter(value) || clean.Length > 0) && seen.Add(clean))
            {
                cleaned.Add(clean);
            }
        }

        return cleaned.ToArray();
    }

    private static bool HasNulCharacter(IEnumerable<string>? values)
    {
        return values is not null && values.Any(HasNulCharacter);
    }

    private static bool HasNulCharacter(string? value)
    {
        return value is not null && value.Contains('\0', StringComparison.Ordinal);
    }

    private static List<(ItemValueType MagicNumber, string Value)> GetItemValuesToSave(BaseItemDto item, List<string> inheritedTags)
    {
        var list = new List<(ItemValueType, string)>();

        if (item is IHasArtist hasArtist)
        {
            list.AddRange(hasArtist.Artists.Select(i => ((ItemValueType)0, i)));
        }

        if (item is IHasAlbumArtist hasAlbumArtist)
        {
            list.AddRange(hasAlbumArtist.AlbumArtists.Select(i => (ItemValueType.AlbumArtist, i)));
        }

        list.AddRange(item.Genres.Select(i => (ItemValueType.Genre, i)));
        list.AddRange(item.Studios.Select(i => (ItemValueType.Studios, i)));
        list.AddRange(item.Tags.Select(i => (ItemValueType.Tags, i)));

        list.AddRange(inheritedTags.Select(i => (ItemValueType.InheritedTags, i)));

        // Cleaned and cut before the values are de-duplicated and matched against stored rows, so that a value with a
        // NUL maps to the row stored without it, and values that are equal up to the length of the column are one
        // value. A row stored earlier may still hold a NUL, or be longer, and is not matched.
        return list
            .Where(i => !string.IsNullOrWhiteSpace(i.Item2))
            .Select(i => (i.Item1, KeyNormalizer.CutItemValue(i.Item2.RemoveNullCharacters())))
            .Where(i => !string.IsNullOrWhiteSpace(i.Item2))
            .ToList();
    }
}
