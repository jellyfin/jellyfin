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
    // What the by-name item is reached through.
    private enum ByNameLink
    {
        Credit,
        Genre,
        Studio
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetAllArtists(InternalItemsQuery filter)
    {
        return GetItemsByName(filter, ByNameLink.Credit, _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist], _artistCreditKinds);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetArtists(InternalItemsQuery filter)
    {
        return GetItemsByName(filter, ByNameLink.Credit, _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist], _trackArtistCreditKinds);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetAlbumArtists(InternalItemsQuery filter)
    {
        return GetItemsByName(filter, ByNameLink.Credit, _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist], _albumArtistCreditKinds);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetStudios(InternalItemsQuery filter)
    {
        return GetItemsByName(filter, ByNameLink.Studio, _itemTypeLookup.BaseItemKindNames[BaseItemKind.Studio]);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetGenres(InternalItemsQuery filter)
    {
        return GetItemsByName(filter, ByNameLink.Genre, _itemTypeLookup.BaseItemKindNames[BaseItemKind.Genre]);
    }

    /// <inheritdoc />
    public QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetMusicGenres(InternalItemsQuery filter)
    {
        return GetItemsByName(filter, ByNameLink.Genre, _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicGenre]);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetStudioNames()
    {
        return GetLinkedByNameNames(BaseItemKind.Studio);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllArtistNames()
    {
        using var context = _dbProvider.CreateDbContext();

        // From the credits, so a renamed artist is listed under the name its item carries now.
        return context.Peoples
            .AsNoTracking()
            .Where(p => _artistCreditKinds.Contains(p.PersonType))
            .Join(
                context.BaseItems.Where(b => b.Type == _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist]),
                p => p.ItemId,
                b => b.Id,
                (p, b) => b.Name!)
            .Distinct()
            .ToArray();
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetMusicGenreNames()
    {
        return GetLinkedByNameNames(BaseItemKind.MusicGenre);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetGenreNames()
    {
        return GetLinkedByNameNames(BaseItemKind.Genre);
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

    /// <inheritdoc />
    public IReadOnlyList<string> GetTagNames(InternalItemsQuery filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        PrepareFilterQuery(filter);

        using var context = _dbProvider.CreateDbContext();
        var baseQuery = PrepareItemQuery(context, filter);
        baseQuery = TranslateQuery(baseQuery, context, filter);

        var matchingItemIds = baseQuery.Select(e => e.Id);

        return context.BaseItemTags
            .AsNoTracking()
            .Where(t => matchingItemIds.Contains(t.ItemId))
            .GroupBy(t => t.CleanValue)
            .Select(g => g.Min(t => t.Value)!)
            .OrderBy(t => t)
            .ToArray();
    }

    // From the links, so a genre or studio is listed under the name its item carries now.
    private string[] GetLinkedByNameNames(BaseItemKind kind)
    {
        using var context = _dbProvider.CreateDbContext();

        var typeName = _itemTypeLookup.BaseItemKindNames[kind];
        var linkedIds = kind == BaseItemKind.Studio
            ? context.BaseItemStudios.Select(e => e.StudioItemId)
            : context.BaseItemGenres.Select(e => e.GenreItemId);

        return context.BaseItems
            .AsNoTracking()
            .Where(e => e.Type == typeName && e.Name != null && linkedIds.Contains(e.Id))
            .Select(e => e.Name!)
            .Distinct()
            .ToArray();
    }

    private QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetItemsByName(
        InternalItemsQuery filter,
        ByNameLink link,
        string returnType,
        string[]? personKinds = null)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (link == ByNameLink.Credit == (personKinds is null))
        {
            throw new ArgumentException("Credit kinds apply to credits and nothing else.", nameof(personKinds));
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

        var byName = PrepareItemQuery(context, filter).Where(e => e.Type == returnType);

        var innerQuery = link switch
        {
            ByNameLink.Credit => byName.Where(e => context.PeopleBaseItemMap
                .Where(m => m.People.ItemId.Equals(e.Id) && personKinds!.Contains(m.People.PersonType))
                .Join(
                    innerQueryFilter,
                    m => m.ItemId,
                    g => g.Id,
                    (m, g) => m.ItemId)
                .Any()),
            ByNameLink.Genre => byName.Where(e => context.BaseItemGenres
                .Where(m => m.GenreItemId == e.Id)
                .Join(
                    innerQueryFilter,
                    m => m.ItemId,
                    g => g.Id,
                    (m, g) => m.ItemId)
                .Any()),
            _ => byName.Where(e => context.BaseItemStudios
                .Where(m => m.StudioItemId == e.Id)
                .Join(
                    innerQueryFilter,
                    m => m.ItemId,
                    g => g.Id,
                    (m, g) => m.ItemId)
                .Any()),
        };

        var outerQueryFilter = new InternalItemsQuery(filter.User)
        {
            IsPlayed = filter.IsPlayed,
            IsFavorite = filter.IsFavorite,
            IsFavoriteOrLiked = filter.IsFavoriteOrLiked,
            IsLiked = filter.IsLiked,
            IsLocked = filter.IsLocked,
            ImageTypes = filter.ImageTypes,
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
            ExcludeItemIds = filter.ExcludeItemIds,

            // A genre, studio or artist carries none of the tags of the media it describes, so an
            // allow list can only ever hide all of them. Reachability is settled by innerQueryFilter
            // instead: a value gets this far only when an item the user may see carries it.
            IncludeInheritedTags = []
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
        var page = query.AsEnumerable().Where(e => e is not null).ToList();

        if (filter.DtoOptions.ContainsField(ItemFields.ItemCounts))
        {
            // Only the items on this page need counting, which keeps the work proportional to the
            // page instead of to every genre, studio or artist in the library.
            var countsByItem = BuildItemCountsByLinkedItem(context, filter, link, personKinds, page.Select(e => e.Id).ToList());

            result.Items =
            [
                .. page
                    .Select(e =>
                    {
                        var item = DeserializeBaseItem(e, filter.SkipDeserialization);
                        var itemCount = countsByItem.GetValueOrDefault(e.Id);
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
                .. page
                    .Select(e => DeserializeBaseItem(e, filter.SkipDeserialization))
                    .Where(item => item != null)
                    .Select(item => (item!, (ItemCounts?)null))
            ];
        }

        return result;
    }

    private Dictionary<Guid, ItemCounts> BuildItemCountsByLinkedItem(
        Database.Implementations.JellyfinDbContext context,
        InternalItemsQuery filter,
        ByNameLink link,
        string[]? personKinds,
        IReadOnlyList<Guid> keys)
    {
        if (keys.Count == 0)
        {
            return [];
        }

        // The counts describe everything the item is linked to, not only the types the list was
        // filtered down to. GET /Studios/{name} answers that way and the two have to agree.
        var scopeQuery = new InternalItemsQuery(filter.User)
        {
            ExcludeItemTypes = filter.ExcludeItemTypes,
            MediaTypes = filter.MediaTypes,
            AncestorIds = filter.AncestorIds,
            ExcludeItemIds = filter.ExcludeItemIds,
            ItemIds = filter.ItemIds,
            TopParentIds = filter.TopParentIds,
            ParentId = filter.ParentId,
            IsPlayed = filter.IsPlayed
        };

        var scopedItems = TranslateQuery(context.BaseItems.AsNoTracking().Where(e => e.Id != EF.Constant(PlaceholderId)), context, scopeQuery);

        // Joined from the link table rather than walked through a navigation: SelectMany needs SQL APPLY.
        // The scope is joined in as well rather than checked as "ItemId IN (SELECT Id FROM BaseItems
        // WHERE ...)": that form makes SQLite scan every item in the library before the keys narrow it.
        IQueryable<ByNameLinkRow> links = link switch
        {
            ByNameLink.Credit => context.PeopleBaseItemMap
                .AsNoTracking()
                .Where(m => personKinds!.Contains(m.People.PersonType))
                .Select(m => new ByNameLinkRow { Key = m.People.ItemId, ItemId = m.ItemId }),
            ByNameLink.Genre => context.BaseItemGenres
                .AsNoTracking()
                .Select(m => new ByNameLinkRow { Key = m.GenreItemId, ItemId = m.ItemId }),
            _ => context.BaseItemStudios
                .AsNoTracking()
                .Select(m => new ByNameLinkRow { Key = m.StudioItemId, ItemId = m.ItemId }),
        };

        links = links.WhereOneOrMany(keys, l => l.Key);

        var rawCounts = links
            .Join(
                scopedItems,
                l => l.ItemId,
                e => e.Id,
                (l, e) => new { l.Key, e.Type, e.SeriesId, e.Id })
            .GroupBy(x => new { x.Key, x.Type, x.SeriesId })
            // Distinct items: one artist can be credited on a track more than once.
            .Select(g => new { g.Key.Key, g.Key.Type, g.Key.SeriesId, Count = g.Select(x => x.Id).Distinct().Count() })
            .ToList();

        var seriesTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Series];
        var episodeTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Episode];

        // A series passes its genres and studios down to every one of its episodes; a credit on the
        // series says nothing about who worked on a given episode.
        // Grouping by series as well costs nothing here and saves a second pass over the same rows:
        // it is what tells an episode carrying the link itself apart from one that inherits it.
        var episodeCounts = link == ByNameLink.Credit
            ? rawCounts
                .Where(x => x.Type == episodeTypeName)
                .GroupBy(x => x.Key)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Count))
            : BuildEpisodeCountsByLinkedItem(
                scopedItems,
                links,
                rawCounts
                    .Where(x => x.Type == episodeTypeName)
                    .Select(x => (x.Key, x.SeriesId, x.Count))
                    .ToList(),
                seriesTypeName,
                episodeTypeName);

        var countsByKey = new Dictionary<Guid, ItemCounts>();
        foreach (var group in rawCounts.GroupBy(x => x.Key))
        {
            var counts = ItemCountBuilder.Build(_itemTypeLookup, group.Select(x => (x.Type, x.Count)));

            // Episodes are counted separately: a series usually holds the link on their behalf.
            ItemCountBuilder.SetEpisodeCount(counts, episodeCounts.GetValueOrDefault(group.Key));
            countsByKey[group.Key] = counts;
        }

        // An item nothing but the episodes below a linked series stands for has no row of its own.
        foreach (var (key, episodeCount) in episodeCounts)
        {
            if (!countsByKey.ContainsKey(key))
            {
                var counts = new ItemCounts();
                ItemCountBuilder.SetEpisodeCount(counts, episodeCount);
                countsByKey[key] = counts;
            }
        }

        return countsByKey;
    }

    /// <summary>
    /// Counts the episodes an item stands for: the ones linked to it plus the ones below a series that is.
    /// </summary>
    private static Dictionary<Guid, int> BuildEpisodeCountsByLinkedItem(
        IQueryable<BaseItemEntity> scopedItems,
        IQueryable<ByNameLinkRow> links,
        IReadOnlyList<(Guid Key, Guid? SeriesId, int Count)> linkedEpisodes,
        string seriesTypeName,
        string episodeTypeName)
    {
        // Resolved in steps rather than as one union: each of these drives off an index, while the
        // single-statement form leaves SQLite free to scan every episode in the library instead.
        var linkedSeries = links
            .Join(
                scopedItems.Where(e => e.Type == seriesTypeName),
                l => l.ItemId,
                e => e.Id,
                (l, e) => new { l.Key, SeriesId = e.Id })
            .ToList();

        var seriesIds = linkedSeries.Select(x => x.SeriesId).Distinct().ToArray();
        var episodesPerSeries = seriesIds.Length == 0
            ? new Dictionary<Guid, int>()
            : scopedItems
                .Where(e => e.Type == episodeTypeName && e.SeriesId != null)
                .WhereOneOrMany(seriesIds, e => e.SeriesId!.Value)
                .GroupBy(e => e.SeriesId!.Value)
                .Select(g => new { SeriesId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.SeriesId, x => x.Count);

        var episodeCounts = new Dictionary<Guid, int>();
        var seriesByKey = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var group in linkedSeries.GroupBy(x => x.Key))
        {
            var series = group.Select(x => x.SeriesId).ToHashSet();
            seriesByKey[group.Key] = series;
            episodeCounts[group.Key] = series.Sum(id => episodesPerSeries.GetValueOrDefault(id));
        }

        foreach (var row in linkedEpisodes)
        {
            // Already covered by the series it belongs to.
            if (row.SeriesId is not null
                && seriesByKey.TryGetValue(row.Key, out var series)
                && series.Contains(row.SeriesId.Value))
            {
                continue;
            }

            episodeCounts[row.Key] = episodeCounts.GetValueOrDefault(row.Key) + row.Count;
        }

        return episodeCounts;
    }

    private sealed class ByNameLinkRow
    {
        public Guid Key { get; set; }

        public Guid ItemId { get; set; }
    }
}
