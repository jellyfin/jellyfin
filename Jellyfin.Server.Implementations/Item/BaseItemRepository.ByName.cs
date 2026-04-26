#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
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

        if (!filter.Limit.HasValue)
        {
            filter.EnableTotalRecordCount = false;
        }

        using var context = _dbProvider.CreateDbContext();

        var innerQueryFilter = TranslateQuery(context.BaseItems.Where(e => e.Id != EF.Constant(PlaceholderId)), context, new InternalItemsQuery(filter.User)
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
        });

        // Keep this as an IQueryable sub-select. Materializing to a list would inline one
        // bound parameter per CleanValue and hit SQLite's variable cap on libraries with
        // high-cardinality value types (e.g. tens of thousands of artists).
        var matchingCleanValues = context.ItemValuesMap
            .Where(ivm => itemValueTypes.Contains(ivm.ItemValue.Type))
            .Join(
                innerQueryFilter,
                ivm => ivm.ItemId,
                g => g.Id,
                (ivm, g) => ivm.ItemValue.CleanValue)
            .Distinct();

        var innerQuery = PrepareItemQuery(context, filter)
            .Where(e => e.Type == returnType)
            .Where(e => matchingCleanValues.Contains(e.CleanName!));

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

        // Build the master query and collapse rows that share a PresentationUniqueKey
        // (e.g. alternate versions) by picking the lowest Id per group.
        var masterQuery = TranslateQuery(innerQuery, context, outerQueryFilter);

        var orderedMasterQuery = ApplyOrder(masterQuery, filter, context)
            .GroupBy(e => e.PresentationUniqueKey)
            .Select(g => g.Min(e => e.Id));

        var result = new QueryResult<(BaseItemDto, ItemCounts?)>();
        if (filter.EnableTotalRecordCount)
        {
            result.TotalRecordCount = orderedMasterQuery.Count();
        }

        if (filter.StartIndex.HasValue && filter.StartIndex.Value > 0)
        {
            orderedMasterQuery = orderedMasterQuery.Skip(filter.StartIndex.Value);
        }

        if (filter.Limit.HasValue)
        {
            orderedMasterQuery = orderedMasterQuery.Take(filter.Limit.Value);
        }

        var masterIds = orderedMasterQuery.ToList();

        var query = ApplyNavigations(
                context.BaseItems.AsNoTracking().AsSingleQuery().Where(e => masterIds.Contains(e.Id)),
                filter);

        query = ApplyOrder(query, filter, context);

        if (filter.IncludeItemTypes.Length > 0)
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
            // Instead, start from ItemValueMaps and join with BaseItems
            var countsByCleanName = context.ItemValuesMap
                .Where(ivm => itemValueTypes.Contains(ivm.ItemValue.Type))
                .Where(ivm => itemIds.Contains(ivm.ItemId))
                .Join(
                    context.BaseItems,
                    ivm => ivm.ItemId,
                    e => e.Id,
                    (ivm, e) => new { CleanName = ivm.ItemValue.CleanValue, e.Type })
                .GroupBy(x => new { x.CleanName, x.Type })
                .Select(g => new { g.Key.CleanName, g.Key.Type, Count = g.Count() })
                .GroupBy(x => x.CleanName)
                .ToDictionary(
                    g => g.Key,
                    g => new ItemCounts
                    {
                        SeriesCount = g.Where(x => x.Type == seriesTypeName).Sum(x => x.Count),
                        EpisodeCount = g.Where(x => x.Type == episodeTypeName).Sum(x => x.Count),
                        MovieCount = g.Where(x => x.Type == movieTypeName).Sum(x => x.Count),
                        AlbumCount = g.Where(x => x.Type == musicAlbumTypeName).Sum(x => x.Count),
                        ArtistCount = g.Where(x => x.Type == musicArtistTypeName).Sum(x => x.Count),
                        SongCount = g.Where(x => x.Type == audioTypeName).Sum(x => x.Count),
                        TrailerCount = g.Where(x => x.Type == trailerTypeName).Sum(x => x.Count),
                    });

            result.StartIndex = filter.StartIndex ?? 0;
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
            result.StartIndex = filter.StartIndex ?? 0;
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
}
