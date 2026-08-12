#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.EntityFrameworkCore;
using BaseItemDto = MediaBrowser.Controller.Entities.BaseItem;

namespace Jellyfin.Server.Implementations.Item;

public sealed partial class BaseItemRepository
{
    // How many items one pass of a rename holds, bounding a rename of a genre the whole library carries.
    private const int RenameChunkSize = 500;

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
    public IReadOnlyDictionary<Guid, ItemByNameLinks> GetItemByNameLinks(IReadOnlyList<Guid> itemIds)
    {
        ArgumentNullException.ThrowIfNull(itemIds);

        if (itemIds.Count == 0)
        {
            return new Dictionary<Guid, ItemByNameLinks>();
        }

        using var context = _dbProvider.CreateDbContext();

        var genres = context.BaseItemGenres
            .AsNoTracking()
            .WhereOneOrMany(itemIds, e => e.ItemId)
            .Join(
                context.BaseItems,
                e => e.GenreItemId,
                b => b.Id,
                (e, b) => new { e.ItemId, LinkedId = b.Id, b.Name })
            .ToList();

        var studios = context.BaseItemStudios
            .AsNoTracking()
            .WhereOneOrMany(itemIds, e => e.ItemId)
            .Join(
                context.BaseItems,
                e => e.StudioItemId,
                b => b.Id,
                (e, b) => new { e.ItemId, LinkedId = b.Id, b.Name })
            .ToList();

        var genresByItem = genres
            .GroupBy(e => e.ItemId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<NameGuidPair>)[.. g.Select(e => new NameGuidPair { Id = e.LinkedId, Name = e.Name ?? string.Empty })]);
        var studiosByItem = studios
            .GroupBy(e => e.ItemId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<NameGuidPair>)[.. g.Select(e => new NameGuidPair { Id = e.LinkedId, Name = e.Name ?? string.Empty })]);

        var result = new Dictionary<Guid, ItemByNameLinks>(genresByItem.Count + studiosByItem.Count);
        foreach (var itemId in genresByItem.Keys.Concat(studiosByItem.Keys).Distinct())
        {
            result[itemId] = new ItemByNameLinks(
                genresByItem.GetValueOrDefault(itemId) ?? [],
                studiosByItem.GetValueOrDefault(itemId) ?? []);
        }

        return result;
    }

    /// <inheritdoc />
    public ByNameRename RenameByNameLinks(Guid byNameItemId, BaseItemKind kind, string newName)
    {
        ArgumentException.ThrowIfNullOrEmpty(newName);

        using var context = _dbProvider.CreateDbContext();

        // One transaction over the whole rename, opened before anything is read. Committing it in
        // pieces would leave the by-name item and the items naming it disagreeing if anything failed
        // part way, and the next save of one of those items would resolve its old spelling to a second
        // by-name item. Reading inside it also means the set of links cannot grow behind the rewrite.
        using var transaction = context.Database.BeginTransaction();

        var previousName = context.BaseItems
            .AsNoTracking()
            .Where(e => e.Id.Equals(byNameItemId))
            .Select(e => e.Name)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(previousName) || string.Equals(previousName, newName, StringComparison.Ordinal))
        {
            return ByNameRename.None;
        }

        var isStudio = kind == BaseItemKind.Studio;

        // Materialised rather than composed: an AsNoTracking sub-query carries that flag into the
        // query it is composed into, and the rewrite below needs the items tracked to be saved.
        var linkedIds = isStudio
            ? context.BaseItemStudios.AsNoTracking().Where(e => e.StudioItemId.Equals(byNameItemId)).Select(e => e.ItemId).ToArray()
            : context.BaseItemGenres.AsNoTracking().Where(e => e.GenreItemId.Equals(byNameItemId)).Select(e => e.ItemId).ToArray();

        var rewritten = new List<Guid>();

        // The change tracker is emptied between chunks, so what it holds stays bounded however many
        // items carry the name. The transaction still covers all of them.
        foreach (var chunk in linkedIds.Chunk(RenameChunkSize))
        {
            var linked = context.BaseItems.WhereOneOrMany(chunk, e => e.Id).ToList();
            var changedInChunk = 0;

            foreach (var item in linked)
            {
                var names = isStudio ? item.Studios : item.Genres;
                if (string.IsNullOrEmpty(names) || !TryRename(names, previousName, newName, out var updated))
                {
                    continue;
                }

                if (isStudio)
                {
                    item.Studios = updated;
                }
                else
                {
                    item.Genres = updated;
                }

                rewritten.Add(item.Id);
                changedInChunk++;
            }

            if (changedInChunk > 0)
            {
                context.SaveChanges();
            }

            context.ChangeTracker.Clear();
        }

        // Last and in the same transaction, so the name the items now carry is the one the by-name item
        // ends up with, whether or not the caller goes on to save it.
        var byNameItem = context.BaseItems.FirstOrDefault(e => e.Id.Equals(byNameItemId));
        if (byNameItem is null)
        {
            // Gone since the name was read, so there is nothing to rename and nothing to have rewritten.
            transaction.Rollback();
            return ByNameRename.None;
        }

        byNameItem.Name = newName;
        byNameItem.CleanName = newName.GetCleanValue();
        context.SaveChanges();

        transaction.Commit();

        return new ByNameRename(previousName, rewritten);
    }

    // The names sit in one delimited column, so the whole value is rewritten rather than a substring of
    // it: replacing "Sci-Fi" inside "Sci-Fi Horror" would corrupt a name that merely contains it.
    private static bool TryRename(string names, string previousName, string newName, out string updated)
    {
        var parts = names.Split('|');
        var changed = false;
        for (var i = 0; i < parts.Length; i++)
        {
            if (string.Equals(parts[i], previousName, StringComparison.OrdinalIgnoreCase))
            {
                parts[i] = newName;
                changed = true;
            }
        }

        if (!changed)
        {
            updated = names;
            return false;
        }

        // Deduplicated, because the item may already carry the name it is being renamed onto.
        updated = string.Join('|', parts.Distinct(StringComparer.OrdinalIgnoreCase));
        return true;
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

    private QueryResult<(BaseItemDto Item, ItemCounts? ItemCounts)> GetItemsByName(
        InternalItemsQuery filter,
        ByNameLink link,
        string returnType,
        string[]? personKinds = null)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if ((link == ByNameLink.Credit) != (personKinds is not null))
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
            var countsByItem = BuildItemCountsByLinkedItem(context, filter, link, personKinds);

            result.Items =
            [
                .. query
                    .AsEnumerable()
                    .Where(e => e is not null)
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

    private Dictionary<Guid, ItemCounts> BuildItemCountsByLinkedItem(
        Database.Implementations.JellyfinDbContext context,
        InternalItemsQuery filter,
        ByNameLink link,
        string[]? personKinds)
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

        var itemIds = TranslateQuery(context.BaseItems.AsNoTracking().Where(e => e.Id != EF.Constant(PlaceholderId)), context, typeSubQuery)
            .Select(e => e.Id);

        // Joined from the link table rather than walked through a navigation: SelectMany needs SQL APPLY.
        IEnumerable<(Guid Key, string Type, int Count)> rawCounts = link switch
        {
            ByNameLink.Credit => context.PeopleBaseItemMap
                .Where(m => personKinds!.Contains(m.People.PersonType))
                .Where(m => itemIds.Contains(m.ItemId))
                .Join(
                    context.BaseItems,
                    m => m.ItemId,
                    e => e.Id,
                    (m, e) => new { Key = m.People.ItemId, e.Type })
                .GroupBy(x => new { x.Key, x.Type })
                .Select(g => new { g.Key.Key, g.Key.Type, Count = g.Count() })
                .AsEnumerable()
                .Select(x => (x.Key, x.Type, x.Count)),
            ByNameLink.Genre => context.BaseItemGenres
                .Where(m => itemIds.Contains(m.ItemId))
                .Join(
                    context.BaseItems,
                    m => m.ItemId,
                    e => e.Id,
                    (m, e) => new { Key = m.GenreItemId, e.Type })
                .GroupBy(x => new { x.Key, x.Type })
                .Select(g => new { g.Key.Key, g.Key.Type, Count = g.Count() })
                .AsEnumerable()
                .Select(x => (x.Key, x.Type, x.Count)),
            _ => context.BaseItemStudios
                .Where(m => itemIds.Contains(m.ItemId))
                .Join(
                    context.BaseItems,
                    m => m.ItemId,
                    e => e.Id,
                    (m, e) => new { Key = m.StudioItemId, e.Type })
                .GroupBy(x => new { x.Key, x.Type })
                .Select(g => new { g.Key.Key, g.Key.Type, Count = g.Count() })
                .AsEnumerable()
                .Select(x => (x.Key, x.Type, x.Count)),
        };

        return FoldCounts(rawCounts);
    }

    private Dictionary<TKey, ItemCounts> FoldCounts<TKey>(IEnumerable<(TKey Key, string Type, int Count)> rawCounts)
        where TKey : notnull
    {
        var seriesTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Series];
        var movieTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie];
        var episodeTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Episode];
        var musicAlbumTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicAlbum];
        var musicArtistTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist];
        var audioTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Audio];
        var trailerTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Trailer];

        var countsByKey = new Dictionary<TKey, ItemCounts>();
        foreach (var group in rawCounts.GroupBy(x => x.Key))
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

            countsByKey[group.Key] = counts;
        }

        return countsByKey;
    }
}
