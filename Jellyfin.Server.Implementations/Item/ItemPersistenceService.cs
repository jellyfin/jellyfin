#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
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

        var extraIds = context.BaseItems
            .Where(e => e.OwnerId.HasValue && descendantIds.Contains(e.OwnerId.Value))
            .Select(e => e.Id)
            .ToArray();

        foreach (var extraId in extraIds)
        {
            descendantIds.Add(extraId);
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
        context.ItemValues.Where(e => e.BaseItemsMap!.Count == 0).ExecuteDelete();
        context.ItemValuesMap.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.LinkedChildren.WhereOneOrMany(relatedItems, e => e.ParentId).ExecuteDelete();
        context.LinkedChildren.WhereOneOrMany(relatedItems, e => e.ChildId).ExecuteDelete();
        context.BaseItems.WhereOneOrMany(relatedItems, e => e.Id).ExecuteDelete();
        context.KeyframeData.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.MediaSegments.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.MediaStreamInfos.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        var query = context.PeopleBaseItemMap.WhereOneOrMany(relatedItems, e => e.ItemId).Select(f => f.PeopleId).Distinct().ToArray();
        context.PeopleBaseItemMap.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
        context.Peoples.WhereOneOrMany(query, e => e.Id).Where(e => e.BaseItems!.Count == 0).ExecuteDelete();
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
            if (!await context.BaseItems
                .AnyAsync(bi => bi.Id == item.Id, cancellationToken)
                .ConfigureAwait(false))
            {
                _logger.LogWarning("Unable to save ImageInfo for non existing BaseItem");
                return;
            }

            await context.BaseItemImageInfos
                .Where(e => e.ItemId == item.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            await context.BaseItemImageInfos
                .AddRangeAsync(images, cancellationToken)
                .ConfigureAwait(false);

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
                var userKeys = item.GetUserDataKeys().ToArray();
                var retentionDate = (DateTime?)null;

                await dbContext.UserData
                    .Where(e => e.ItemId == BaseItemRepository.PlaceholderId)
                    .Where(e => userKeys.Contains(e.CustomDataKey))
                    .ExecuteUpdateAsync(
                        e => e
                            .SetProperty(f => f.ItemId, item.Id)
                            .SetProperty(f => f.RetentionDate, retentionDate),
                        cancellationToken).ConfigureAwait(false);

                item.UserData = await dbContext.UserData
                    .AsNoTracking()
                    .Where(e => e.ItemId == item.Id)
                    .ToArrayAsync(cancellationToken)
                    .ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private void UpdateOrInsertItems(IReadOnlyList<BaseItemDto> items, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        cancellationToken.ThrowIfCancellationRequested();

        var tuples = new List<(BaseItemDto Item, List<Guid>? AncestorIds, BaseItemDto TopParent, IEnumerable<string> UserDataKey, List<string> InheritedTags)>();
        foreach (var item in items.GroupBy(e => e.Id).Select(e => e.Last()).Where(e => e.Id != BaseItemRepository.PlaceholderId))
        {
            var ancestorIds = item.SupportsAncestors ?
                item.GetAncestorIds().Distinct().ToList() :
                null;

            var topParent = item.GetTopParent();

            var userdataKey = item.GetUserDataKeys();
            var inheritedTags = item.GetInheritedTags();

            tuples.Add((item, ancestorIds, topParent, userdataKey, inheritedTags));
        }

        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();

        var ids = tuples.Select(f => f.Item.Id).ToArray();
        var existingItems = context.BaseItems.Where(e => ids.Contains(e.Id)).Select(f => f.Id).ToArray();

        foreach (var item in tuples)
        {
            var entity = BaseItemMapper.Map(item.Item, _appHost);
            entity.TopParentId = item.TopParent?.Id;

            if (!existingItems.Any(e => e == entity.Id))
            {
                context.BaseItems.Add(entity);
            }
            else
            {
                context.BaseItemProviders.Where(e => e.ItemId == entity.Id).ExecuteDelete();
                context.BaseItemImageInfos.Where(e => e.ItemId == entity.Id).ExecuteDelete();
                context.BaseItemMetadataFields.Where(e => e.ItemId == entity.Id).ExecuteDelete();

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
        var existingValues = context.ItemValues
            .Select(e => new
            {
                item = e,
                Key = e.Type + "+" + e.Value
            })
            .Where(f => allListedItemValues.Select(e => $"{(int)e.MagicNumber}+{e.Value}").Contains(f.Key))
            .Select(e => e.item)
            .ToArray();
        var missingItemValues = allListedItemValues.Except(existingValues.Select(f => (MagicNumber: f.Type, f.Value))).Select(f => new ItemValue()
        {
            CleanValue = f.Value.GetCleanValue(),
            ItemValueId = Guid.NewGuid(),
            Type = f.MagicNumber,
            Value = f.Value
        }).ToArray();
        context.ItemValues.AddRange(missingItemValues);

        var itemValuesStore = existingValues.Concat(missingItemValues).ToArray();
        var valueMap = itemValueMaps
            .Select(f => (f.Item, Values: f.Values.Select(e => itemValuesStore.First(g => g.Value == e.Value && g.Type == e.MagicNumber)).DistinctBy(e => e.ItemValueId).ToArray()))
            .ToArray();

        var mappedValues = context.ItemValuesMap.Where(e => ids.Contains(e.ItemId)).ToList();

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
            if (item.Item is Folder folder)
            {
                var existingLinkedChildren = allLinkedChildrenByParent.GetValueOrDefault(item.Item.Id)?.ToList() ?? new List<LinkedChildEntity>();
                if (folder.LinkedChildren.Length > 0)
                {
#pragma warning disable CS0618 // Type or member is obsolete - legacy path resolution for old data
                    var pathsToResolve = folder.LinkedChildren
                        .Where(lc => (!lc.ItemId.HasValue || lc.ItemId.Value.IsEmpty()) && !string.IsNullOrEmpty(lc.Path))
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
                        if (!childItemId.HasValue || childItemId.Value.IsEmpty())
                        {
                            if (!string.IsNullOrEmpty(linkedChild.Path) && pathToIdMap.TryGetValue(linkedChild.Path, out var resolvedId))
                            {
                                childItemId = resolvedId;
                            }
                        }
#pragma warning restore CS0618

                        if (childItemId.HasValue && !childItemId.Value.IsEmpty())
                        {
                            resolvedChildren.Add((linkedChild, childItemId.Value));
                        }
                    }

                    resolvedChildren = resolvedChildren
                        .GroupBy(c => c.ChildId)
                        .Select(g => g.Last())
                        .ToList();

                    var childIdsToCheck = resolvedChildren.Select(c => c.ChildId).ToList();
                    var existingChildIds = childIdsToCheck.Count > 0
                        ? context.BaseItems
                            .Where(e => childIdsToCheck.Contains(e.Id))
                            .Select(e => e.Id)
                            .ToHashSet()
                        : [];

                    var isPlaylist = folder is Playlist;
                    var sortOrder = 0;
                    foreach (var (linkedChild, childId) in resolvedChildren)
                    {
                        if (!existingChildIds.Contains(childId))
                        {
                            _logger.LogWarning(
                                "Skipping LinkedChild for parent {ParentName} ({ParentId}): child item {ChildId} does not exist in database",
                                item.Item.Name,
                                item.Item.Id,
                                childId);
                            continue;
                        }

                        var existingLink = existingLinkedChildren.FirstOrDefault(e => e.ChildId == childId);
                        if (existingLink is null)
                        {
                            context.LinkedChildren.Add(new LinkedChildEntity()
                            {
                                ParentId = item.Item.Id,
                                ChildId = childId,
                                ChildType = (DbLinkedChildType)linkedChild.Type,
                                SortOrder = isPlaylist ? sortOrder : null
                            });
                        }
                        else
                        {
                            existingLink.SortOrder = isPlaylist ? sortOrder : null;
                            existingLink.ChildType = (DbLinkedChildType)linkedChild.Type;
                            existingLinkedChildren.Remove(existingLink);
                        }

                        sortOrder++;
                    }
                }

                if (existingLinkedChildren.Count > 0)
                {
                    context.LinkedChildren.RemoveRange(existingLinkedChildren);
                }
            }

            if (item.Item is Video video)
            {
                var existingLinkedChildren = (allLinkedChildrenByParent.GetValueOrDefault(video.Id) ?? new List<LinkedChildEntity>())
                    .Where(e => (int)e.ChildType == 2 || (int)e.ChildType == 3)
                    .ToList();

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

                newLinkedChildren = newLinkedChildren
                    .GroupBy(c => c.ChildId)
                    .Select(g => g.Last())
                    .ToList();

                var childIdsToCheck = newLinkedChildren.Select(c => c.ChildId).ToList();
                var existingChildIds = childIdsToCheck.Count > 0
                    ? context.BaseItems
                        .Where(e => childIdsToCheck.Contains(e.Id))
                        .Select(e => e.Id)
                        .ToHashSet()
                    : [];

                int sortOrder = 0;
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

                    var existingLink = existingLinkedChildren.FirstOrDefault(e => e.ChildId == childId);
                    if (existingLink is null)
                    {
                        context.LinkedChildren.Add(new LinkedChildEntity
                        {
                            ParentId = video.Id,
                            ChildId = childId,
                            ChildType = (DbLinkedChildType)childType,
                            SortOrder = sortOrder
                        });
                    }
                    else
                    {
                        existingLink.ChildType = (DbLinkedChildType)childType;
                        existingLink.SortOrder = sortOrder;
                        existingLinkedChildren.Remove(existingLink);
                    }

                    sortOrder++;
                }

                if (existingLinkedChildren.Count > 0)
                {
                    var orphanedLocalVersionIds = existingLinkedChildren
                        .Where(e => e.ChildType == DbLinkedChildType.LocalAlternateVersion)
                        .Select(e => e.ChildId)
                        .ToList();

                    context.LinkedChildren.RemoveRange(existingLinkedChildren);

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

        list.RemoveAll(i => string.IsNullOrWhiteSpace(i.Item2));

        return list;
    }
}
