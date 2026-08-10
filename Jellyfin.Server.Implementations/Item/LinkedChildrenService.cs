#pragma warning disable RS0030 // Do not use banned APIs
#pragma warning disable CA1304 // Specify CultureInfo
#pragma warning disable CA1311 // Specify a culture or use an invariant version

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Persistence;
using Microsoft.EntityFrameworkCore;
using DbLinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;
using LinkedChildType = MediaBrowser.Controller.Entities.LinkedChildType;

namespace Jellyfin.Server.Implementations.Item;

/// <summary>
/// Provides linked children query and manipulation operations.
/// </summary>
public class LinkedChildrenService : ILinkedChildrenService
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IItemTypeLookup _itemTypeLookup;
    private readonly IItemQueryHelpers _queryHelpers;

    /// <summary>
    /// Initializes a new instance of the <see cref="LinkedChildrenService"/> class.
    /// </summary>
    /// <param name="dbProvider">The database context factory.</param>
    /// <param name="itemTypeLookup">The item type lookup.</param>
    /// <param name="queryHelpers">The shared query helpers.</param>
    public LinkedChildrenService(
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IItemTypeLookup itemTypeLookup,
        IItemQueryHelpers queryHelpers)
    {
        _dbProvider = dbProvider;
        _itemTypeLookup = itemTypeLookup;
        _queryHelpers = queryHelpers;
    }

    /// <inheritdoc/>
    public IReadOnlyList<Guid> GetLinkedChildrenIds(Guid parentId, int? childType = null)
    {
        using var dbContext = _dbProvider.CreateDbContext();

        var query = dbContext.LinkedChildren
            .Where(lc => lc.ParentId.Equals(parentId));

        if (childType.HasValue)
        {
            query = query.Where(lc => (int)lc.ChildType == childType.Value);
        }

        return query
            .OrderBy(lc => lc.SortOrder)
            .Select(lc => lc.ChildId)
            .ToArray();
    }

    /// <inheritdoc/>
    public IReadOnlyList<Guid> GetParentIdsWithChildType(LinkedChildType childType)
    {
        using var dbContext = _dbProvider.CreateDbContext();

        return dbContext.LinkedChildren
            .Where(lc => lc.ChildType == (DbLinkedChildType)childType)
            .Select(lc => lc.ParentId)
            .Distinct()
            .ToArray();
    }

    /// <inheritdoc/>
    public IReadOnlySet<Guid> GetItemIdsWithAlternateVersions(IReadOnlyList<Guid> itemIds)
    {
        if (itemIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        using var dbContext = _dbProvider.CreateDbContext();

        return dbContext.LinkedChildren
            .Where(lc => lc.ChildType == DbLinkedChildType.LocalAlternateVersion
                || lc.ChildType == DbLinkedChildType.LinkedAlternateVersion)
            .WhereOneOrMany(itemIds as IList<Guid> ?? itemIds.ToList(), lc => lc.ParentId)
            .Select(lc => lc.ParentId)
            .Distinct()
            .ToHashSet();
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, MusicArtist[]> FindArtists(IReadOnlyList<string> artistNames)
    {
        using var dbContext = _dbProvider.CreateDbContext();

        var lowerNames = artistNames.Select(n => n.ToLowerInvariant()).ToArray();
        var artists = dbContext.BaseItems
            .AsNoTracking()
            .Where(e => e.Type == _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist]!)
            .Where(e => lowerNames.Contains(e.Name!.ToLower()))
            .ToArray();

        var lookup = artists
            .GroupBy(e => e.Name!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => _queryHelpers.DeserializeBaseItem(f)).Where(dto => dto is not null).Cast<MusicArtist>().ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, MusicArtist[]>(artistNames.Count);
        foreach (var name in artistNames)
        {
            if (lookup.TryGetValue(name, out var artistArray))
            {
                result[name] = artistArray;
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public IReadOnlyList<Guid> GetManualLinkedParentIds(Guid childId, BaseItemKind? parentType = null)
    {
        using var context = _dbProvider.CreateDbContext();

        var query = context.LinkedChildren
            .Where(lc => lc.ChildId == childId && lc.ChildType == DbLinkedChildType.Manual);

        if (parentType.HasValue)
        {
            var parentTypeName = _itemTypeLookup.BaseItemKindNames[parentType.Value];
            query = query.Join(
                context.BaseItems
                    .Where(item => item.Type == parentTypeName),
                lc => lc.ParentId,
                item => item.Id,
                (lc, _) => lc);
        }

        return query.Select(lc => lc.ParentId).Distinct().ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<Guid> RerouteLinkedChildren(Guid fromChildId, Guid toChildId)
    {
        using var context = _dbProvider.CreateDbContext();

        var affectedParentIds = context.LinkedChildren
            .Where(lc => lc.ChildId == fromChildId && lc.ChildType == DbLinkedChildType.Manual)
            .Select(lc => lc.ParentId)
            .Distinct()
            .ToList();

        if (affectedParentIds.Count == 0)
        {
            return affectedParentIds;
        }

        var parentsWithTarget = context.LinkedChildren
            .Where(lc => lc.ChildId == toChildId && lc.ChildType == DbLinkedChildType.Manual)
            .Select(lc => lc.ParentId)
            .ToHashSet();

        context.LinkedChildren
            .Where(lc => lc.ChildId == fromChildId
                && lc.ChildType == DbLinkedChildType.Manual
                && !parentsWithTarget.Contains(lc.ParentId))
            .ExecuteUpdate(s => s.SetProperty(e => e.ChildId, toChildId));

        context.LinkedChildren
            .Where(lc => lc.ChildId == fromChildId
                && lc.ChildType == DbLinkedChildType.Manual
                && parentsWithTarget.Contains(lc.ParentId))
            .ExecuteDelete();

        return affectedParentIds;
    }

    /// <inheritdoc/>
    public void UpsertLinkedChild(Guid parentId, Guid childId, LinkedChildType childType)
    {
        using var context = _dbProvider.CreateDbContext();

        var dbChildType = (DbLinkedChildType)childType;
        var existingLink = context.LinkedChildren
            .FirstOrDefault(lc => lc.ParentId == parentId && lc.ChildId == childId);

        if (existingLink is null)
        {
            var nextSortOrder = (context.LinkedChildren
                .Where(lc => lc.ParentId == parentId)
                .Max(lc => (int?)lc.SortOrder) ?? -1) + 1;

            context.LinkedChildren.Add(new Jellyfin.Database.Implementations.Entities.LinkedChildEntity
            {
                ParentId = parentId,
                ChildId = childId,
                ChildType = dbChildType,
                SortOrder = nextSortOrder
            });
        }
        else
        {
            existingLink.ChildType = dbChildType;
        }

        context.SaveChanges();
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> GetAutoMergeExclusions()
    {
        using var context = _dbProvider.CreateDbContext();

        var pairs = context.LinkedChildren
            .Where(lc => lc.ChildType == DbLinkedChildType.ExcludedAlternateVersion)
            .Select(lc => new { lc.ParentId, lc.ChildId })
            .ToList();

        var exclusions = new Dictionary<Guid, List<Guid>>();
        foreach (var pair in pairs)
        {
            // Only one direction is stored; expose both so callers can look up either side.
            Add(pair.ParentId, pair.ChildId);
            Add(pair.ChildId, pair.ParentId);
        }

        return exclusions.ToDictionary(e => e.Key, e => (IReadOnlyList<Guid>)e.Value);

        void Add(Guid itemId, Guid excludedId)
        {
            if (!exclusions.TryGetValue(itemId, out var excluded))
            {
                excluded = [];
                exclusions[itemId] = excluded;
            }

            if (!excluded.Contains(excludedId))
            {
                excluded.Add(excludedId);
            }
        }
    }

    /// <inheritdoc/>
    public void AddAutoMergeExclusions(Guid itemId, IReadOnlyList<Guid> excludedItemIds)
    {
        ArgumentNullException.ThrowIfNull(excludedItemIds);

        if (excludedItemIds.Count == 0)
        {
            return;
        }

        using var context = _dbProvider.CreateDbContext();

        var recorded = context.LinkedChildren
            .Where(lc => lc.ChildType == DbLinkedChildType.ExcludedAlternateVersion
                && ((lc.ParentId == itemId && excludedItemIds.Contains(lc.ChildId))
                    || (lc.ChildId == itemId && excludedItemIds.Contains(lc.ParentId))))
            .Select(lc => lc.ParentId == itemId ? lc.ChildId : lc.ParentId)
            .ToHashSet();

        // Exclusions are not owned by the item's version links, which are rewritten from scratch on
        // every save starting at sort order 0, so they live in the negative sort order space.
        var nextSortOrder = Math.Min(
            context.LinkedChildren.Where(lc => lc.ParentId == itemId).Min(lc => (int?)lc.SortOrder) ?? 0,
            0) - 1;

        foreach (var excludedItemId in excludedItemIds)
        {
            if (excludedItemId.Equals(itemId) || !recorded.Add(excludedItemId))
            {
                continue;
            }

            context.LinkedChildren.Add(new Jellyfin.Database.Implementations.Entities.LinkedChildEntity
            {
                ParentId = itemId,
                ChildId = excludedItemId,
                ChildType = DbLinkedChildType.ExcludedAlternateVersion,
                SortOrder = nextSortOrder
            });

            nextSortOrder--;
        }

        context.SaveChanges();
    }

    /// <inheritdoc/>
    public void RemoveAutoMergeExclusions(IReadOnlyList<Guid> itemIds)
    {
        ArgumentNullException.ThrowIfNull(itemIds);

        if (itemIds.Count < 2)
        {
            return;
        }

        using var context = _dbProvider.CreateDbContext();

        context.LinkedChildren
            .Where(lc => lc.ChildType == DbLinkedChildType.ExcludedAlternateVersion
                && itemIds.Contains(lc.ParentId)
                && itemIds.Contains(lc.ChildId))
            .ExecuteDelete();
    }
}
