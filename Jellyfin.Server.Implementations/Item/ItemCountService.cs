#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
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
            else if (string.Equals(count.Key, lookup[BaseItemKind.AudioBook], StringComparison.Ordinal))
            {
                result.AudioBookCount = count.Count;
            }
        }

        return result;
    }

    /// <inheritdoc />
    public ItemCounts GetItemCountsForNameItem(BaseItemKind kind, Guid id, BaseItemKind[] relatedItemKinds, InternalItemsQuery accessFilter)
    {
        return GetItemCountsForNameItems(kind, [id], relatedItemKinds, accessFilter)[id];
    }

    private static ItemValueType[] GetItemValueTypes(BaseItemKind kind)
        => kind switch
        {
            BaseItemKind.MusicArtist => [ItemValueType.Artist, ItemValueType.AlbumArtist],
            BaseItemKind.Genre or BaseItemKind.MusicGenre => [ItemValueType.Genre],
            BaseItemKind.Studio => [ItemValueType.Studios],
            _ => []
        };

    /// <inheritdoc />
    public Dictionary<Guid, ItemCounts> GetItemCountsForNameItems(BaseItemKind kind, IReadOnlyList<Guid> ids, BaseItemKind[] relatedItemKinds, InternalItemsQuery accessFilter)
    {
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentNullException.ThrowIfNull(relatedItemKinds);
        ArgumentNullException.ThrowIfNull(accessFilter);

        var result = new Dictionary<Guid, ItemCounts>();
        if (ids.Count == 0)
        {
            return result;
        }

        using var context = _dbProvider.CreateDbContext();

        var idsArray = ids as Guid[] ?? ids.ToArray();
        var nameItems = context.BaseItems.AsNoTracking()
            .WhereOneOrMany(idsArray, e => e.Id)
            .Select(e => new NameItem(e.Id, e.Name, e.CleanName))
            .ToArray();

        foreach (var id in ids)
        {
            result[id] = new ItemCounts();
        }

        if (nameItems.Length == 0)
        {
            return result;
        }

        var typeNames = relatedItemKinds.Select(k => _itemTypeLookup.BaseItemKindNames[k]).ToArray();
        var related = _queryHelpers.ApplyAccessFiltering(
            context,
            context.BaseItems.AsNoTracking().Where(e => typeNames.Contains(e.Type)),
            accessFilter);

        var valueTypes = GetItemValueTypes(kind);
        if (valueTypes.Length > 0)
        {
            CountByItemValue(context, related, kind, relatedItemKinds, valueTypes, nameItems, result);
        }
        else if (kind == BaseItemKind.Person)
        {
            CountByPersonName(context, related, nameItems, result);
        }
        else if (kind == BaseItemKind.Year)
        {
            CountByProductionYear(related, nameItems, result);
        }

        return result;
    }

    private void CountByItemValue(
        JellyfinDbContext context,
        IQueryable<BaseItemEntity> related,
        BaseItemKind kind,
        BaseItemKind[] relatedItemKinds,
        ItemValueType[] valueTypes,
        NameItem[] nameItems,
        Dictionary<Guid, ItemCounts> result)
    {
        var cleanNames = nameItems
            .Select(n => n.CleanName)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (cleanNames.Length == 0)
        {
            return;
        }

        var grouped = context.ItemValuesMap.AsNoTracking()
            .Where(ivm => valueTypes.Contains(ivm.ItemValue.Type))
            .WhereOneOrMany(cleanNames, ivm => ivm.ItemValue.CleanValue)
            .Join(related, ivm => ivm.ItemId, e => e.Id, (ivm, e) => new { ivm.ItemValue.CleanValue, e.Type, e.Id })
            .GroupBy(x => new { x.CleanValue, x.Type })
            .Select(g => new { g.Key.CleanValue, g.Key.Type, Count = g.Select(x => x.Id).Distinct().Count() })
            .ToArray();

        var byCleanName = grouped
            .GroupBy(g => g.CleanValue, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(x => (x.Type, x.Count)).ToArray(), StringComparer.Ordinal);

        var seriesTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Series];
        var episodeRollUp = RollsUpEpisodes(kind, relatedItemKinds)
                && Array.Exists(grouped, g => string.Equals(g.Type, seriesTypeName, StringComparison.Ordinal))
            ? CountEpisodesOfTaggedSeriesByCleanName(context, related, valueTypes, cleanNames)
            : null;

        foreach (var nameItem in nameItems)
        {
            if (nameItem.CleanName is null || !byCleanName.TryGetValue(nameItem.CleanName, out var counts))
            {
                continue;
            }

            var itemCounts = ItemCountBuilder.Build(_itemTypeLookup, counts);

            if (episodeRollUp is not null)
            {
                var rollUp = episodeRollUp.GetValueOrDefault(nameItem.CleanName);

                // Episodes of a tagged series count towards it even when untagged themselves, and
                // a tagged episode of a tagged series must not be counted a second time.
                var directEpisodeCount = itemCounts.EpisodeCount - rollUp.TaggedEpisodesOfTaggedSeries;
                ItemCountBuilder.SetEpisodeCount(itemCounts, rollUp.EpisodesOfTaggedSeries + directEpisodeCount);
            }

            result[nameItem.Id] = itemCounts;
        }
    }

    private void CountByPersonName(
        JellyfinDbContext context,
        IQueryable<BaseItemEntity> related,
        NameItem[] nameItems,
        Dictionary<Guid, ItemCounts> result)
    {
        var names = nameItems
            .Select(n => n.Name)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (names.Length == 0)
        {
            return;
        }

        var grouped = context.PeopleBaseItemMap.AsNoTracking()
            .WhereOneOrMany(names, m => m.People.Name)
            .Join(related, m => m.ItemId, e => e.Id, (m, e) => new { m.People.Name, e.Type, e.Id })
            .GroupBy(x => new { x.Name, x.Type })
            // A person can be credited on one item more than once, in different roles.
            .Select(g => new { g.Key.Name, g.Key.Type, Count = g.Select(x => x.Id).Distinct().Count() })
            .ToArray();

        ApplyGroupedCounts(nameItems, n => n.Name, grouped.Select(g => (g.Name, g.Type, g.Count)), result);
    }

    private void CountByProductionYear(
        IQueryable<BaseItemEntity> related,
        NameItem[] nameItems,
        Dictionary<Guid, ItemCounts> result)
    {
        var years = new List<int>();
        foreach (var nameItem in nameItems)
        {
            if (int.TryParse(nameItem.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year)
                && !years.Contains(year))
            {
                years.Add(year);
            }
            else if (string.Equals(count.Key, lookup[BaseItemKind.AudioBook], StringComparison.Ordinal))
            {
                result.AudioBookCount = count.Count;
            }
        }

        if (years.Count == 0)
        {
            return;
        }

        // No join, so no row can be reached twice and a plain count is the distinct count.
        var grouped = related
            .Where(e => e.ProductionYear != null)
            .WhereOneOrMany(years, e => e.ProductionYear!.Value)
            .GroupBy(e => new { Year = e.ProductionYear!.Value, e.Type })
            .Select(g => new { g.Key.Year, g.Key.Type, Count = g.Count() })
            .ToArray();

        var byYear = grouped
            .GroupBy(g => g.Year)
            .ToDictionary(g => g.Key, g => g.Select(x => (x.Type, x.Count)).ToArray());

        foreach (var nameItem in nameItems)
        {
            if (int.TryParse(nameItem.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year)
                && byYear.TryGetValue(year, out var counts))
            {
                result[nameItem.Id] = ItemCountBuilder.Build(_itemTypeLookup, counts);
            }
        }
    }

    private void ApplyGroupedCounts(
        NameItem[] nameItems,
        Func<NameItem, string?> keySelector,
        IEnumerable<(string Key, string Type, int Count)> grouped,
        Dictionary<Guid, ItemCounts> result)
    {
        var byKey = grouped
            .GroupBy(g => g.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(x => (x.Type, x.Count)).ToArray(), StringComparer.Ordinal);

        foreach (var nameItem in nameItems)
        {
            var key = keySelector(nameItem);
            if (key is not null && byKey.TryGetValue(key, out var counts))
            {
                result[nameItem.Id] = ItemCountBuilder.Build(_itemTypeLookup, counts);
            }
        }
    }

    private static bool RollsUpEpisodes(BaseItemKind kind, BaseItemKind[] relatedItemKinds)
        => kind is BaseItemKind.Studio or BaseItemKind.Genre or BaseItemKind.MusicGenre
            && relatedItemKinds.Contains(BaseItemKind.Episode)
            && relatedItemKinds.Contains(BaseItemKind.Series);

    private Dictionary<string, (int EpisodesOfTaggedSeries, int TaggedEpisodesOfTaggedSeries)> CountEpisodesOfTaggedSeriesByCleanName(
        JellyfinDbContext context,
        IQueryable<BaseItemEntity> related,
        ItemValueType[] valueTypes,
        string[] cleanNames)
    {
        var seriesTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Series];
        var episodeTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Episode];

        var taggedValues = context.ItemValuesMap.AsNoTracking()
            .Where(ivm => valueTypes.Contains(ivm.ItemValue.Type))
            .WhereOneOrMany(cleanNames, ivm => ivm.ItemValue.CleanValue);

        // The series carrying each clean name. Distinct, because one item can be mapped to the
        // same clean name once per value type.
        var taggedSeries = taggedValues
            .Join(
                related.Where(e => e.Type == seriesTypeName),
                ivm => ivm.ItemId,
                e => e.Id,
                (ivm, e) => new { ivm.ItemValue.CleanValue, SeriesId = e.Id })
            .Distinct();

        var episodes = related.Where(e => e.Type == episodeTypeName && e.SeriesId != null);

        var episodesOfTaggedSeries = taggedSeries
            .Join(episodes, s => s.SeriesId, e => e.SeriesId!.Value, (s, e) => new { s.CleanValue, e.Id })
            .GroupBy(x => x.CleanValue)
            .Select(g => new { CleanValue = g.Key, Count = g.Select(x => x.Id).Distinct().Count() })
            .ToArray();

        // Episodes that carry the clean name themselves *and* belong to a series carrying it. The
        // roll-up already counts those, so they have to come off the directly tagged ones.
        var taggedEpisodesOfTaggedSeries = taggedValues
            .Join(episodes, ivm => ivm.ItemId, e => e.Id, (ivm, e) => new { ivm.ItemValue.CleanValue, e.Id, e.SeriesId })
            .Join(
                taggedSeries,
                e => new { e.CleanValue, SeriesId = e.SeriesId!.Value },
                s => new { s.CleanValue, s.SeriesId },
                (e, s) => new { e.CleanValue, e.Id })
            .GroupBy(x => x.CleanValue)
            .Select(g => new { CleanValue = g.Key, Count = g.Select(x => x.Id).Distinct().Count() })
            .ToArray();

        var taggedLookup = taggedEpisodesOfTaggedSeries
            .ToDictionary(x => x.CleanValue, x => x.Count, StringComparer.Ordinal);

        // Every clean name in taggedLookup came from an episode of a tagged series, so it always
        // has a row in episodesOfTaggedSeries too - no second merge pass is needed.
        var result = new Dictionary<string, (int EpisodesOfTaggedSeries, int TaggedEpisodesOfTaggedSeries)>(StringComparer.Ordinal);
        foreach (var entry in episodesOfTaggedSeries)
        {
            result[entry.CleanValue] = (entry.Count, taggedLookup.GetValueOrDefault(entry.CleanValue));
        }

        return result;
    }

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

    /// <summary>
    /// A by-name item, reduced to the three columns the counting keys off.
    /// </summary>
    /// <param name="Id">The id of the by-name item.</param>
    /// <param name="Name">The name of the by-name item.</param>
    /// <param name="CleanName">The cleaned name of the by-name item.</param>
    private sealed record NameItem(Guid Id, string? Name, string? CleanName);
}
