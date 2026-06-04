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
                baseQuery = context.PeopleBaseItemMap
                    .AsNoTracking()
                    .Where(m => m.People.Name == item.Name)
                    .Select(m => m.Item);
                break;
            case BaseItemKind.MusicArtist:
                baseQuery = context.ItemValuesMap
                    .AsNoTracking()
                    .Where(ivm => ivm.ItemValue.CleanValue == item.CleanName
                        && (ivm.ItemValue.Type == ItemValueType.Artist || ivm.ItemValue.Type == ItemValueType.AlbumArtist))
                    .Select(ivm => ivm.Item);
                break;
            case BaseItemKind.Genre:
            case BaseItemKind.MusicGenre:
                baseQuery = context.ItemValuesMap
                    .AsNoTracking()
                    .Where(ivm => ivm.ItemValue.CleanValue == item.CleanName
                        && ivm.ItemValue.Type == ItemValueType.Genre)
                    .Select(ivm => ivm.Item);
                break;
            case BaseItemKind.Studio:
                baseQuery = context.ItemValuesMap
                    .AsNoTracking()
                    .Where(ivm => ivm.ItemValue.CleanValue == item.CleanName
                        && ivm.ItemValue.Type == ItemValueType.Studios)
                    .Select(ivm => ivm.Item);
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

        result.ItemCount = totalCount;

        return result;
    }

    /// <inheritdoc/>
    public int GetPlayedCount(InternalItemsQuery filter, Guid ancestorId)
    {
        ArgumentNullException.ThrowIfNull(filter.User);
        using var dbContext = _dbProvider.CreateDbContext();

        var baseQuery = _queryHelpers.BuildAccessFilteredDescendantsQuery(dbContext, filter, ancestorId);
        return baseQuery.Count(b => b.UserData!.Any(u => u.UserId == filter.User.Id && u.Played));
    }

    /// <inheritdoc/>
    public int GetTotalCount(InternalItemsQuery filter, Guid ancestorId)
    {
        using var dbContext = _dbProvider.CreateDbContext();

        var baseQuery = _queryHelpers.BuildAccessFilteredDescendantsQuery(dbContext, filter, ancestorId);
        return baseQuery.Count();
    }

    /// <inheritdoc/>
    public (int Played, int Total) GetPlayedAndTotalCount(InternalItemsQuery filter, Guid ancestorId)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(filter.User);
        using var dbContext = _dbProvider.CreateDbContext();

        var baseQuery = _queryHelpers.BuildAccessFilteredDescendantsQuery(dbContext, filter, ancestorId);
        return GetPlayedAndTotalCountFromQuery(baseQuery, filter.User.Id);
    }

    /// <inheritdoc/>
    public (int Played, int Total) GetPlayedAndTotalCountFromLinkedChildren(InternalItemsQuery filter, Guid parentId)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(filter.User);
        using var dbContext = _dbProvider.CreateDbContext();

        var allDescendantIds = DescendantQueryHelper.GetAllDescendantIds(dbContext, parentId);
        var baseQuery = dbContext.BaseItems
            .Where(b => allDescendantIds.Contains(b.Id) && !b.IsFolder && !b.IsVirtualItem);
        baseQuery = _queryHelpers.ApplyAccessFiltering(dbContext, baseQuery, filter);

        return GetPlayedAndTotalCountFromQuery(baseQuery, filter.User.Id);
    }

    /// <inheritdoc/>
    public Dictionary<Guid, int> GetChildCountBatch(IReadOnlyList<Guid> parentIds, Guid? userId)
    {
        ArgumentNullException.ThrowIfNull(parentIds);

        if (parentIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        using var dbContext = _dbProvider.CreateDbContext();

        var parentIdsArray = parentIds.ToArray();

        var hierarchicalCounts = dbContext.BaseItems
            .Where(b => b.ParentId.HasValue && parentIdsArray.Contains(b.ParentId.Value))
            .GroupBy(b => b.ParentId!.Value)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.ParentId, x => x.Count);

        var linkedCounts = dbContext.LinkedChildren
            .Where(lc => parentIdsArray.Contains(lc.ParentId))
            .GroupBy(lc => lc.ParentId)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.ParentId, x => x.Count);

        var result = new Dictionary<Guid, int>();
        foreach (var parentId in parentIds)
        {
            var hierarchicalCount = hierarchicalCounts.GetValueOrDefault(parentId, 0);
            var linkedCount = linkedCounts.GetValueOrDefault(parentId, 0);

            result[parentId] = linkedCount > 0 ? linkedCount : hierarchicalCount;
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
        var folderIdsArray = folderIds.ToArray();
        var filter = new InternalItemsQuery(user);
        var userId = user.Id;

        var leafItems = dbContext.BaseItems
            .Where(b => !b.IsFolder && !b.IsVirtualItem);
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

        var results = ancestorLeaves
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

        return results;
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
