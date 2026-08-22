#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.EntityFrameworkCore;
using BaseItemDto = MediaBrowser.Controller.Entities.BaseItem;

namespace Jellyfin.Server.Implementations.Item;

public sealed partial class BaseItemRepository
{
    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetAllArtists(InternalItemsQuery filter)
    {
        return GetItemValues(filter, _getAllArtistsValueTypes, _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist]);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetArtists(InternalItemsQuery filter)
    {
        return GetItemValues(filter, _getArtistValueTypes, _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist]);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetAlbumArtists(InternalItemsQuery filter)
    {
        return GetItemValues(filter, _getAlbumArtistValueTypes, _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist]);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetStudios(InternalItemsQuery filter)
    {
        return GetItemValues(filter, _getStudiosValueTypes, _itemTypeLookup.BaseItemKindNames[BaseItemKind.Studio]);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetGenres(InternalItemsQuery filter)
    {
        return GetItemValues(filter, _getGenreValueTypes, _itemTypeLookup.BaseItemKindNames[BaseItemKind.Genre]);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetMusicGenres(InternalItemsQuery filter)
    {
        return GetItemValues(filter, _getGenreValueTypes, _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicGenre]);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetStudioNames()
    {
        return GetItemValueNames(_getStudiosValueTypes, [], []);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllArtistNames()
    {
        return GetItemValueNames(_getAllArtistsValueTypes, [], []);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetMusicGenreNames()
    {
        return GetItemValueNames(
            _getGenreValueTypes,
            _itemTypeLookup.MusicGenreTypes,
            []);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetGenreNames()
    {
        return GetItemValueNames(
            _getGenreValueTypes,
            [],
            _itemTypeLookup.MusicGenreTypes);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetMediaStreamLanguages(InternalItemsQuery filter, MediaStreamType mediaStreamType)
    {
        ArgumentNullException.ThrowIfNull(filter);

        using var context = _dbProvider.CreateDbContext();

        return TranslateQuery(
            context.BaseItems.Include(e => e.MediaStreams).Where(e => e.Id != EF.Constant(PlaceholderId)),
            context,
            new InternalItemsQuery(filter.User)
            {
                IncludeOwnedItems = filter.IncludeOwnedItems,
                ExcludeItemTypes = filter.ExcludeItemTypes,
                IncludeItemTypes = filter.IncludeItemTypes,
                MediaTypes = filter.MediaTypes,
                AncestorIds = filter.AncestorIds,
                ItemIds = filter.ItemIds,
                TopParentIds = filter.TopParentIds,
                ParentId = filter.ParentId,
                IsAiring = filter.IsAiring,
                IsMovie = filter.IsMovie,
                IsSports = filter.IsSports,
                IsKids = filter.IsKids,
                IsNews = filter.IsNews,
                IsSeries = filter.IsSeries
            })
            .SelectMany(e => e.MediaStreams!)
            .Where(e => e.StreamType == (MediaStreamTypeEntity)mediaStreamType)
            .Select(s => string.IsNullOrEmpty(s.Language) ? "und" : s.Language) // und = undetermined
            .Distinct()
            .ToArray();
    }

    private string[] GetItemValueNames(IReadOnlyList<ItemValueType> itemValueTypes, IReadOnlyList<string> withItemTypes, IReadOnlyList<string> excludeItemTypes)
    {
        using var context = _dbProvider.CreateDbContext();

        var query = context.ItemValuesMap
            .AsNoTracking()
            .Where(e => itemValueTypes.Any(w => w == e.ItemValue.Type));
        if (withItemTypes.Count > 0)
        {
            query = query.Where(e => withItemTypes.Contains(e.Item.Type));
        }

        if (excludeItemTypes.Count > 0)
        {
            query = query.Where(e => !excludeItemTypes.Contains(e.Item.Type));
        }

        return query.Select(e => e.ItemValue)
            .GroupBy(e => e.CleanValue)
            .Select(g => g.Min(v => v.Value)!)
            .ToArray();
    }

    private QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetItemValues(InternalItemsQuery filter, IReadOnlyList<ItemValueType> itemValueTypes, string returnType)
    {
        ArgumentNullException.ThrowIfNull(filter);

        using var context = _dbProvider.CreateDbContext();

        var innerQueryFilter = TranslateQuery(
            context.BaseItems.Where(e => e.Id != EF.Constant(PlaceholderId)),
            context,
            new InternalItemsQuery(filter.User)
            {
                ExcludeItemTypes = filter.ExcludeItemTypes,
                IncludeItemTypes = filter.IncludeItemTypes,
                MediaTypes = filter.MediaTypes,
                AncestorIds = filter.AncestorIds,
                ItemIds = filter.ItemIds,
                TopParentIds = filter.TopParentIds,
                ParentId = filter.ParentId,
                IsAiring = filter.IsAiring,
                IsMovie = filter.IsMovie,
                IsSports = filter.IsSports,
                IsKids = filter.IsKids,
                IsNews = filter.IsNews,
                IsSeries = filter.IsSeries
            },
            isCorrelatedSubQuery: true);

        var innerQuery = PrepareItemQuery(context, filter)
            .Where(e => e.Type == returnType)
            .Where(e => context.ItemValuesMap
                .Where(ivm => itemValueTypes.Contains(ivm.ItemValue.Type) && ivm.ItemValue.CleanValue == e.CleanName)
                .Join(
                    innerQueryFilter,
                    ivm => ivm.ItemId,
                    g => g.Id,
                    (ivm, g) => ivm.ItemId)
                .Any());

        var outerQueryFilter = new InternalItemsQuery(filter.User)
        {
            IsPlayed = filter.IsPlayed,
            IsFavorite = filter.IsFavorite,
            IsFavoriteOrLiked = filter.IsFavoriteOrLiked,
            IsLiked = filter.IsLiked,
            IsLocked = filter.IsLocked,
            NameLessThan = filter.NameLessThan,
            NameStartsWith = filter.NameStartsWith,
            NameStartsWithOrGreater = filter.NameStartsWithOrGreater,
            Tags = filter.Tags,
            OfficialRatings = filter.OfficialRatings,
            StudioIds = filter.StudioIds,
            GenreIds = filter.GenreIds,
            Genres = filter.Genres,
            Years = filter.Years,
            NameContains = filter.NameContains,
            SearchTerm = filter.SearchTerm,
            ExcludeItemIds = filter.ExcludeItemIds
        };

        // Collapse rows that share a PresentationUniqueKey (e.g. alternate versions) into one
        // representative id per group, then materialize the representative ids once.
        var masterQuery = TranslateQuery(innerQuery, context, outerQueryFilter);
        var isMusicArtist = returnType == _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist];
        List<Guid> representativeIds;
        if (isMusicArtist)
        {
            // For MusicArtist, prefer the entity from a library the user can actually access.
            // Materialize to prevent correlated per-group first-row queries which hurt performance.
            var topParentIds = filter.TopParentIds;
            representativeIds = masterQuery
                .Select(e => new { e.Id, e.PresentationUniqueKey, e.TopParentId })
                .AsEnumerable()
                .GroupBy(e => e.PresentationUniqueKey)
                .Select(g => g
                    .OrderBy(e => topParentIds.Contains(e.TopParentId ?? Guid.Empty) ? 0 : 1)
                    .ThenBy(e => e.Id)
                    .First().Id)
                .ToList();
        }
        else
        {
            representativeIds = masterQuery
                .GroupBy(e => e.PresentationUniqueKey)
                .Select(g => g.Min(e => e.Id))
                .ToList();
        }

        var result = new QueryResult<(BaseItemDto, ItemCounts?)>();
        if (filter.EnableTotalRecordCount)
        {
            result.TotalRecordCount = representativeIds.Count;
        }

        var query = ApplyNavigations(
                context.BaseItems.AsNoTracking().AsSingleQuery().WhereOneOrMany(representativeIds, e => e.Id),
                filter);

        query = ApplyOrder(query, filter, context);

        if (filter.StartIndex.HasValue && filter.StartIndex.Value > 0)
        {
            query = query.Skip(filter.StartIndex.Value);
        }

        if (filter.Limit.HasValue)
        {
            query = query.Take(filter.Limit.Value);
        }

        result.StartIndex = filter.StartIndex ?? 0;
        if (filter.IncludeItemTypes.Length > 0)
        {
            var countsByCleanName = BuildItemCountsByCleanName(context, filter, itemValueTypes);
            result.Items =
            [
                .. query
                    .AsEnumerable()
                    .Where(e => e is not null)
                    .Select(e =>
                    {
                        var item = DeserializeBaseItem(e, filter.SkipDeserialization);
                        countsByCleanName.TryGetValue(e.CleanName ?? string.Empty, out var itemCount);
                        return (item, itemCount);
                    })
                    .Where(x => x.item is not null)
                    .Select(x => (x.item!, x.itemCount))
            ];
        }
        else
        {
            result.Items =
            [
                .. query
                    .AsEnumerable()
                    .Where(e => e != null)
                    .Select(e => DeserializeBaseItem(e, filter.SkipDeserialization))
                    .Where(item => item != null)
                    .Select(item => (item!, (ItemCounts?)null))
            ];
        }

        return result;
    }

    private Dictionary<string, ItemCounts> BuildItemCountsByCleanName(
        Database.Implementations.JellyfinDbContext context,
        InternalItemsQuery filter,
        IReadOnlyList<ItemValueType> itemValueTypes)
    {
        var typeSubQuery = new InternalItemsQuery(filter.User)
        {
            ExcludeItemTypes = filter.ExcludeItemTypes,
            IncludeItemTypes = filter.IncludeItemTypes,
            MediaTypes = filter.MediaTypes,
            AncestorIds = filter.AncestorIds,
            ExcludeItemIds = filter.ExcludeItemIds,
            ItemIds = filter.ItemIds,
            TopParentIds = filter.TopParentIds,
            ParentId = filter.ParentId,
            IsPlayed = filter.IsPlayed
        };

        var itemCountQuery = TranslateQuery(context.BaseItems.AsNoTracking().Where(e => e.Id != EF.Constant(PlaceholderId)), context, typeSubQuery)
            .Where(e => e.ItemValues!.Any(f => itemValueTypes!.Contains(f.ItemValue.Type)));

        var seriesTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Series];
        var movieTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie];
        var episodeTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Episode];
        var musicAlbumTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicAlbum];
        var musicArtistTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist];
        var audioTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Audio];
        var trailerTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Trailer];
        var itemIds = itemCountQuery.Select(e => e.Id);

        // Rewrite query to avoid SelectMany on navigation properties (which requires SQL APPLY, not supported on SQLite)
        // Instead, start from ItemValueMaps and join with BaseItems.
        var rawCounts = context.ItemValuesMap
            .Where(ivm => itemValueTypes.Contains(ivm.ItemValue.Type))
            .Where(ivm => itemIds.Contains(ivm.ItemId))
            .Join(
                context.BaseItems,
                ivm => ivm.ItemId,
                e => e.Id,
                (ivm, e) => new { CleanName = ivm.ItemValue.CleanValue, e.Type })
            .GroupBy(x => new { x.CleanName, x.Type })
            .Select(g => new { g.Key.CleanName, g.Key.Type, Count = g.Count() })
            .AsEnumerable();

        var countsByCleanName = new Dictionary<string, ItemCounts>();
        foreach (var group in rawCounts.GroupBy(x => x.CleanName))
        {
            var counts = new ItemCounts();
            foreach (var row in group)
            {
                if (row.Type == seriesTypeName)
                {
                    counts.SeriesCount += row.Count;
                }
                else if (row.Type == episodeTypeName)
                {
                    counts.EpisodeCount += row.Count;
                }
                else if (row.Type == movieTypeName)
                {
                    counts.MovieCount += row.Count;
                }
                else if (row.Type == musicAlbumTypeName)
                {
                    counts.AlbumCount += row.Count;
                }
                else if (row.Type == musicArtistTypeName)
                {
                    counts.ArtistCount += row.Count;
                }
                else if (row.Type == audioTypeName)
                {
                    counts.SongCount += row.Count;
                }
                else if (row.Type == trailerTypeName)
                {
                    counts.TrailerCount += row.Count;
                }
            }

            countsByCleanName[group.Key] = counts;
        }

        return countsByCleanName;
    }
}
