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
        context.ItemValues.Where(e => e.BaseItemsMap!.Count == 0).ExecuteDelete();
        context.ItemValuesMap.WhereOneOrMany(relatedItems, e => e.ItemId).ExecuteDelete();
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
    public async Task UpsertProviderIdAsync(Guid itemId, string name, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(value);

        var context = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            // (ItemId, ProviderId) is the primary key, so the row either exists or it does not.
            var existing = await context.BaseItemProviders
                .FirstOrDefaultAsync(e => e.ItemId == itemId && e.ProviderId == name, cancellationToken)
                .ConfigureAwait(false);

            if (existing is null)
            {
                await context.BaseItemProviders.AddAsync(
                    new BaseItemProvider
                    {
                        ItemId = itemId,
                        ProviderId = name,
                        ProviderValue = value,
                        Item = null!
                    },
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                existing.ProviderValue = value;
            }

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task RemoveProviderIdAsync(Guid itemId, string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var context = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            await context.BaseItemProviders
                .Where(e => e.ItemId == itemId && e.ProviderId == name)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task UpsertImageAsync(Guid itemId, ItemImageInfo image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);

        var context = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var existing = await FindImageAsync(context, itemId, image, cancellationToken).ConfigureAwait(false);
            var entity = BaseItemMapper.MapImageToEntity(itemId, image);

            if (existing is null)
            {
                await context.BaseItemImageInfos.AddAsync(entity, cancellationToken).ConfigureAwait(false);
                image.Id = entity.Id;
            }
            else
            {
                existing.Path = entity.Path;
                existing.ImageType = entity.ImageType;
                existing.Blurhash = entity.Blurhash;
                existing.DateModified = entity.DateModified;
                existing.Width = entity.Width;
                existing.Height = entity.Height;
                image.Id = existing.Id;
            }

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task RemoveImageAsync(Guid itemId, ItemImageInfo image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);

        var context = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var existing = await FindImageAsync(context, itemId, image, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                return;
            }

            context.BaseItemImageInfos.Remove(existing);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Finds the row an image stands for: by its own id where it has one, otherwise by the type and
    /// path that identify it before it has been stored.
    /// </summary>
    private async Task<BaseItemImageInfo?> FindImageAsync(
        JellyfinDbContext context,
        Guid itemId,
        ItemImageInfo image,
        CancellationToken cancellationToken)
    {
        if (!image.Id.IsEmpty())
        {
            return await context.BaseItemImageInfos
                .FirstOrDefaultAsync(e => e.ItemId == itemId && e.Id == image.Id, cancellationToken)
                .ConfigureAwait(false);
        }

        var path = _appHost.ReverseVirtualPath(image.Path);
        var imageType = (ImageInfoImageType)image.Type;
        return await context.BaseItemImageInfos
            .FirstOrDefaultAsync(
                e => e.ItemId == itemId && e.ImageType == imageType && e.Path == path,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveImagesAsync(BaseItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        // This replaces the stored images with what the item holds, which is only the whole set if
        // the item was read with its images. A scan reaches here through ValidateChildren, where a
        // child may have been read without them.
        if (!item.OwnedRowsRead.HasFlag(OwnedItemRows.Images))
        {
            if (item.ImageInfos.Length > 0)
            {
                _logger.LogWarning(
                    "Not writing images for {ItemName} ({ItemId}): the item was read without them, so what it holds is a partial set",
                    item.Name,
                    item.Id);
            }

            return;
        }

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

    /// <summary>
    /// Picks the stored items whose owned rows this save may rewrite: only the ones that read the
    /// collection, because only they hold the complete set.
    /// </summary>
    /// <remarks>
    /// An item that has something in a collection it never read is a caller changing one entry of a
    /// set it does not have. Rewriting from that would keep the change and drop everything else, so
    /// the write is refused and reported: the targeted writers - UpsertProviderIdAsync, UpsertImageAsync,
    /// UpsertLinkedChild - are how a partial change is meant to be persisted.
    /// </remarks>
    private Guid[] ItemsOwning(
        List<(BaseItemDto Item, List<Guid>? AncestorIds, BaseItemDto TopParent, IEnumerable<string> UserDataKey, List<string> InheritedTags)> tuples,
        HashSet<Guid> existingItems,
        OwnedItemRows rows,
        Func<BaseItemDto, bool> hasContent)
    {
        var owners = new List<Guid>(tuples.Count);
        foreach (var (item, _, _, _, _) in tuples)
        {
            if (!existingItems.Contains(item.Id))
            {
                continue;
            }

            if (item.OwnedRowsRead.HasFlag(rows))
            {
                owners.Add(item.Id);
            }
            else if (hasContent(item))
            {
                _logger.LogWarning(
                    "Not writing {Rows} for {ItemName} ({ItemId}): the item was read without them, so what it holds is a partial set. Use the targeted writer instead.",
                    rows,
                    item.Name,
                    item.Id);
            }
        }

        return [.. owners];
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
                // Only the collections this save is allowed to rewrite are re-added; inserting rows
                // for a collection whose delete was refused would collide with what is still stored.
                if (entity.Images is { Count: > 0 } && item.Item.OwnedRowsRead.HasFlag(OwnedItemRows.Images))
                {
                    context.BaseItemImageInfos.AddRange(entity.Images);
                }

                if (entity.LockedFields is { Count: > 0 } && item.Item.OwnedRowsRead.HasFlag(OwnedItemRows.LockedFields))
                {
                    context.BaseItemMetadataFields.AddRange(entity.LockedFields);
                }

                if (entity.Provider is { Count: > 0 } && !item.Item.OwnedRowsRead.HasFlag(OwnedItemRows.Providers))
                {
                    entity.Provider = [];
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
        // Only for the items that actually carry the collection, though: one read without it holds
        // an empty collection that means "not read", and clearing on that would delete the lot.
        if (existingItems.Count > 0)
        {
            var providerIds = ItemsOwning(tuples, existingItems, OwnedItemRows.Providers, e => e.ProviderIds.Count > 0);
            if (providerIds.Length > 0)
            {
                context.BaseItemProviders.WhereOneOrMany(providerIds, e => e.ItemId).ExecuteDelete();
            }

            var imageIds = ItemsOwning(tuples, existingItems, OwnedItemRows.Images, e => e.ImageInfos.Length > 0);
            if (imageIds.Length > 0)
            {
                context.BaseItemImageInfos.WhereOneOrMany(imageIds, e => e.ItemId).ExecuteDelete();
            }

            var lockedFieldIds = ItemsOwning(tuples, existingItems, OwnedItemRows.LockedFields, e => e.LockedFields.Length > 0);
            if (lockedFieldIds.Length > 0)
            {
                context.BaseItemMetadataFields.WhereOneOrMany(lockedFieldIds, e => e.ItemId).ExecuteDelete();
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
