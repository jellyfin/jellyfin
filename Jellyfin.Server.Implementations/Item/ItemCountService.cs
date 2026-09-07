#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Item;

/// <summary>
/// Provides item counting and played-status query operations.
/// </summary>
public class ItemCountService : IItemCountService
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IItemTypeLookup _itemTypeLookup;
    private readonly IItemQueryHelpers _queryHelpers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemCountService"/> class.
    /// </summary>
    /// <param name="dbProvider">The database context factory.</param>
    /// <param name="itemTypeLookup">The item type lookup.</param>
    /// <param name="queryHelpers">The shared query helpers.</param>
    public ItemCountService(
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IItemTypeLookup itemTypeLookup,
        IItemQueryHelpers queryHelpers)
    {
        _dbProvider = dbProvider;
        _itemTypeLookup = itemTypeLookup;
        _queryHelpers = queryHelpers;
    }

    /// <inheritdoc/>
    public int GetCount(InternalItemsQuery filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        _queryHelpers.PrepareFilterQuery(filter);

        using var context = _dbProvider.CreateDbContext();
        var dbQuery = _queryHelpers.TranslateQuery(context.BaseItems.AsNoTracking(), context, filter);

        return dbQuery.Count();
    }

    /// <inheritdoc />
    public ItemCounts GetItemCounts(InternalItemsQuery filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        _queryHelpers.PrepareFilterQuery(filter);

        using var context = _dbProvider.CreateDbContext();
        var dbQuery = _queryHelpers.TranslateQuery(context.BaseItems.AsNoTracking(), context, filter);

        var counts = dbQuery
            .GroupBy(x => x.Type)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToArray();

        var lookup = _itemTypeLookup.BaseItemKindNames;
        var result = new ItemCounts
        {
            ItemCount = counts.Sum(c => c.Count)
        };
        foreach (var count in counts)
        {
            if (string.Equals(count.Key, lookup[BaseItemKind.MusicAlbum], StringComparison.Ordinal))
            {
                result.AlbumCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.MusicArtist], StringComparison.Ordinal))
            {
                result.ArtistCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.Episode], StringComparison.Ordinal))
            {
                result.EpisodeCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.Movie], StringComparison.Ordinal))
            {
                result.MovieCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.MusicVideo], StringComparison.Ordinal))
            {
                result.MusicVideoCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.LiveTvProgram], StringComparison.Ordinal))
            {
                result.ProgramCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.Series], StringComparison.Ordinal))
            {
                result.SeriesCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.Audio], StringComparison.Ordinal))
            {
                result.SongCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.Trailer], StringComparison.Ordinal))
            {
                result.TrailerCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.BoxSet], StringComparison.Ordinal))
            {
                result.BoxSetCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.Book], StringComparison.Ordinal))
            {
                result.BookCount = count.Count;
            }
        }

        return result;
    }

    /// <inheritdoc />
    public ItemCounts GetItemCountsForNameItem(BaseItemKind kind, Guid id, BaseItemKind[] relatedItemKinds, InternalItemsQuery accessFilter)
    {
        using var context = _dbProvider.CreateDbContext();

        var item = context.BaseItems.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new { e.Name, e.CleanName })
            .FirstOrDefault();

        if (item is null)
        {
            return new ItemCounts();
        }

        IQueryable<BaseItemEntity> baseQuery;
        switch (kind)
        {
            case BaseItemKind.Person:
                baseQuery = ItemsById(context, context.PeopleBaseItemMap
                    .AsNoTracking()
                    .Where(m => m.People.Name == item.Name)
                    .Select(m => m.ItemId));
                break;
            case BaseItemKind.MusicArtist:
                baseQuery = ItemsById(context, context.ItemValuesMap
                    .AsNoTracking()
                    .Where(ivm => ivm.ItemValue.CleanValue == item.CleanName
                        && (ivm.ItemValue.Type == ItemValueType.Artist || ivm.ItemValue.Type == ItemValueType.AlbumArtist))
                    .Select(ivm => ivm.ItemId));
                break;
            case BaseItemKind.Genre:
            case BaseItemKind.MusicGenre:
                baseQuery = ItemsById(context, context.ItemValuesMap
                    .AsNoTracking()
                    .Where(ivm => ivm.ItemValue.CleanValue == item.CleanName
                        && ivm.ItemValue.Type == ItemValueType.Genre)
                    .Select(ivm => ivm.ItemId));
                break;
            case BaseItemKind.Studio:
                baseQuery = ItemsById(context, context.ItemValuesMap
                    .AsNoTracking()
                    .Where(ivm => ivm.ItemValue.CleanValue == item.CleanName
                        && ivm.ItemValue.Type == ItemValueType.Studios)
                    .Select(ivm => ivm.ItemId));
                break;
            case BaseItemKind.Year:
                if (int.TryParse(item.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year))
                {
                    baseQuery = context.BaseItems
                        .AsNoTracking()
                        .Where(e => e.ProductionYear == year);
                }
                else
                {
                    return new ItemCounts();
                }

                break;
            default:
                return new ItemCounts();
        }

        var typeNames = relatedItemKinds.Select(k => _itemTypeLookup.BaseItemKindNames[k]).ToArray();
        baseQuery = baseQuery.Where(e => typeNames.Contains(e.Type));

        baseQuery = _queryHelpers.ApplyAccessFiltering(context, baseQuery, accessFilter);

        var counts = baseQuery
            .GroupBy(x => x.Type)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToArray();

        var lookup = _itemTypeLookup.BaseItemKindNames;
        var result = new ItemCounts();
        var totalCount = 0;

        foreach (var count in counts)
        {
            totalCount += count.Count;

            if (string.Equals(count.Key, lookup[BaseItemKind.MusicAlbum], StringComparison.Ordinal))
            {
                result.AlbumCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.MusicArtist], StringComparison.Ordinal))
            {
                result.ArtistCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.Episode], StringComparison.Ordinal))
            {
                result.EpisodeCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.Movie], StringComparison.Ordinal))
            {
                result.MovieCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.MusicVideo], StringComparison.Ordinal))
            {
                result.MusicVideoCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.LiveTvProgram], StringComparison.Ordinal))
            {
                result.ProgramCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.Series], StringComparison.Ordinal))
            {
                result.SeriesCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.Audio], StringComparison.Ordinal))
            {
                result.SongCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.Trailer], StringComparison.Ordinal))
            {
                result.TrailerCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.BoxSet], StringComparison.Ordinal))
            {
                result.BoxSetCount = count.Count;
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.Book], StringComparison.Ordinal))
            {
                result.BookCount = count.Count;
            }
        }

        if (kind is BaseItemKind.Studio or BaseItemKind.Genre or BaseItemKind.MusicGenre
            && relatedItemKinds.Contains(BaseItemKind.Episode)
            && relatedItemKinds.Contains(BaseItemKind.Series))
        {
            var rolledUpEpisodeCount = CountEpisodesOfTaggedSeries(context, baseQuery, accessFilter, out var directEpisodeCount);
            totalCount += rolledUpEpisodeCount - result.EpisodeCount + directEpisodeCount;
            result.EpisodeCount = rolledUpEpisodeCount + directEpisodeCount;
        }

        result.ItemCount = totalCount;

        return result;
    }

    private int CountEpisodesOfTaggedSeries(
        JellyfinDbContext context,
        IQueryable<BaseItemEntity> taggedItems,
        InternalItemsQuery accessFilter,
        out int unrelatedEpisodeCount)
    {
        var seriesTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Series];
        var episodeTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Episode];

        var taggedSeriesIds = taggedItems.Where(e => e.Type == seriesTypeName).Select(e => e.Id);
        unrelatedEpisodeCount = taggedItems.Count(e => e.Type == episodeTypeName
            && (e.SeriesId == null || !taggedSeriesIds.Contains(e.SeriesId.Value)));

        // Materialised so the episode count drives off IX_BaseItems_SeriesId.
        var seriesIds = taggedItems
            .Where(e => e.Type == seriesTypeName)
            .Select(e => e.Id)
            .ToArray();

        if (seriesIds.Length == 0)
        {
            return 0;
        }

        var episodes = context.BaseItems.AsNoTracking()
            .Where(e => e.Type == episodeTypeName && e.SeriesId != null)
            .WhereOneOrMany(seriesIds, e => e.SeriesId!.Value);

        return _queryHelpers.ApplyAccessFiltering(context, episodes, accessFilter).Count();
    }

    private static IQueryable<BaseItemEntity> ItemsById(JellyfinDbContext context, IQueryable<Guid> itemIds)
        => context.BaseItems.AsNoTracking().Where(e => itemIds.Contains(e.Id));

    /// <inheritdoc/>
    public int GetPlayedCount(InternalItemsQuery filter, Guid ancestorId)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(filter.User);
        using var dbContext = _dbProvider.CreateDbContext();

        var baseQuery = BuildGroupedDescendantsQuery(dbContext, filter, ancestorId);
        return baseQuery.Count(b => b.UserData!.Any(u => u.UserId == filter.User.Id && u.Played));
    }

    /// <inheritdoc/>
    public int GetTotalCount(InternalItemsQuery filter, Guid ancestorId)
    {
        ArgumentNullException.ThrowIfNull(filter);
        using var dbContext = _dbProvider.CreateDbContext();

        var baseQuery = BuildGroupedDescendantsQuery(dbContext, filter, ancestorId);
        return baseQuery.Count();
    }

    /// <inheritdoc/>
    public (int Played, int Total) GetPlayedAndTotalCount(InternalItemsQuery filter, Guid ancestorId)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(filter.User);
        using var dbContext = _dbProvider.CreateDbContext();

        var baseQuery = BuildGroupedDescendantsQuery(dbContext, filter, ancestorId);
        return GetPlayedAndTotalCountFromQuery(baseQuery, filter.User.Id);
    }

    private IQueryable<BaseItemEntity> BuildGroupedDescendantsQuery(JellyfinDbContext dbContext, InternalItemsQuery filter, Guid ancestorId)
    {
        var ancestorIds = GetPresentationKeyGroups(dbContext, [ancestorId])[ancestorId];
        var descendantIds = DescendantQueryHelper.GetAllDescendantIdsBatch(dbContext, ancestorIds).ToArray();

        var baseQuery = dbContext.BaseItems
            .AsNoTracking()
            .WhereOneOrMany(descendantIds, b => b.Id)
            .Where(DescendantQueryHelper.IsCountableLeaf);

        return _queryHelpers.ApplyAccessFiltering(dbContext, baseQuery, filter);
    }

    /// <inheritdoc/>
    public (int Played, int Total) GetPlayedAndTotalCountFromLinkedChildren(InternalItemsQuery filter, Guid parentId)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(filter.User);
        using var dbContext = _dbProvider.CreateDbContext();

        var allDescendantIds = DescendantQueryHelper.GetAllDescendantIdsBatch(dbContext, [parentId]).ToArray();
        var baseQuery = dbContext.BaseItems
            .WhereOneOrMany(allDescendantIds, b => b.Id)
            .Where(DescendantQueryHelper.IsCountableLeaf);
        baseQuery = _queryHelpers.ApplyAccessFiltering(dbContext, baseQuery, filter);

        return GetPlayedAndTotalCountFromQuery(baseQuery, filter.User.Id);
    }

    /// <inheritdoc/>
    public Dictionary<Guid, int> GetChildCountBatch(IReadOnlyList<Guid> parentIds, User? user)
    {
        ArgumentNullException.ThrowIfNull(parentIds);

        if (parentIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        using var dbContext = _dbProvider.CreateDbContext();

        var parentIdsArray = parentIds.ToArray();

        var includeVirtual = user is null || user.DisplayMissingEpisodes;

        var hierarchicalCounts = dbContext.BaseItems
            .Where(b => b.ParentId.HasValue && !b.SeasonId.HasValue && (includeVirtual || !b.IsVirtualItem))
            .WhereOneOrMany(parentIdsArray, b => b.ParentId!.Value)
            .GroupBy(b => b.ParentId!.Value)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.ParentId, x => x.Count);

        // An episode is a child of its season even when it is not stored under one: with a flat
        // structure ParentId points at the series, so counting by ParentId alone leaves the season
        // empty and counts its episodes towards the series instead.
        var seasonCounts = dbContext.BaseItems
            .Where(b => b.SeasonId.HasValue && (includeVirtual || !b.IsVirtualItem))
            .WhereOneOrMany(parentIdsArray, b => b.SeasonId!.Value)
            .GroupBy(b => b.SeasonId!.Value)
            .Select(g => new { SeasonId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.SeasonId, x => x.Count);

        var linkedCounts = dbContext.LinkedChildren
            .WhereOneOrMany(parentIdsArray, lc => lc.ParentId)
            .GroupBy(lc => lc.ParentId)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.ParentId, x => x.Count);

        var mergedChildCounts = GetMergedChildCounts(dbContext, parentIdsArray, includeVirtual);

        var result = new Dictionary<Guid, int>();
        foreach (var parentId in parentIds)
        {
            if (mergedChildCounts.TryGetValue(parentId, out var mergedCount))
            {
                result[parentId] = mergedCount;
                continue;
            }

            var hierarchicalCount = hierarchicalCounts.GetValueOrDefault(parentId, 0)
                + seasonCounts.GetValueOrDefault(parentId, 0);
            var linkedCount = linkedCounts.GetValueOrDefault(parentId, 0);

            result[parentId] = linkedCount > 0 ? linkedCount : hierarchicalCount;
        }

        return result;
    }

    private static Dictionary<Guid, int> GetMergedChildCounts(JellyfinDbContext dbContext, IReadOnlyList<Guid> parentIds, bool includeVirtual)
    {
        var mergedGroups = GetPresentationKeyGroups(dbContext, parentIds)
            .Where(group => group.Value.Count > 1)
            .ToArray();

        if (mergedGroups.Length == 0)
        {
            return [];
        }

        // Only merged folders.
        var memberIds = mergedGroups.SelectMany(group => group.Value).Distinct().ToArray();
        var children = dbContext.BaseItems
            .AsNoTracking()
            .Where(b => b.ParentId.HasValue && !b.SeasonId.HasValue && (includeVirtual || !b.IsVirtualItem))
            .WhereOneOrMany(memberIds, b => b.ParentId!.Value)
            .Select(b => new { ParentId = b.ParentId!.Value, b.Id, b.PresentationUniqueKey })
            .ToArray()
            .Concat(dbContext.BaseItems
                .AsNoTracking()
                .Where(b => b.SeasonId.HasValue && (includeVirtual || !b.IsVirtualItem))
                .WhereOneOrMany(memberIds, b => b.SeasonId!.Value)
                .Select(b => new { ParentId = b.SeasonId!.Value, b.Id, b.PresentationUniqueKey })
                .ToArray())
            .GroupBy(b => b.ParentId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(b => string.IsNullOrEmpty(b.PresentationUniqueKey)
                    ? b.Id.ToString("N", CultureInfo.InvariantCulture)
                    : b.PresentationUniqueKey).ToArray());

        var result = new Dictionary<Guid, int>();
        foreach (var (parentId, members) in mergedGroups)
        {
            var childKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var member in members)
            {
                if (children.TryGetValue(member, out var keys))
                {
                    childKeys.UnionWith(keys);
                }
            }

            result[parentId] = childKeys.Count;
        }

        return result;
    }

    /// <inheritdoc/>
    public Dictionary<Guid, (int Played, int Total)> GetPlayedAndTotalCountBatch(IReadOnlyList<Guid> folderIds, User user)
    {
        ArgumentNullException.ThrowIfNull(folderIds);
        ArgumentNullException.ThrowIfNull(user);

        if (folderIds.Count == 0)
        {
            return new Dictionary<Guid, (int Played, int Total)>();
        }

        using var dbContext = _dbProvider.CreateDbContext();
        var filter = new InternalItemsQuery(user);
        var userId = user.Id;

        // Merged series and seasons are stored as one row per folder-item sharing a presentation key.
        var groups = GetPresentationKeyGroups(dbContext, folderIds);
        var folderIdsArray = groups.Values.SelectMany(members => members).Distinct().ToArray();

        var leafItems = dbContext.BaseItems
            .Where(DescendantQueryHelper.IsCountableLeaf);
        leafItems = _queryHelpers.ApplyAccessFiltering(dbContext, leafItems, filter);

        var playedLeafItems = leafItems
            .Select(b => new { b.Id, Played = b.UserData!.Any(ud => ud.UserId == userId && ud.Played) });

        var ancestorLeaves = dbContext.AncestorIds
            .WhereOneOrMany(folderIdsArray, a => a.ParentItemId)
            .Join(
                playedLeafItems,
                a => a.ItemId,
                b => b.Id,
                (a, b) => new { FolderId = a.ParentItemId, b.Id, b.Played });

        var linkedLeaves = dbContext.LinkedChildren
            .WhereOneOrMany(folderIdsArray, lc => lc.ParentId)
            .Join(
                playedLeafItems,
                lc => lc.ChildId,
                b => b.Id,
                (lc, b) => new { FolderId = lc.ParentId, b.Id, b.Played });

        var linkedFolderLeaves = dbContext.LinkedChildren
            .WhereOneOrMany(folderIdsArray, lc => lc.ParentId)
            .Join(
                dbContext.BaseItems.Where(b => b.IsFolder),
                lc => lc.ChildId,
                b => b.Id,
                (lc, b) => new { lc.ParentId, FolderChildId = b.Id })
            .Join(
                dbContext.AncestorIds,
                x => x.FolderChildId,
                a => a.ParentItemId,
                (x, a) => new { x.ParentId, DescendantId = a.ItemId })
            .Join(
                playedLeafItems,
                x => x.DescendantId,
                b => b.Id,
                (x, b) => new { FolderId = x.ParentId, b.Id, b.Played });

        var countsByFolder = ancestorLeaves
            .Union(linkedLeaves)
            .Union(linkedFolderLeaves)
            .GroupBy(x => x.FolderId)
            .Select(g => new
            {
                FolderId = g.Key,
                Total = g.Select(x => x.Id).Distinct().Count(),
                Played = g.Where(x => x.Played).Select(x => x.Id).Distinct().Count()
            })
            .ToDictionary(x => x.FolderId, x => (x.Played, x.Total));

        var results = new Dictionary<Guid, (int Played, int Total)>();
        foreach (var (folderId, members) in groups)
        {
            var played = 0;
            var total = 0;

            // Members of a group are distinct folders, so their leaves cannot overlap.
            foreach (var member in members)
            {
                if (countsByFolder.TryGetValue(member, out var counts))
                {
                    played += counts.Played;
                    total += counts.Total;
                }
            }

            if (total > 0 || played > 0)
            {
                results[folderId] = (played, total);
            }
        }

        return results;
    }

    private static Dictionary<Guid, List<Guid>> GetPresentationKeyGroups(JellyfinDbContext dbContext, IReadOnlyList<Guid> folderIds)
    {
        var requested = dbContext.BaseItems
            .AsNoTracking()
            .WhereOneOrMany(folderIds, e => e.Id)
            .Select(e => new { e.Id, e.PresentationUniqueKey })
            .ToArray();

        var keys = requested
            .Select(e => e.PresentationUniqueKey)
            .Where(key => !string.IsNullOrEmpty(key))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Every item that is not merged carries a key derived from its own id, so in the common case
        // each group resolves back to the single folder that was asked for.
        var membersByKey = keys.Length == 0
            ? []
            : dbContext.BaseItems
                .AsNoTracking()
                .Where(e => e.IsFolder)
                .WhereOneOrMany(keys, e => e.PresentationUniqueKey!)
                .Select(e => new { e.Id, Key = e.PresentationUniqueKey! })
                .ToArray()
                .GroupBy(e => e.Key, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Id).ToList(), StringComparer.Ordinal);

        var keyById = requested.ToDictionary(e => e.Id, e => e.PresentationUniqueKey);
        var groups = new Dictionary<Guid, List<Guid>>();
        foreach (var folderId in folderIds)
        {
            groups[folderId] = keyById.TryGetValue(folderId, out var key)
                && !string.IsNullOrEmpty(key)
                && membersByKey.TryGetValue(key, out var members)
                && members.Count > 0
                    ? members
                    : [folderId];
        }

        return groups;
    }

    private static (int Played, int Total) GetPlayedAndTotalCountFromQuery(IQueryable<BaseItemEntity> query, Guid userId)
    {
        var result = query
            .Select(b => b.UserData!.Any(u => u.UserId == userId && u.Played))
            .GroupBy(_ => 1)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Total = g.Count(),
                Played = g.Count(isPlayed => isPlayed)
            })
            .FirstOrDefault();

        return result is null ? (0, 0) : (result.Played, result.Total);
    }
}
