#pragma warning disable RS0030 // Do not use banned APIs
// Do not enforce that because EFCore cannot deal with cultures well.
#pragma warning disable CA1304 // Specify CultureInfo
#pragma warning disable CA1309 // Use ordinal string comparison
#pragma warning disable CA1311 // Specify a culture or use an invariant version
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Implementations.MatchCriteria;
using Jellyfin.Extensions;
using Jellyfin.Extensions.Json;
using Jellyfin.Server.Implementations.Extensions;
using MediaBrowser.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Querying;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BaseItemDto = MediaBrowser.Controller.Entities.BaseItem;
using BaseItemEntity = Jellyfin.Database.Implementations.Entities.BaseItemEntity;
using DbLinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;
using LinkedChildType = MediaBrowser.Controller.Entities.LinkedChildType;

namespace Jellyfin.Server.Implementations.Item;

/*
    All queries in this class and all other nullable enabled EFCore repository classes will make liberal use of the null-forgiving operator "!".
    This is done as the code isn't actually executed client side, but only the expressions are interpret and the compiler cannot know that.
    This is your only warning/message regarding this topic.
*/

/// <summary>
/// Handles all storage logic for BaseItems.
/// </summary>
public sealed class BaseItemRepository
    : IItemRepository
{
    /// <summary>
    /// Gets the placeholder id for UserData detached items.
    /// </summary>
    public static readonly Guid PlaceholderId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// This holds all the types in the running assemblies
    /// so that we can de-serialize properly when we don't have strong types.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Type?> _typeMap = new ConcurrentDictionary<string, Type?>();
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IServerApplicationHost _appHost;
    private readonly IItemTypeLookup _itemTypeLookup;
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly ILogger<BaseItemRepository> _logger;

    private static readonly IReadOnlyList<ItemValueType> _getAllArtistsValueTypes = [ItemValueType.Artist, ItemValueType.AlbumArtist];
    private static readonly IReadOnlyList<ItemValueType> _getArtistValueTypes = [ItemValueType.Artist];
    private static readonly IReadOnlyList<ItemValueType> _getAlbumArtistValueTypes = [ItemValueType.AlbumArtist];
    private static readonly IReadOnlyList<ItemValueType> _getStudiosValueTypes = [ItemValueType.Studios];
    private static readonly IReadOnlyList<ItemValueType> _getGenreValueTypes = [ItemValueType.Genre];
    private static readonly IReadOnlyList<char> SearchWildcardTerms = ['%', '_', '[', ']', '^'];

    private static readonly string ImdbProviderName = MetadataProvider.Imdb.ToString().ToLowerInvariant();
    private static readonly string TmdbProviderName = MetadataProvider.Tmdb.ToString().ToLowerInvariant();
    private static readonly string TvdbProviderName = MetadataProvider.Tvdb.ToString().ToLowerInvariant();

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseItemRepository"/> class.
    /// </summary>
    /// <param name="dbProvider">The db factory.</param>
    /// <param name="appHost">The Application host.</param>
    /// <param name="itemTypeLookup">The static type lookup.</param>
    /// <param name="serverConfigurationManager">The server Configuration manager.</param>
    /// <param name="logger">System logger.</param>
    public BaseItemRepository(
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IServerApplicationHost appHost,
        IItemTypeLookup itemTypeLookup,
        IServerConfigurationManager serverConfigurationManager,
        ILogger<BaseItemRepository> logger)
    {
        _dbProvider = dbProvider;
        _appHost = appHost;
        _itemTypeLookup = itemTypeLookup;
        _serverConfigurationManager = serverConfigurationManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public void DeleteItem(params IReadOnlyList<Guid> ids)
    {
        if (ids is null || ids.Count == 0 || ids.Any(f => f.Equals(PlaceholderId)))
        {
            throw new ArgumentException("Guid can't be empty or the placeholder id.", nameof(ids));
        }

        using var context = _dbProvider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();

        var date = (DateTime?)DateTime.UtcNow;

        // Use owned-only traversal (AncestorIds) to avoid deleting items that are merely
        // linked via LinkedChildren (e.g. movies/series inside a BoxSet are associations, not owned children).
        var descendantIds = ids.SelectMany(f => DescendantQueryHelper.GetOwnedDescendantIds(context, f)).ToHashSet();
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

        // Remove any UserData entries for the placeholder item that would conflict with the UserData
        // being detached from the item being deleted. This is necessary because, during an update,
        // UserData may be reattached to a new entry, but some entries can be left behind.
        // Ensures there are no duplicate UserId/CustomDataKey combinations for the placeholder.
        context.UserData
            .Join(
                context.UserData.WhereOneOrMany(relatedItems, e => e.ItemId),
                placeholder => new { placeholder.UserId, placeholder.CustomDataKey },
                userData => new { userData.UserId, userData.CustomDataKey },
                (placeholder, userData) => placeholder)
            .Where(e => e.ItemId == PlaceholderId)
            .ExecuteDelete();

        // Detach all user watch data
        context.UserData.WhereOneOrMany(relatedItems, e => e.ItemId)
            .ExecuteUpdate(e => e
                .SetProperty(f => f.RetentionDate, date)
                .SetProperty(f => f.ItemId, PlaceholderId));

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
        // ItemValue Inheritance is now correctly mapped via AncestorId on demand
        context.SaveChanges();

        transaction.Commit();
    }

    /// <inheritdoc />
    public IReadOnlyList<Guid> GetItemIdsList(InternalItemsQuery filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        PrepareFilterQuery(filter);

        using var context = _dbProvider.CreateDbContext();
        return ApplyQueryFilter(context.BaseItems.AsNoTracking().Where(e => e.Id != EF.Constant(PlaceholderId)), context, filter).Select(e => e.Id).ToArray();
    }

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
    public QueryResult<BaseItemDto> GetItems(InternalItemsQuery filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (!filter.EnableTotalRecordCount || (!filter.Limit.HasValue && (filter.StartIndex ?? 0) == 0))
        {
            var returnList = GetItemList(filter);
            return new QueryResult<BaseItemDto>(
                filter.StartIndex,
                returnList.Count,
                returnList);
        }

        PrepareFilterQuery(filter);
        var result = new QueryResult<BaseItemDto>();

        using var context = _dbProvider.CreateDbContext();

        IQueryable<BaseItemEntity> dbQuery = PrepareItemQuery(context, filter);

        dbQuery = TranslateQuery(dbQuery, context, filter);
        dbQuery = ApplyGroupingFilter(context, dbQuery, filter);

        if (filter.EnableTotalRecordCount)
        {
            result.TotalRecordCount = dbQuery.Count();
        }

        dbQuery = ApplyQueryPaging(dbQuery, filter);
        dbQuery = ApplyNavigations(dbQuery, filter);

        result.Items = dbQuery.AsEnumerable().Where(e => e is not null).Select(w => DeserializeBaseItem(w, filter.SkipDeserialization)).Where(dto => dto is not null).ToArray()!;
        result.StartIndex = filter.StartIndex ?? 0;
        return result;
    }

    /// <inheritdoc />
    public IReadOnlyList<BaseItemDto> GetItemList(InternalItemsQuery filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        PrepareFilterQuery(filter);

        using var context = _dbProvider.CreateDbContext();
        IQueryable<BaseItemEntity> dbQuery = PrepareItemQuery(context, filter);

        dbQuery = TranslateQuery(dbQuery, context, filter);

        dbQuery = ApplyGroupingFilter(context, dbQuery, filter);
        dbQuery = ApplyQueryPaging(dbQuery, filter);

        var hasRandomSort = filter.OrderBy.Any(e => e.OrderBy == ItemSortBy.Random);
        if (hasRandomSort)
        {
            var orderedIds = dbQuery.Select(e => e.Id).ToList();
            if (orderedIds.Count == 0)
            {
                return Array.Empty<BaseItemDto>();
            }

            var itemsById = ApplyNavigations(context.BaseItems.Where(e => orderedIds.Contains(e.Id)), filter)
                .AsSplitQuery()
                .AsEnumerable()
                .Select(w => DeserializeBaseItem(w, filter.SkipDeserialization))
                .Where(dto => dto is not null)
                .ToDictionary(i => i!.Id);

            return orderedIds.Where(itemsById.ContainsKey).Select(id => itemsById[id]).ToArray()!;
        }

        dbQuery = ApplyNavigations(dbQuery, filter).AsSplitQuery();

        return dbQuery.AsEnumerable().Where(e => e is not null).Select(w => DeserializeBaseItem(w, filter.SkipDeserialization)).Where(dto => dto is not null).ToArray()!;
    }

    /// <inheritdoc/>
    public IReadOnlyList<BaseItemDto> GetLatestItemList(InternalItemsQuery filter, CollectionType collectionType)
    {
        ArgumentNullException.ThrowIfNull(filter);
        PrepareFilterQuery(filter);

        // Early exit if collection type is not supported
        if (collectionType is not CollectionType.movies and not CollectionType.tvshows and not CollectionType.music)
        {
            return [];
        }

        var limit = filter.Limit ?? 50;
        using var context = _dbProvider.CreateDbContext();

        var baseQuery = PrepareItemQuery(context, filter);
        baseQuery = TranslateQuery(baseQuery, context, filter);

        if (collectionType == CollectionType.tvshows)
        {
            return GetLatestTvShowItems(context, baseQuery, filter, limit);
        }

        // Find the top N group keys ordered by most recent DateCreated.
        // Movies group by PresentationUniqueKey (alternate versions like 4K/1080p share a key).
        // Music groups by Album.
        List<string> topGroupKeys;
        if (collectionType is CollectionType.movies)
        {
            topGroupKeys = baseQuery
                .Where(e => e.PresentationUniqueKey != null)
                .GroupBy(e => e.PresentationUniqueKey)
                .Select(g => new { GroupKey = g.Key!, MaxDate = g.Max(e => e.DateCreated) })
                .OrderByDescending(g => g.MaxDate)
                .Take(limit)
                .Select(g => g.GroupKey)
                .ToList();
        }
        else
        {
            topGroupKeys = baseQuery
                .Where(e => e.Album != null)
                .GroupBy(e => e.Album)
                .Select(g => new { GroupKey = g.Key!, MaxDate = g.Max(e => e.DateCreated) })
                .OrderByDescending(g => g.MaxDate)
                .Take(limit)
                .Select(g => g.GroupKey)
                .ToList();
        }

        // Get only the first (most recent) item ID per group using a lightweight projection,
        // then fetch full entities only for those items. This avoids loading all versions/tracks
        // with expensive navigation properties just to discard duplicates.
        var allItemsLite = collectionType switch
        {
            CollectionType.movies => baseQuery
                .Where(e => e.PresentationUniqueKey != null && topGroupKeys.Contains(e.PresentationUniqueKey))
                .OrderByDescending(e => e.DateCreated)
                .ThenByDescending(e => e.Id)
                .Select(e => new { e.Id, GroupKey = e.PresentationUniqueKey })
                .ToList(),
            _ => baseQuery
                .Where(e => e.Album != null && topGroupKeys.Contains(e.Album))
                .OrderByDescending(e => e.DateCreated)
                .ThenByDescending(e => e.Id)
                .Select(e => new { e.Id, GroupKey = e.Album })
                .ToList()
        };

        var firstIds = allItemsLite
            .DistinctBy(e => e.GroupKey)
            .Select(e => e.Id)
            .ToList();

        var itemsQuery = context.BaseItems.AsNoTracking().Where(e => firstIds.Contains(e.Id));
        itemsQuery = ApplyNavigations(itemsQuery, filter).AsSingleQuery();

        return itemsQuery
            .AsEnumerable()
            .OrderByDescending(e => e.DateCreated)
            .ThenByDescending(e => e.Id)
            .Select(w => DeserializeBaseItem(w, filter.SkipDeserialization))
            .Where(dto => dto is not null)
            .ToArray()!;
    }

    /// <summary>
    /// Gets the latest TV show items with smart Season/Series container selection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method implements intelligent container selection for TV shows in the "Latest" section.
    /// Instead of always showing individual episodes, it analyzes recent additions and may return
    /// a Season or Series container when multiple related episodes were recently added.
    /// </para>
    /// <para>
    /// The selection logic is:
    /// <list type="bullet">
    ///     <item>If recent episodes span multiple seasons → return the Series</item>
    ///     <item>If multiple recent episodes are from one season AND the series has multiple seasons → return the Season</item>
    ///     <item>If multiple recent episodes are from one season AND the series has only one season → return the Series</item>
    ///     <item>Otherwise → return the most recent Episode</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <param name="context">The database context.</param>
    /// <param name="baseQuery">The base query with filters already applied.</param>
    /// <param name="filter">The query filter options.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <returns>A list of BaseItemDto representing the latest TV content.</returns>
    private IReadOnlyList<BaseItemDto> GetLatestTvShowItems(JellyfinDbContext context, IQueryable<BaseItemEntity> baseQuery, InternalItemsQuery filter, int limit)
    {
        // Episodes added within this window are considered "recently added together"
        const double RecentAdditionWindowHours = 24.0;

        // Step 1: Find the top N series with recently added content, ordered by most recent addition
        var topSeriesWithDates = baseQuery
            .Where(e => e.SeriesName != null)
            .GroupBy(e => e.SeriesName)
            .Select(g => new { SeriesName = g.Key!, MaxDate = g.Max(e => e.DateCreated) })
            .OrderByDescending(g => g.MaxDate)
            .Take(limit)
            .ToList();

        var topSeriesNames = topSeriesWithDates.Select(g => g.SeriesName).ToList();

        // Compute a global date cutoff: the oldest series' max date minus the window.
        // Episodes before this cutoff cannot be in any series' "recent additions" window,
        // so we can safely exclude them to avoid loading ancient episodes.
        var globalCutoff = topSeriesWithDates.Count > 0
            ? topSeriesWithDates.Min(g => g.MaxDate)?.AddHours(-RecentAdditionWindowHours)
            : null;

        // Fetch only the columns needed for analysis (lightweight projection).
        var episodeQuery = baseQuery
            .Where(e => e.SeriesName != null && topSeriesNames.Contains(e.SeriesName));
        if (globalCutoff is not null)
        {
            episodeQuery = episodeQuery.Where(e => e.DateCreated >= globalCutoff);
        }

        var allEpisodes = episodeQuery
            .OrderByDescending(e => e.DateCreated)
            .ThenByDescending(e => e.Id)
            .Select(e => new { e.Id, e.SeriesName, e.DateCreated, e.SeasonId, e.SeriesId })
            .ToList();

        // Collect all season/series IDs we'll need to look up for count information
        var allSeasonIds = new HashSet<Guid>();
        var allSeriesIds = new HashSet<Guid>();

        // Analysis data for each series: recent episode count, season IDs, and the most recent episode ID
        var analysisData = new List<(
            int RecentEpisodeCount,
            List<Guid> SeasonIds,
            Guid? FirstRecentSeriesId,
            DateTime MaxDate,
            Guid MostRecentEpisodeId)>();

        // Step 3: Analyze each series to identify recent additions within the time window
        foreach (var group in allEpisodes.GroupBy(e => e.SeriesName))
        {
            var episodes = group.ToList();
            var mostRecentDate = episodes[0].DateCreated ?? DateTime.MinValue;
            var recentCutoff = mostRecentDate.AddHours(-RecentAdditionWindowHours);

            // Find episodes added within the recent window
            var recentEpisodeCount = 0;
            var seasonIdSet = new HashSet<Guid>();
            Guid? firstRecentSeriesId = null;

            foreach (var ep in episodes)
            {
                if (ep.DateCreated >= recentCutoff)
                {
                    recentEpisodeCount++;
                    if (ep.SeasonId.HasValue)
                    {
                        seasonIdSet.Add(ep.SeasonId.Value);
                    }

                    firstRecentSeriesId ??= ep.SeriesId;
                }
            }

            var seasonIds = seasonIdSet.ToList();
            analysisData.Add((recentEpisodeCount, seasonIds, firstRecentSeriesId, mostRecentDate, episodes[0].Id));

            // Track all unique season/series IDs for batch lookups
            foreach (var sid in seasonIds)
            {
                allSeasonIds.Add(sid);
            }

            if (firstRecentSeriesId.HasValue)
            {
                allSeriesIds.Add(firstRecentSeriesId.Value);
            }
        }

        // Step 4: Batch fetch counts - episodes per season and seasons per series
        // These counts help determine whether to show Season or Series as the container
        var episodeType = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Episode];
        var seasonType = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Season];
        var seasonEpisodeCounts = allSeasonIds.Count > 0
            ? context.BaseItems
                .AsNoTracking()
                .Where(e => e.SeasonId.HasValue && allSeasonIds.Contains(e.SeasonId.Value) && e.Type == episodeType)
                .GroupBy(e => e.SeasonId!.Value)
                .Select(g => new { SeasonId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.SeasonId, x => x.Count)
            : [];

        var seriesSeasonCounts = allSeriesIds.Count > 0
            ? context.BaseItems
                .AsNoTracking()
                .Where(e => e.SeriesId.HasValue && allSeriesIds.Contains(e.SeriesId.Value) && e.Type == seasonType)
                .GroupBy(e => e.SeriesId!.Value)
                .Select(g => new { SeriesId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.SeriesId, x => x.Count)
            : [];

        // Step 5: Apply the container selection logic for each series
        var entitiesToFetch = new HashSet<Guid>();
        var seriesResults = new List<(Guid? SeasonId, Guid? SeriesId, DateTime MaxDate, Guid MostRecentEpisodeId)>(analysisData.Count);

        foreach (var (recentEpisodeCount, seasonIds, firstRecentSeriesId, maxDate, mostRecentEpisodeId) in analysisData)
        {
            Guid? seasonId = null;
            Guid? seriesId = null;

            if (seasonIds.Count == 1)
            {
                // All recent episodes are from a single season
                var sid = seasonIds[0];
                var totalEpisodes = seasonEpisodeCounts.GetValueOrDefault(sid, 0);
                var totalSeasonsInSeries = firstRecentSeriesId.HasValue
                    ? seriesSeasonCounts.GetValueOrDefault(firstRecentSeriesId.Value, 1)
                    : 1;

                // Check if multiple episodes were added, or if all episodes in the season were added
                var hasMultipleOrAllEpisodes = recentEpisodeCount > 1 || recentEpisodeCount == totalEpisodes;

                if (totalSeasonsInSeries > 1 && hasMultipleOrAllEpisodes)
                {
                    // Multi-season series with bulk additions: show the Season
                    seasonId = sid;
                    entitiesToFetch.Add(sid);
                }
                else if (hasMultipleOrAllEpisodes && firstRecentSeriesId.HasValue)
                {
                    // Single-season series with bulk additions: show the Series
                    seriesId = firstRecentSeriesId;
                    entitiesToFetch.Add(firstRecentSeriesId.Value);
                }

                // Otherwise: single episode, will fall through to show the Episode
            }
            else if (seasonIds.Count > 1 && firstRecentSeriesId.HasValue)
            {
                // Recent episodes span multiple seasons: show the Series
                seriesId = firstRecentSeriesId;
                entitiesToFetch.Add(seriesId!.Value);
            }

            if (seasonId is null && seriesId is null)
            {
                entitiesToFetch.Add(mostRecentEpisodeId);
            }

            seriesResults.Add((seasonId, seriesId, maxDate, mostRecentEpisodeId));
        }

        // Step 6: Fetch the Season/Series entities we decided to return
        var entities = entitiesToFetch.Count > 0
            ? ApplyNavigations(
                    context.BaseItems.AsNoTracking().Where(e => entitiesToFetch.Contains(e.Id)),
                    filter)
                .AsSingleQuery()
                .ToDictionary(e => e.Id)
            : [];

        // Step 7: Build final results, preferring Season > Series > Episode.
        // All needed entities are already fetched in step 6 with navigation properties.
        var results = new List<(BaseItemEntity Entity, DateTime MaxDate)>(seriesResults.Count);
        foreach (var (seasonId, seriesId, maxDate, mostRecentEpisodeId) in seriesResults)
        {
            if (seasonId.HasValue && entities.TryGetValue(seasonId.Value, out var seasonEntity))
            {
                results.Add((seasonEntity, maxDate));
                continue;
            }

            if (seriesId.HasValue && entities.TryGetValue(seriesId.Value, out var seriesEntity))
            {
                results.Add((seriesEntity, maxDate));
                continue;
            }

            if (entities.TryGetValue(mostRecentEpisodeId, out var episodeEntity))
            {
                results.Add((episodeEntity, maxDate));
            }
        }

        return results
            .OrderByDescending(r => r.MaxDate)
            .ThenByDescending(r => r.Entity.Id)
            .Take(limit)
            .Select(r => DeserializeBaseItem(r.Entity, filter.SkipDeserialization))
            .Where(dto => dto is not null)
            .ToArray()!;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetNextUpSeriesKeys(InternalItemsQuery filter, DateTime dateCutoff)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(filter.User);

        using var context = _dbProvider.CreateDbContext();

        var query = context.BaseItems
            .AsNoTracking()
            .Where(i => filter.TopParentIds.Contains(i.TopParentId!.Value))
            .Where(i => i.Type == _itemTypeLookup.BaseItemKindNames[BaseItemKind.Episode])
            .Join(
                context.UserData.AsNoTracking().Where(e => e.ItemId != EF.Constant(PlaceholderId)),
                i => new { UserId = filter.User.Id, ItemId = i.Id },
                u => new { u.UserId, u.ItemId },
                (entity, data) => new { Item = entity, UserData = data })
            .GroupBy(g => g.Item.SeriesPresentationUniqueKey)
            .Select(g => new { g.Key, LastPlayedDate = g.Max(u => u.UserData.LastPlayedDate) })
            .Where(g => g.Key != null && g.LastPlayedDate != null && g.LastPlayedDate >= dateCutoff)
            .OrderByDescending(g => g.LastPlayedDate)
            .Select(g => g.Key!);

        if (filter.Limit.HasValue)
        {
            query = query.Take(filter.Limit.Value);
        }

        return query.ToArray();
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, NextUpEpisodeBatchResult> GetNextUpEpisodesBatch(
        InternalItemsQuery filter,
        IReadOnlyList<string> seriesKeys,
        bool includeSpecials,
        bool includeWatchedForRewatching)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(filter.User);

        if (seriesKeys.Count == 0)
        {
            return new Dictionary<string, NextUpEpisodeBatchResult>();
        }

        PrepareFilterQuery(filter);
        using var context = _dbProvider.CreateDbContext();

        var userId = filter.User.Id;
        var episodeTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Episode];

        // Get the last watched episode ID per series (highest season/episode that is played)
        var lastWatchedInfo = context.BaseItems
            .AsNoTracking()
            .Where(e => e.Type == episodeTypeName)
            .Where(e => e.SeriesPresentationUniqueKey != null && seriesKeys.Contains(e.SeriesPresentationUniqueKey))
            .Where(e => e.ParentIndexNumber != 0)
            .Where(e => e.UserData!.Any(ud => ud.UserId == userId && ud.Played))
            .GroupBy(e => e.SeriesPresentationUniqueKey)
            .Select(g => new
            {
                SeriesKey = g.Key!,
                LastWatchedId = g.OrderByDescending(e => e.ParentIndexNumber)
                                 .ThenByDescending(e => e.IndexNumber)
                                 .Select(e => e.Id)
                                 .FirstOrDefault()
            })
            .ToDictionary(x => x.SeriesKey, x => x.LastWatchedId);

        Dictionary<string, Guid> lastWatchedByDateInfo = new();
        if (includeWatchedForRewatching)
        {
            lastWatchedByDateInfo = context.BaseItems
                .AsNoTracking()
                .Where(e => e.Type == episodeTypeName)
                .Where(e => e.SeriesPresentationUniqueKey != null && seriesKeys.Contains(e.SeriesPresentationUniqueKey))
                .Where(e => e.ParentIndexNumber != 0)
                .Where(e => e.UserData!.Any(ud => ud.UserId == userId && ud.Played))
                .SelectMany(e => e.UserData!.Where(ud => ud.UserId == userId && ud.Played)
                    .Select(ud => new { Episode = e, ud.LastPlayedDate }))
                .GroupBy(x => x.Episode.SeriesPresentationUniqueKey)
                .Select(g => new
                {
                    SeriesKey = g.Key!,
                    LastWatchedId = g.OrderByDescending(x => x.LastPlayedDate)
                                     .Select(x => x.Episode.Id)
                                     .FirstOrDefault()
                })
                .ToDictionary(x => x.SeriesKey, x => x.LastWatchedId);
        }

        var allLastWatchedIds = lastWatchedInfo.Values
            .Concat(lastWatchedByDateInfo.Values)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        var lastWatchedEpisodes = new Dictionary<Guid, BaseItemEntity>();
        if (allLastWatchedIds.Count > 0)
        {
            var lwQuery = context.BaseItems.AsNoTracking().Where(e => allLastWatchedIds.Contains(e.Id));
            lwQuery = ApplyNavigations(lwQuery, filter).AsSingleQuery();
            lastWatchedEpisodes = lwQuery.ToDictionary(e => e.Id);
        }

        Dictionary<string, List<BaseItemEntity>> specialsBySeriesKey = new();
        if (includeSpecials)
        {
            var specialsQuery = context.BaseItems
                .AsNoTracking()
                .Where(e => e.Type == episodeTypeName)
                .Where(e => e.SeriesPresentationUniqueKey != null && seriesKeys.Contains(e.SeriesPresentationUniqueKey))
                .Where(e => e.ParentIndexNumber == 0)
                .Where(e => !e.IsVirtualItem);
            specialsQuery = ApplyNavigations(specialsQuery, filter).AsSingleQuery();

            foreach (var special in specialsQuery)
            {
                var key = special.SeriesPresentationUniqueKey!;
                if (!specialsBySeriesKey.TryGetValue(key, out var list))
                {
                    list = new List<BaseItemEntity>();
                    specialsBySeriesKey[key] = list;
                }

                list.Add(special);
            }
        }

        // Build position lookup from already-loaded last watched data
        var positionLookup = new Dictionary<string, (int Season, int Episode)>();
        foreach (var kvp in lastWatchedInfo)
        {
            if (kvp.Value != Guid.Empty
                && lastWatchedEpisodes.TryGetValue(kvp.Value, out var lw)
                && lw.ParentIndexNumber.HasValue
                && lw.IndexNumber.HasValue)
            {
                positionLookup[kvp.Key] = (lw.ParentIndexNumber.Value, lw.IndexNumber.Value);
            }
        }

        // Single query: fetch all unplayed non-virtual non-special episodes for all series.
        // Uses NOT EXISTS (via !Any) for the played check, which is more efficient than GroupJoin.
        // Only unplayed episodes are loaded (typically ~10% of total), keeping memory usage low.
        var allUnplayedCandidates = context.BaseItems
            .AsNoTracking()
            .Where(e => e.Type == episodeTypeName)
            .Where(e => e.SeriesPresentationUniqueKey != null && seriesKeys.Contains(e.SeriesPresentationUniqueKey))
            .Where(e => e.ParentIndexNumber != 0)
            .Where(e => !e.IsVirtualItem)
            .Where(e => !e.UserData!.Any(ud => ud.UserId == userId && ud.Played))
            .Select(e => new
            {
                e.Id,
                e.SeriesPresentationUniqueKey,
                e.ParentIndexNumber,
                EpisodeNumber = e.IndexNumber
            })
            .ToList();

        // In-memory: find the next unplayed episode per series, respecting last-watched position
        var nextEpisodeIds = new HashSet<Guid>();
        var seriesNextIdMap = new Dictionary<string, Guid>();

        foreach (var seriesKey in seriesKeys)
        {
            var candidates = allUnplayedCandidates
                .Where(c => c.SeriesPresentationUniqueKey == seriesKey);

            if (positionLookup.TryGetValue(seriesKey, out var pos))
            {
                candidates = candidates.Where(c =>
                    c.ParentIndexNumber > pos.Season
                    || (c.ParentIndexNumber == pos.Season && c.EpisodeNumber > pos.Episode));
            }

            var nextCandidate = candidates
                .OrderBy(c => c.ParentIndexNumber)
                .ThenBy(c => c.EpisodeNumber)
                .FirstOrDefault();

            if (nextCandidate is not null && nextCandidate.Id != Guid.Empty)
            {
                nextEpisodeIds.Add(nextCandidate.Id);
                seriesNextIdMap[seriesKey] = nextCandidate.Id;
            }
        }

        // Find next played episode per series for rewatching mode
        var seriesNextPlayedIdMap = new Dictionary<string, Guid>();
        if (includeWatchedForRewatching)
        {
            var allPlayedCandidates = context.BaseItems
                .AsNoTracking()
                .Where(e => e.Type == episodeTypeName)
                .Where(e => e.SeriesPresentationUniqueKey != null && seriesKeys.Contains(e.SeriesPresentationUniqueKey))
                .Where(e => e.ParentIndexNumber != 0)
                .Where(e => !e.IsVirtualItem)
                .Where(e => e.UserData!.Any(ud => ud.UserId == userId && ud.Played))
                .Select(e => new
                {
                    e.Id,
                    e.SeriesPresentationUniqueKey,
                    e.ParentIndexNumber,
                    EpisodeNumber = e.IndexNumber
                })
                .ToList();

            foreach (var seriesKey in seriesKeys)
            {
                if (!lastWatchedByDateInfo.TryGetValue(seriesKey, out var lastByDateId))
                {
                    continue;
                }

                var lastByDateEntity = lastWatchedEpisodes.GetValueOrDefault(lastByDateId);
                if (lastByDateEntity is null)
                {
                    continue;
                }

                var playedCandidates = allPlayedCandidates
                    .Where(c => c.SeriesPresentationUniqueKey == seriesKey);

                if (lastByDateEntity.ParentIndexNumber.HasValue && lastByDateEntity.IndexNumber.HasValue)
                {
                    var lastSeason = lastByDateEntity.ParentIndexNumber.Value;
                    var lastEp = lastByDateEntity.IndexNumber.Value;
                    playedCandidates = playedCandidates.Where(c =>
                        c.ParentIndexNumber > lastSeason
                        || (c.ParentIndexNumber == lastSeason && c.EpisodeNumber > lastEp));
                }

                var nextPlayedCandidate = playedCandidates
                    .OrderBy(c => c.ParentIndexNumber)
                    .ThenBy(c => c.EpisodeNumber)
                    .FirstOrDefault();

                if (nextPlayedCandidate is not null && nextPlayedCandidate.Id != Guid.Empty)
                {
                    nextEpisodeIds.Add(nextPlayedCandidate.Id);
                    seriesNextPlayedIdMap[seriesKey] = nextPlayedCandidate.Id;
                }
            }
        }

        // Batch fetch all next episode entities with navigation properties
        var nextEpisodes = new Dictionary<Guid, BaseItemEntity>();
        if (nextEpisodeIds.Count > 0)
        {
            var nextQuery = context.BaseItems.AsNoTracking().Where(e => nextEpisodeIds.Contains(e.Id));
            nextQuery = ApplyNavigations(nextQuery, filter).AsSingleQuery();
            nextEpisodes = nextQuery.ToDictionary(e => e.Id);
        }

        var result = new Dictionary<string, NextUpEpisodeBatchResult>();
        foreach (var seriesKey in seriesKeys)
        {
            var batchResult = new NextUpEpisodeBatchResult();

            if (lastWatchedInfo.TryGetValue(seriesKey, out var lwId) && lwId != Guid.Empty)
            {
                if (lastWatchedEpisodes.TryGetValue(lwId, out var entity))
                {
                    batchResult.LastWatched = DeserializeBaseItem(entity, filter.SkipDeserialization);
                }
            }

            if (seriesNextIdMap.TryGetValue(seriesKey, out var nextId) && nextEpisodes.TryGetValue(nextId, out var nextEntity))
            {
                batchResult.NextUp = DeserializeBaseItem(nextEntity, filter.SkipDeserialization);
            }

            if (includeSpecials && specialsBySeriesKey.TryGetValue(seriesKey, out var specials))
            {
                batchResult.Specials = specials.Select(s => DeserializeBaseItem(s, filter.SkipDeserialization)!).ToList();
            }
            else
            {
                batchResult.Specials = Array.Empty<BaseItemDto>();
            }

            if (includeWatchedForRewatching)
            {
                if (lastWatchedByDateInfo.TryGetValue(seriesKey, out var lastByDateId) &&
                    lastWatchedEpisodes.TryGetValue(lastByDateId, out var lastByDateEntity))
                {
                    batchResult.LastWatchedForRewatching = DeserializeBaseItem(lastByDateEntity, filter.SkipDeserialization);
                }

                if (seriesNextPlayedIdMap.TryGetValue(seriesKey, out var nextPlayedId) &&
                    nextEpisodes.TryGetValue(nextPlayedId, out var nextPlayedEntity))
                {
                    batchResult.NextPlayedForRewatching = DeserializeBaseItem(nextPlayedEntity, filter.SkipDeserialization);
                }
            }

            result[seriesKey] = batchResult;
        }

        return result;
    }

    private IQueryable<BaseItemEntity> ApplyGroupingFilter(JellyfinDbContext context, IQueryable<BaseItemEntity> dbQuery, InternalItemsQuery filter)
    {
        // This whole block is needed to filter duplicate entries on request
        // for the time being it cannot be used because it would destroy the ordering
        // this results in "duplicate" responses for queries that try to lookup individual series or multiple versions but
        // for that case the invoker has to run a DistinctBy(e => e.PresentationUniqueKey) on their own

        var enableGroupByPresentationUniqueKey = EnableGroupByPresentationUniqueKey(filter);
        if (enableGroupByPresentationUniqueKey && filter.GroupBySeriesPresentationUniqueKey)
        {
            var tempQuery = dbQuery.GroupBy(e => new { e.PresentationUniqueKey, e.SeriesPresentationUniqueKey }).Select(e => e.OrderBy(x => x.Id).FirstOrDefault()).Select(e => e!.Id);
            dbQuery = context.BaseItems.Where(e => tempQuery.Contains(e.Id));
        }
        else if (enableGroupByPresentationUniqueKey)
        {
            var tempQuery = dbQuery.GroupBy(e => e.PresentationUniqueKey).Select(e => e.OrderBy(x => x.Id).FirstOrDefault()).Select(e => e!.Id);
            dbQuery = context.BaseItems.Where(e => tempQuery.Contains(e.Id));
        }
        else if (filter.GroupBySeriesPresentationUniqueKey)
        {
            var tempQuery = dbQuery.GroupBy(e => e.SeriesPresentationUniqueKey).Select(e => e.OrderBy(x => x.Id).FirstOrDefault()).Select(e => e!.Id);
            dbQuery = context.BaseItems.Where(e => tempQuery.Contains(e.Id));
        }
        else
        {
            dbQuery = dbQuery.Distinct();
        }

        if (filter.CollapseBoxSetItems == true)
        {
            dbQuery = ApplyBoxSetCollapsing(context, dbQuery, filter.CollapseBoxSetItemTypes);
        }

        dbQuery = ApplyOrder(dbQuery, filter, context);

        return dbQuery;
    }

    private IQueryable<BaseItemEntity> ApplyBoxSetCollapsing(
        JellyfinDbContext context,
        IQueryable<BaseItemEntity> dbQuery,
        BaseItemKind[] collapsibleTypes)
    {
        var boxSetTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.BoxSet];

        var currentIds = dbQuery.Select(e => e.Id);

        if (collapsibleTypes.Length == 0)
        {
            // Collapse all item types into box sets
            return ApplyBoxSetCollapsingAll(context, currentIds, boxSetTypeName);
        }

        // Only collapse specific item types, keep others untouched
        var collapsibleTypeNames = collapsibleTypes.Select(t => _itemTypeLookup.BaseItemKindNames[t]).ToList();

        // Items whose type is NOT collapsible (always kept in results)
        var nonCollapsibleIds = currentIds
            .Where(id => !context.BaseItems.Any(bi => bi.Id == id && collapsibleTypeNames.Contains(bi.Type)));

        // Collapsible items that are NOT in any box set (kept in results)
        var collapsibleNotInBoxSet = currentIds
            .Where(id =>
                context.BaseItems.Any(bi => bi.Id == id && collapsibleTypeNames.Contains(bi.Type))
                && !context.BaseItems.Any(bs => bs.Id == id && bs.Type == boxSetTypeName)
                && !context.LinkedChildren.Any(lc =>
                    lc.ChildId == id
                    && lc.ChildType == DbLinkedChildType.Manual
                    && context.BaseItems.Any(bs => bs.Id == lc.ParentId && bs.Type == boxSetTypeName)));

        // Box set IDs containing at least one accessible collapsible child item
        var boxSetIds = context.LinkedChildren
            .Where(lc =>
                lc.ChildType == DbLinkedChildType.Manual
                && currentIds.Contains(lc.ChildId)
                && context.BaseItems.Any(bi => bi.Id == lc.ChildId && collapsibleTypeNames.Contains(bi.Type))
                && context.BaseItems.Any(bs => bs.Id == lc.ParentId && bs.Type == boxSetTypeName))
            .Select(lc => lc.ParentId)
            .Distinct();

        var collapsedIds = nonCollapsibleIds.Union(collapsibleNotInBoxSet).Union(boxSetIds);
        return context.BaseItems.Where(e => collapsedIds.Contains(e.Id));
    }

    private static IQueryable<BaseItemEntity> ApplyBoxSetCollapsingAll(
        JellyfinDbContext context,
        IQueryable<Guid> currentIds,
        string boxSetTypeName)
    {
        // Items that are NOT box sets and NOT in any box set
        var notInBoxSet = currentIds
            .Where(id =>
                !context.BaseItems.Any(bs => bs.Id == id && bs.Type == boxSetTypeName)
                && !context.LinkedChildren.Any(lc =>
                    lc.ChildId == id
                    && lc.ChildType == DbLinkedChildType.Manual
                    && context.BaseItems.Any(bs => bs.Id == lc.ParentId && bs.Type == boxSetTypeName)));

        // Box set IDs containing at least one accessible child item
        var boxSetIds = context.LinkedChildren
            .Where(lc =>
                lc.ChildType == DbLinkedChildType.Manual
                && currentIds.Contains(lc.ChildId)
                && context.BaseItems.Any(bs => bs.Id == lc.ParentId && bs.Type == boxSetTypeName))
            .Select(lc => lc.ParentId)
            .Distinct();

        var collapsedIds = notInBoxSet.Union(boxSetIds);
        return context.BaseItems.Where(e => collapsedIds.Contains(e.Id));
    }

    private static IQueryable<BaseItemEntity> ApplyNavigations(IQueryable<BaseItemEntity> dbQuery, InternalItemsQuery filter)
    {
        if (filter.TrailerTypes.Length > 0 || filter.IncludeItemTypes.Contains(BaseItemKind.Trailer))
        {
            dbQuery = dbQuery.Include(e => e.TrailerTypes);
        }

        if (filter.DtoOptions.ContainsField(ItemFields.ProviderIds))
        {
            dbQuery = dbQuery.Include(e => e.Provider);
        }

        if (filter.DtoOptions.ContainsField(ItemFields.Settings))
        {
            dbQuery = dbQuery.Include(e => e.LockedFields);
        }

        if (filter.DtoOptions.EnableUserData)
        {
            dbQuery = dbQuery.Include(e => e.UserData);
        }

        if (filter.DtoOptions.EnableImages)
        {
            dbQuery = dbQuery.Include(e => e.Images);
        }

        // Include LinkedChildEntities for container types and videos that use them
        // (BoxSet, Playlist, CollectionFolder for manual linking; Video, Movie for alternate versions).
        // When IncludeItemTypes is empty (any type may be returned), always include them to ensure
        // LinkedChildren are loaded before items are saved back, preventing accidental deletion.
        var linkedChildTypes = new[]
        {
            BaseItemKind.BoxSet,
            BaseItemKind.Playlist,
            BaseItemKind.CollectionFolder,
            BaseItemKind.Video,
            BaseItemKind.Movie
        };
        if (filter.IncludeItemTypes.Length == 0 || filter.IncludeItemTypes.Any(linkedChildTypes.Contains))
        {
            dbQuery = dbQuery.Include(e => e.LinkedChildEntities);
        }

        if (filter.IncludeExtras)
        {
            dbQuery = dbQuery.Include(e => e.Extras);
        }

        return dbQuery;
    }

    private IQueryable<BaseItemEntity> ApplyQueryPaging(IQueryable<BaseItemEntity> dbQuery, InternalItemsQuery filter)
    {
        if (filter.Limit.HasValue || filter.StartIndex.HasValue)
        {
            var offset = filter.StartIndex ?? 0;

            if (offset > 0)
            {
                dbQuery = dbQuery.Skip(offset);
            }

            if (filter.Limit.HasValue)
            {
                dbQuery = dbQuery.Take(filter.Limit.Value);
            }
        }

        return dbQuery;
    }

    private IQueryable<BaseItemEntity> ApplyQueryFilter(IQueryable<BaseItemEntity> dbQuery, JellyfinDbContext context, InternalItemsQuery filter)
    {
        dbQuery = TranslateQuery(dbQuery, context, filter);
        dbQuery = ApplyGroupingFilter(context, dbQuery, filter);
        dbQuery = ApplyQueryPaging(dbQuery, filter);
        dbQuery = ApplyNavigations(dbQuery, filter).AsSplitQuery();
        return dbQuery;
    }

    private IQueryable<BaseItemEntity> PrepareItemQuery(JellyfinDbContext context, InternalItemsQuery filter)
    {
        IQueryable<BaseItemEntity> dbQuery = context.BaseItems.AsNoTracking();
        dbQuery = dbQuery.AsSingleQuery();

        return dbQuery;
    }

    /// <inheritdoc/>
    public int GetCount(InternalItemsQuery filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        // Hack for right now since we currently don't support filtering out these duplicates within a query
        PrepareFilterQuery(filter);

        using var context = _dbProvider.CreateDbContext();
        var dbQuery = TranslateQuery(context.BaseItems.AsNoTracking(), context, filter);

        return dbQuery.Count();
    }

    /// <inheritdoc />
    public QueryFiltersLegacy GetQueryFiltersLegacy(InternalItemsQuery filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        PrepareFilterQuery(filter);

        using var context = _dbProvider.CreateDbContext();
        var baseQuery = PrepareItemQuery(context, filter);
        baseQuery = TranslateQuery(baseQuery, context, filter);

        // Get matching item IDs as a subquery (not materialized)
        var matchingItemIds = baseQuery.Select(e => e.Id);

        // Query distinct years directly from the database
        var years = baseQuery
            .Where(e => e.ProductionYear != null && e.ProductionYear > 0)
            .Select(e => e.ProductionYear!.Value)
            .Distinct()
            .OrderBy(y => y)
            .ToArray();

        // Query distinct official ratings directly from the database
        var officialRatings = baseQuery
            .Where(e => e.OfficialRating != null && e.OfficialRating != string.Empty)
            .Select(e => e.OfficialRating!)
            .Distinct()
            .OrderBy(r => r)
            .ToArray();

        // Tags via ItemValuesMap JOIN - uses subquery for matching items
        var tags = context.ItemValuesMap
            .Where(ivm => ivm.ItemValue.Type == ItemValueType.Tags)
            .Where(ivm => matchingItemIds.Contains(ivm.ItemId))
            .Select(ivm => ivm.ItemValue.CleanValue)
            .Distinct()
            .OrderBy(t => t)
            .ToArray();

        // Genres via ItemValuesMap JOIN - uses subquery for matching items
        var genres = context.ItemValuesMap
            .Where(ivm => ivm.ItemValue.Type == ItemValueType.Genre)
            .Where(ivm => matchingItemIds.Contains(ivm.ItemId))
            .Select(ivm => ivm.ItemValue.CleanValue)
            .Distinct()
            .OrderBy(g => g)
            .ToArray();

        return new QueryFiltersLegacy
        {
            Years = years,
            OfficialRatings = officialRatings,
            Tags = tags,
            Genres = genres
        };
    }

    /// <inheritdoc />
    public ItemCounts GetItemCounts(InternalItemsQuery filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        // Hack for right now since we currently don't support filtering out these duplicates within a query
        PrepareFilterQuery(filter);

        using var context = _dbProvider.CreateDbContext();
        var dbQuery = TranslateQuery(context.BaseItems.AsNoTracking(), context, filter);

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

#pragma warning disable CA1307 // Specify StringComparison for clarity
    /// <summary>
    /// Gets the type.
    /// </summary>
    /// <param name="typeName">Name of the type.</param>
    /// <returns>Type.</returns>
    /// <exception cref="ArgumentNullException"><c>typeName</c> is null.</exception>
    private static Type? GetType(string typeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);

        // TODO: this isn't great. Refactor later to be both globally handled by a dedicated service not just an static variable and be loaded eagerly.
        // currently this is done so that plugins may introduce their own type of baseitems as we dont know when we are first called, before or after plugins are loaded
        return _typeMap.GetOrAdd(typeName, k => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(k))
            .FirstOrDefault(t => t is not null));
    }

    /// <inheritdoc  />
    public void SaveItems(IReadOnlyList<BaseItemDto> items, CancellationToken cancellationToken)
    {
        UpdateOrInsertItems(items, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveImagesAsync(BaseItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var images = item.ImageInfos.Select(e => Map(item.Id, e)).ToArray();

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

    /// <inheritdoc cref="IItemRepository"/>
    public void UpdateOrInsertItems(IReadOnlyList<BaseItemDto> items, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        cancellationToken.ThrowIfCancellationRequested();

        var tuples = new List<(BaseItemDto Item, List<Guid>? AncestorIds, BaseItemDto TopParent, IEnumerable<string> UserDataKey, List<string> InheritedTags)>();
        foreach (var item in items.GroupBy(e => e.Id).Select(e => e.Last()).Where(e => e.Id != PlaceholderId))
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
            var entity = Map(item.Item);
            // TODO: refactor this "inconsistency"
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
                    // map exists, remove from list so its been handled.
                    itemMappedValues.Remove(existingItem);
                }
            }

            // all still listed values are not in the new list so remove them.
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

        // This is necessary because LocalAlternateVersions resolution queries the database by path,
        // and newly imported alternate version items need to exist before we can link them
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
                            .AsEnumerable()
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

                    // Deduplicate by ChildId, keeping the last occurrence (for playlist ordering)
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

            // Handle Video alternate versions
            if (item.Item is Video video)
            {
                // Use batch-fetched data and filter for alternate version types (2 = LocalAlternateVersion, 3 = LinkedAlternateVersion)
                var existingLinkedChildren = (allLinkedChildrenByParent.GetValueOrDefault(video.Id) ?? new List<LinkedChildEntity>())
                    .Where(e => (int)e.ChildType == 2 || (int)e.ChildType == 3)
                    .ToList();

                var newLinkedChildren = new List<(Guid ChildId, LinkedChildType Type)>();

                // Process LocalAlternateVersions (path-based alternate versions)
                if (video.LocalAlternateVersions.Length > 0)
                {
                    var pathsToResolve = video.LocalAlternateVersions.Where(p => !string.IsNullOrEmpty(p)).ToList();
                    if (pathsToResolve.Count > 0)
                    {
                        var pathToIdMap = context.BaseItems
                            .Where(e => e.Path != null && pathsToResolve.Contains(e.Path))
                            .Select(e => new { e.Path, e.Id })
                            .AsEnumerable()
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

                // Process LinkedAlternateVersions (ID-based alternate versions)
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

                // Deduplicate by ChildId, keeping the last occurrence
                newLinkedChildren = newLinkedChildren
                    .GroupBy(c => c.ChildId)
                    .Select(g => g.Last())
                    .ToList();

                // Validate that all child items exist
                var childIdsToCheck = newLinkedChildren.Select(c => c.ChildId).ToList();
                var existingChildIds = childIdsToCheck.Count > 0
                    ? context.BaseItems
                        .Where(e => childIdsToCheck.Contains(e.Id))
                        .Select(e => e.Id)
                        .ToHashSet()
                    : [];

                // Add or update LinkedChildren entries
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
                            SortOrder = null
                        });
                    }
                    else
                    {
                        existingLink.ChildType = (DbLinkedChildType)childType;
                        existingLinkedChildren.Remove(existingLink);
                    }
                }

                // Remove orphaned alternate version links and their items
                if (existingLinkedChildren.Count > 0)
                {
                    // Get the child IDs of LocalAlternateVersions that are being removed
                    // These items should be deleted as they are owned by this video
                    var orphanedLocalVersionIds = existingLinkedChildren
                        .Where(e => e.ChildType == DbLinkedChildType.LocalAlternateVersion)
                        .Select(e => e.ChildId)
                        .ToList();

                    context.LinkedChildren.RemoveRange(existingLinkedChildren);

                    // Delete the orphaned LocalAlternateVersion items themselves
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

        // Phase 2 commit: Save LinkedChildren changes
        context.SaveChanges();
        transaction.Commit();
    }

    /// <summary>
    /// Reattaches user data entries that were incorrectly associated with a different item.
    /// </summary>
    /// <param name="item">The item DTO to reattach user data for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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
                    .Where(e => e.ItemId == PlaceholderId)
                    .Where(e => userKeys.Contains(e.CustomDataKey))
                    .ExecuteUpdateAsync(
                        e => e
                            .SetProperty(f => f.ItemId, item.Id)
                            .SetProperty(f => f.RetentionDate, retentionDate),
                        cancellationToken).ConfigureAwait(false);

                // Rehydrate the cached userdata
                item.UserData = await dbContext.UserData
                    .AsNoTracking()
                    .Where(e => e.ItemId == item.Id)
                    .ToArrayAsync(cancellationToken)
                    .ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc  />
    public BaseItemDto? RetrieveItem(Guid id)
    {
        if (id.IsEmpty())
        {
            throw new ArgumentException("Guid can't be empty", nameof(id));
        }

        using var context = _dbProvider.CreateDbContext();
        var dbQuery = PrepareItemQuery(context, new()
        {
            DtoOptions = new()
            {
                EnableImages = true
            }
        });
        dbQuery = dbQuery.Include(e => e.TrailerTypes)
            .Include(e => e.Provider)
            .Include(e => e.LockedFields)
            .Include(e => e.UserData)
            .Include(e => e.Images)
            .Include(e => e.LinkedChildEntities)
            .AsSingleQuery();

        var item = dbQuery.FirstOrDefault(e => e.Id == id);
        if (item is null)
        {
            return null;
        }

        return DeserializeBaseItem(item);
    }

    /// <summary>
    /// Maps a Entity to the DTO.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="dto">The dto base instance.</param>
    /// <param name="appHost">The Application server Host.</param>
    /// <param name="logger">The applogger.</param>
    /// <returns>The dto to map.</returns>
    public static BaseItemDto Map(BaseItemEntity entity, BaseItemDto dto, IServerApplicationHost? appHost, ILogger logger)
    {
        dto.Id = entity.Id;
        dto.ParentId = entity.ParentId.GetValueOrDefault();
        dto.Path = appHost?.ExpandVirtualPath(entity.Path) ?? entity.Path;
        dto.EndDate = entity.EndDate;
        dto.CommunityRating = entity.CommunityRating;
        dto.CustomRating = entity.CustomRating;
        dto.IndexNumber = entity.IndexNumber;
        dto.IsLocked = entity.IsLocked;
        dto.Name = entity.Name;
        dto.OfficialRating = entity.OfficialRating;
        dto.Overview = entity.Overview;
        dto.ParentIndexNumber = entity.ParentIndexNumber;
        dto.PremiereDate = entity.PremiereDate;
        dto.ProductionYear = entity.ProductionYear;
        dto.SortName = entity.SortName;
        dto.ForcedSortName = entity.ForcedSortName;
        dto.RunTimeTicks = entity.RunTimeTicks;
        dto.PreferredMetadataLanguage = entity.PreferredMetadataLanguage;
        dto.PreferredMetadataCountryCode = entity.PreferredMetadataCountryCode;
        dto.IsInMixedFolder = entity.IsInMixedFolder;
        dto.InheritedParentalRatingValue = entity.InheritedParentalRatingValue;
        dto.InheritedParentalRatingSubValue = entity.InheritedParentalRatingSubValue;
        dto.CriticRating = entity.CriticRating;
        dto.PresentationUniqueKey = entity.PresentationUniqueKey;
        dto.OriginalTitle = entity.OriginalTitle;
        dto.Album = entity.Album;
        dto.LUFS = entity.LUFS;
        dto.NormalizationGain = entity.NormalizationGain;
        dto.IsVirtualItem = entity.IsVirtualItem;
        dto.ExternalSeriesId = entity.ExternalSeriesId;
        dto.Tagline = entity.Tagline;
        dto.TotalBitrate = entity.TotalBitrate;
        dto.ExternalId = entity.ExternalId;
        dto.Size = entity.Size;
        dto.Genres = string.IsNullOrWhiteSpace(entity.Genres) ? [] : entity.Genres.Split('|');
        dto.DateCreated = entity.DateCreated ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        dto.DateModified = entity.DateModified ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        dto.ChannelId = entity.ChannelId ?? Guid.Empty;
        dto.DateLastRefreshed = entity.DateLastRefreshed ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        dto.DateLastSaved = entity.DateLastSaved ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        dto.OwnerId = entity.OwnerId ?? Guid.Empty;
        dto.Width = entity.Width.GetValueOrDefault();
        dto.Height = entity.Height.GetValueOrDefault();
        dto.UserData = entity.UserData;

        if (entity.Provider is not null)
        {
            dto.ProviderIds = entity.Provider.ToDictionary(e => e.ProviderId, e => e.ProviderValue);
        }

        if (entity.ExtraType is not null)
        {
            dto.ExtraType = (ExtraType)entity.ExtraType;
        }

        if (entity.LockedFields is not null)
        {
            dto.LockedFields = entity.LockedFields?.Select(e => (MetadataField)e.Id).ToArray() ?? [];
        }

        if (entity.Audio is not null)
        {
            dto.Audio = (ProgramAudio)entity.Audio;
        }

        dto.ProductionLocations = entity.ProductionLocations?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        dto.Studios = entity.Studios?.Split('|') ?? [];
        dto.Tags = string.IsNullOrWhiteSpace(entity.Tags) ? [] : entity.Tags.Split('|');

        if (dto is IHasProgramAttributes hasProgramAttributes)
        {
            hasProgramAttributes.IsMovie = entity.IsMovie;
            hasProgramAttributes.IsSeries = entity.IsSeries;
            hasProgramAttributes.EpisodeTitle = entity.EpisodeTitle;
            hasProgramAttributes.IsRepeat = entity.IsRepeat;
        }

        if (dto is LiveTvChannel liveTvChannel)
        {
            liveTvChannel.ServiceName = entity.ExternalServiceId;
        }

        if (dto is Trailer trailer)
        {
            trailer.TrailerTypes = entity.TrailerTypes?.Select(e => (TrailerType)e.Id).ToArray() ?? [];
        }

        if (dto is Video video)
        {
            video.PrimaryVersionId = entity.PrimaryVersionId;
        }

        if (dto is IHasSeries hasSeriesName)
        {
            hasSeriesName.SeriesName = entity.SeriesName;
            hasSeriesName.SeriesId = entity.SeriesId.GetValueOrDefault();
            hasSeriesName.SeriesPresentationUniqueKey = entity.SeriesPresentationUniqueKey;
        }

        if (dto is Episode episode)
        {
            episode.SeasonName = entity.SeasonName;
            episode.SeasonId = entity.SeasonId.GetValueOrDefault();
        }

        if (dto is IHasArtist hasArtists)
        {
            hasArtists.Artists = entity.Artists?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        }

        if (dto is IHasAlbumArtist hasAlbumArtists)
        {
            hasAlbumArtists.AlbumArtists = entity.AlbumArtists?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        }

        if (dto is LiveTvProgram program)
        {
            program.ShowId = entity.ShowId;
        }

        if (entity.Images is not null)
        {
            dto.ImageInfos = entity.Images.Select(e => Map(e, appHost)).ToArray();
        }

        // dto.Type = entity.Type;
        // dto.Data = entity.Data;
        // dto.MediaType = Enum.TryParse<MediaType>(entity.MediaType);
        if (dto is IHasStartDate hasStartDate)
        {
            hasStartDate.StartDate = entity.StartDate.GetValueOrDefault();
        }

        // Fields that are present in the DB but are never actually used
        // dto.UnratedType = entity.UnratedType;
        // dto.TopParentId = entity.TopParentId;
        // dto.CleanName = entity.CleanName;
        // dto.UserDataKey = entity.UserDataKey;

        if (dto is Folder folder)
        {
            folder.DateLastMediaAdded = entity.DateLastMediaAdded ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            if (entity.LinkedChildEntities is not null && entity.LinkedChildEntities.Count > 0)
            {
                folder.LinkedChildren = entity.LinkedChildEntities
                    .OrderBy(e => e.SortOrder)
                    .Select(e => new LinkedChild
                    {
                        ItemId = e.ChildId,
                        Type = (LinkedChildType)e.ChildType
                    })
                    .ToArray();
            }
        }

        return dto;
    }

    /// <summary>
    /// Maps a Entity to the DTO.
    /// </summary>
    /// <param name="dto">The entity.</param>
    /// <returns>The dto to map.</returns>
    public BaseItemEntity Map(BaseItemDto dto)
    {
        var dtoType = dto.GetType();
        var entity = new BaseItemEntity()
        {
            Type = dtoType.ToString(),
            Id = dto.Id
        };

        if (TypeRequiresDeserialization(dtoType))
        {
            entity.Data = JsonSerializer.Serialize(dto, dtoType, JsonDefaults.Options);
        }

        entity.ParentId = !dto.ParentId.IsEmpty() ? dto.ParentId : null;
        entity.Path = GetPathToSave(dto.Path);
        entity.EndDate = dto.EndDate;
        entity.CommunityRating = dto.CommunityRating;
        entity.CustomRating = dto.CustomRating;
        entity.IndexNumber = dto.IndexNumber;
        entity.IsLocked = dto.IsLocked;
        entity.Name = dto.Name;
        entity.CleanName = dto.Name.GetCleanValue();
        entity.OfficialRating = dto.OfficialRating;
        entity.Overview = dto.Overview;
        entity.ParentIndexNumber = dto.ParentIndexNumber;
        entity.PremiereDate = dto.PremiereDate;
        entity.ProductionYear = dto.ProductionYear;
        entity.SortName = dto.SortName;
        entity.ForcedSortName = dto.ForcedSortName;
        entity.RunTimeTicks = dto.RunTimeTicks;
        entity.PreferredMetadataLanguage = dto.PreferredMetadataLanguage;
        entity.PreferredMetadataCountryCode = dto.PreferredMetadataCountryCode;
        entity.IsInMixedFolder = dto.IsInMixedFolder;
        entity.InheritedParentalRatingValue = dto.InheritedParentalRatingValue;
        entity.InheritedParentalRatingSubValue = dto.InheritedParentalRatingSubValue;
        entity.CriticRating = dto.CriticRating;
        entity.PresentationUniqueKey = dto.PresentationUniqueKey;
        entity.OriginalTitle = dto.OriginalTitle;
        entity.Album = dto.Album;
        entity.LUFS = dto.LUFS;
        entity.NormalizationGain = dto.NormalizationGain;
        entity.IsVirtualItem = dto.IsVirtualItem;
        entity.ExternalSeriesId = dto.ExternalSeriesId;
        entity.Tagline = dto.Tagline;
        entity.TotalBitrate = dto.TotalBitrate;
        entity.ExternalId = dto.ExternalId;
        entity.Size = dto.Size;
        entity.Genres = string.Join('|', dto.Genres);
        entity.DateCreated = dto.DateCreated == DateTime.MinValue ? null : dto.DateCreated;
        entity.DateModified = dto.DateModified == DateTime.MinValue ? null : dto.DateModified;
        entity.ChannelId = dto.ChannelId;
        entity.DateLastRefreshed = dto.DateLastRefreshed == DateTime.MinValue ? null : dto.DateLastRefreshed;
        entity.DateLastSaved = dto.DateLastSaved == DateTime.MinValue ? null : dto.DateLastSaved;
        entity.OwnerId = dto.OwnerId == Guid.Empty ? null : dto.OwnerId;
        entity.Width = dto.Width;
        entity.Height = dto.Height;
        entity.Provider = dto.ProviderIds.Select(e => new BaseItemProvider()
        {
            Item = entity,
            ProviderId = e.Key,
            ProviderValue = e.Value
        }).ToList();

        if (dto.Audio.HasValue)
        {
            entity.Audio = (ProgramAudioEntity)dto.Audio;
        }

        if (dto.ExtraType.HasValue)
        {
            entity.ExtraType = (BaseItemExtraType)dto.ExtraType;
        }

        entity.ProductionLocations = dto.ProductionLocations is not null ? string.Join('|', dto.ProductionLocations.Where(p => !string.IsNullOrWhiteSpace(p))) : null;
        entity.Studios = dto.Studios is not null ? string.Join('|', dto.Studios) : null;
        entity.Tags = dto.Tags is not null ? string.Join('|', dto.Tags) : null;
        entity.LockedFields = dto.LockedFields is not null ? dto.LockedFields
            .Select(e => new BaseItemMetadataField()
            {
                Id = (int)e,
                Item = entity,
                ItemId = entity.Id
            })
            .ToArray() : null;

        if (dto is IHasProgramAttributes hasProgramAttributes)
        {
            entity.IsMovie = hasProgramAttributes.IsMovie;
            entity.IsSeries = hasProgramAttributes.IsSeries;
            entity.EpisodeTitle = hasProgramAttributes.EpisodeTitle;
            entity.IsRepeat = hasProgramAttributes.IsRepeat;
        }

        if (dto is LiveTvChannel liveTvChannel)
        {
            entity.ExternalServiceId = liveTvChannel.ServiceName;
        }

        if (dto is Video video)
        {
            entity.PrimaryVersionId = video.PrimaryVersionId;
        }

        if (dto is IHasSeries hasSeriesName)
        {
            entity.SeriesName = hasSeriesName.SeriesName;
            entity.SeriesId = hasSeriesName.SeriesId;
            entity.SeriesPresentationUniqueKey = hasSeriesName.SeriesPresentationUniqueKey;
        }

        if (dto is Episode episode)
        {
            entity.SeasonName = episode.SeasonName;
            entity.SeasonId = episode.SeasonId;
        }

        if (dto is IHasArtist hasArtists)
        {
            entity.Artists = hasArtists.Artists is not null ? string.Join('|', hasArtists.Artists) : null;
        }

        if (dto is IHasAlbumArtist hasAlbumArtists)
        {
            entity.AlbumArtists = hasAlbumArtists.AlbumArtists is not null ? string.Join('|', hasAlbumArtists.AlbumArtists) : null;
        }

        if (dto is LiveTvProgram program)
        {
            entity.ShowId = program.ShowId;
        }

        if (dto.ImageInfos is not null)
        {
            entity.Images = dto.ImageInfos.Select(f => Map(dto.Id, f)).ToArray();
        }

        if (dto is Trailer trailer)
        {
            entity.TrailerTypes = trailer.TrailerTypes?.Select(e => new BaseItemTrailerType()
            {
                Id = (int)e,
                Item = entity,
                ItemId = entity.Id
            }).ToArray() ?? [];
        }

        // dto.Type = entity.Type;
        // dto.Data = entity.Data;
        entity.MediaType = dto.MediaType.ToString();
        if (dto is IHasStartDate hasStartDate)
        {
            entity.StartDate = hasStartDate.StartDate;
        }

        entity.UnratedType = dto.GetBlockUnratedType().ToString();

        // Fields that are present in the DB but are never actually used
        // dto.UserDataKey = entity.UserDataKey;

        if (dto is Folder folder)
        {
            entity.DateLastMediaAdded = folder.DateLastMediaAdded == DateTime.MinValue ? null : folder.DateLastMediaAdded;
            entity.IsFolder = folder.IsFolder;
        }

        return entity;
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

        // query = query.DistinctBy(e => e.CleanValue);
        return query.Select(e => e.ItemValue)
            .GroupBy(e => e.CleanValue)
            .Select(e => e.OrderBy(v => v.Value).First().Value)
            .ToArray();
    }

    private static bool TypeRequiresDeserialization(Type type)
    {
        return type.GetCustomAttribute<RequiresSourceSerialisationAttribute>() == null;
    }

    private BaseItemDto? DeserializeBaseItem(BaseItemEntity baseItemEntity, bool skipDeserialization = false)
    {
        ArgumentNullException.ThrowIfNull(baseItemEntity, nameof(baseItemEntity));
        if (_serverConfigurationManager?.Configuration is null)
        {
            throw new InvalidOperationException("Server Configuration manager or configuration is null");
        }

        var typeToSerialise = GetType(baseItemEntity.Type);
        return BaseItemRepository.DeserializeBaseItem(
            baseItemEntity,
            _logger,
            _appHost,
            skipDeserialization || (_serverConfigurationManager.Configuration.SkipDeserializationForBasicTypes && (typeToSerialise == typeof(Channel) || typeToSerialise == typeof(UserRootFolder))));
    }

    /// <summary>
    /// Deserializes a BaseItemEntity and sets all properties.
    /// </summary>
    /// <param name="baseItemEntity">The DB entity.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="appHost">The application server Host.</param>
    /// <param name="skipDeserialization">If only mapping should be processed.</param>
    /// <returns>A mapped BaseItem, or null if the item type is unknown.</returns>
    public static BaseItemDto? DeserializeBaseItem(BaseItemEntity baseItemEntity, ILogger logger, IServerApplicationHost? appHost, bool skipDeserialization = false)
    {
        var type = GetType(baseItemEntity.Type);
        if (type is null)
        {
            logger.LogWarning(
                "Skipping item {ItemId} with unknown type '{ItemType}'. This may indicate a removed plugin or database corruption.",
                baseItemEntity.Id,
                baseItemEntity.Type);
            return null;
        }

        BaseItemDto? dto = null;
        if (TypeRequiresDeserialization(type) && baseItemEntity.Data is not null && !skipDeserialization)
        {
            try
            {
                dto = JsonSerializer.Deserialize(baseItemEntity.Data, type, JsonDefaults.Options) as BaseItemDto;
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Error deserializing item with JSON: {Data}", baseItemEntity.Data);
            }
        }

        if (dto is null)
        {
            dto = Activator.CreateInstance(type) as BaseItemDto ?? throw new InvalidOperationException("Cannot deserialize unknown type.");
        }

        return Map(baseItemEntity, dto, appHost, logger);
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
        var itemValuesQuery = context.ItemValuesMap
            .Where(ivm => itemValueTypes.Contains(ivm.ItemValue.Type))
            .Join(
                innerQueryFilter,
                ivm => ivm.ItemId,
                g => g.Id,
                (ivm, g) => ivm.ItemValue.CleanValue);

        var innerQuery = PrepareItemQuery(context, filter)
            .Where(e => e.Type == returnType)
            .Where(e => itemValuesQuery.Contains(e.CleanName));

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

        var masterQuery = TranslateQuery(innerQuery, context, outerQueryFilter)
            .GroupBy(e => e.PresentationUniqueKey)
            .Select(e => e.OrderBy(x => x.Id).FirstOrDefault())
            .Select(e => e!.Id);

        var query = context.BaseItems
            .Include(e => e.TrailerTypes)
            .Include(e => e.Provider)
            .Include(e => e.LockedFields)
            .Include(e => e.Images)
            .Include(e => e.LinkedChildEntities)
            .AsSingleQuery()
            .Where(e => masterQuery.Contains(e.Id));

        query = ApplyOrder(query, filter, context);

        var result = new QueryResult<(BaseItemDto, ItemCounts?)>();
        if (filter.EnableTotalRecordCount)
        {
            result.TotalRecordCount = query.Count();
        }

        if (filter.Limit.HasValue || filter.StartIndex.HasValue)
        {
            var offset = filter.StartIndex ?? 0;

            if (offset > 0)
            {
                query = query.Skip(offset);
            }

            if (filter.Limit.HasValue)
            {
                query = query.Take(filter.Limit.Value);
            }
        }

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

            // Get the IDs from itemCountQuery to use in the join
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
                    .Where(e => e is not null)
                    .Select(e => DeserializeBaseItem(e, filter.SkipDeserialization))
                    .Where(item => item is not null)
                    .Select(item => (item!, (ItemCounts?)null))
            ];
        }

        return result;
    }

    private static void PrepareFilterQuery(InternalItemsQuery query)
    {
        if (query.Limit.HasValue && query.EnableGroupByMetadataKey)
        {
            query.Limit = query.Limit.Value + 4;
        }

        if (query.IsResumable ?? false)
        {
            query.IsVirtualItem = false;
        }
    }

    private List<(ItemValueType MagicNumber, string Value)> GetItemValuesToSave(BaseItemDto item, List<string> inheritedTags)
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

        // keywords was 5

        list.AddRange(inheritedTags.Select(i => (ItemValueType.InheritedTags, i)));

        // Remove all invalid values.
        list.RemoveAll(i => string.IsNullOrWhiteSpace(i.Item2));

        return list;
    }

    private static BaseItemImageInfo Map(Guid baseItemId, ItemImageInfo e)
    {
        return new BaseItemImageInfo()
        {
            ItemId = baseItemId,
            Id = Guid.NewGuid(),
            Path = e.Path,
            Blurhash = e.BlurHash is null ? null : Encoding.UTF8.GetBytes(e.BlurHash),
            DateModified = e.DateModified,
            Height = e.Height,
            Width = e.Width,
            ImageType = (ImageInfoImageType)e.Type,
            Item = null!
        };
    }

    private static ItemImageInfo Map(BaseItemImageInfo e, IServerApplicationHost? appHost)
    {
        return new ItemImageInfo()
        {
            Path = appHost?.ExpandVirtualPath(e.Path) ?? e.Path,
            BlurHash = e.Blurhash is null ? null : Encoding.UTF8.GetString(e.Blurhash),
            DateModified = e.DateModified ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
            Height = e.Height,
            Width = e.Width,
            Type = (ImageType)e.ImageType
        };
    }

    private string? GetPathToSave(string path)
    {
        if (path is null)
        {
            return null;
        }

        return _appHost.ReverseVirtualPath(path);
    }

    private List<string> GetItemByNameTypesInQuery(InternalItemsQuery query)
    {
        var list = new List<string>();

        if (IsTypeInQuery(BaseItemKind.Person, query))
        {
            list.Add(_itemTypeLookup.BaseItemKindNames[BaseItemKind.Person]!);
        }

        if (IsTypeInQuery(BaseItemKind.Genre, query))
        {
            list.Add(_itemTypeLookup.BaseItemKindNames[BaseItemKind.Genre]!);
        }

        if (IsTypeInQuery(BaseItemKind.MusicGenre, query))
        {
            list.Add(_itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicGenre]!);
        }

        if (IsTypeInQuery(BaseItemKind.MusicArtist, query))
        {
            list.Add(_itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist]!);
        }

        if (IsTypeInQuery(BaseItemKind.Studio, query))
        {
            list.Add(_itemTypeLookup.BaseItemKindNames[BaseItemKind.Studio]!);
        }

        return list;
    }

    private bool IsTypeInQuery(BaseItemKind type, InternalItemsQuery query)
    {
        if (query.ExcludeItemTypes.Contains(type))
        {
            return false;
        }

        return query.IncludeItemTypes.Length == 0 || query.IncludeItemTypes.Contains(type);
    }

    private bool EnableGroupByPresentationUniqueKey(InternalItemsQuery query)
    {
        if (!query.GroupByPresentationUniqueKey)
        {
            return false;
        }

        if (query.GroupBySeriesPresentationUniqueKey)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.PresentationUniqueKey))
        {
            return false;
        }

        if (query.User is null)
        {
            return false;
        }

        if (query.IncludeItemTypes.Length == 0)
        {
            return true;
        }

        return query.IncludeItemTypes.Contains(BaseItemKind.Episode)
            || query.IncludeItemTypes.Contains(BaseItemKind.Video)
            || query.IncludeItemTypes.Contains(BaseItemKind.Movie)
            || query.IncludeItemTypes.Contains(BaseItemKind.MusicVideo)
            || query.IncludeItemTypes.Contains(BaseItemKind.Series)
            || query.IncludeItemTypes.Contains(BaseItemKind.Season);
    }

    private IQueryable<BaseItemEntity> ApplyOrder(IQueryable<BaseItemEntity> query, InternalItemsQuery filter, JellyfinDbContext context)
    {
        var orderBy = filter.OrderBy.Where(e => e.OrderBy != ItemSortBy.Default).ToArray();
        var hasSearch = !string.IsNullOrEmpty(filter.SearchTerm);

        IOrderedQueryable<BaseItemEntity>? orderedQuery = null;

        if (hasSearch)
        {
            var relevanceExpression = OrderMapper.MapSearchRelevanceOrder(filter.SearchTerm!);
            orderedQuery = query.OrderBy(relevanceExpression);
        }

        if (orderBy.Length > 0)
        {
            var firstOrdering = orderBy[0];
            var expression = OrderMapper.MapOrderByField(firstOrdering.OrderBy, filter, context);

            if (orderedQuery is null)
            {
                orderedQuery = firstOrdering.SortOrder == SortOrder.Ascending
                    ? query.OrderBy(expression)
                    : query.OrderByDescending(expression);
            }
            else
            {
                orderedQuery = firstOrdering.SortOrder == SortOrder.Ascending
                    ? orderedQuery.ThenBy(expression)
                    : orderedQuery.ThenByDescending(expression);
            }

            if (firstOrdering.OrderBy is ItemSortBy.Default or ItemSortBy.SortName)
            {
                orderedQuery = firstOrdering.SortOrder == SortOrder.Ascending
                    ? orderedQuery.ThenBy(e => e.Name)
                    : orderedQuery.ThenByDescending(e => e.Name);
            }

            foreach (var item in orderBy.Skip(1))
            {
                expression = OrderMapper.MapOrderByField(item.OrderBy, filter, context);
                orderedQuery = item.SortOrder == SortOrder.Ascending
                    ? orderedQuery.ThenBy(expression)
                    : orderedQuery.ThenByDescending(expression);
            }
        }

        if (orderedQuery is null)
        {
            return query.OrderBy(e => e.SortName);
        }

        // Add SortName as final tiebreaker
        if (!hasSearch && (orderBy.Length == 0 || orderBy.All(o => o.OrderBy is not ItemSortBy.SortName and not ItemSortBy.Name)))
        {
            orderedQuery = orderedQuery.ThenBy(e => e.SortName);
        }

        return orderedQuery;
    }

    private IQueryable<BaseItemEntity> TranslateQuery(
        IQueryable<BaseItemEntity> baseQuery,
        JellyfinDbContext context,
        InternalItemsQuery filter)
    {
        const int HDWidth = 1200;
        const int UHDWidth = 3800;
        const int UHDHeight = 2100;

        var minWidth = filter.MinWidth;
        var maxWidth = filter.MaxWidth;
        var now = DateTime.UtcNow;

        if (filter.IsHD.HasValue || filter.Is4K.HasValue)
        {
            bool includeSD = false;
            bool includeHD = false;
            bool include4K = false;

            if (filter.IsHD.HasValue && !filter.IsHD.Value)
            {
                includeSD = true;
            }

            if (filter.IsHD.HasValue && filter.IsHD.Value)
            {
                includeHD = true;
            }

            if (filter.Is4K.HasValue && filter.Is4K.Value)
            {
                include4K = true;
            }

            baseQuery = baseQuery.Where(e =>
                (includeSD && e.Width < HDWidth) ||
                (includeHD && e.Width >= HDWidth && !(e.Width >= UHDWidth || e.Height >= UHDHeight)) ||
                (include4K && (e.Width >= UHDWidth || e.Height >= UHDHeight)));
        }

        if (minWidth.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Width >= minWidth);
        }

        if (filter.MinHeight.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Height >= filter.MinHeight);
        }

        if (maxWidth.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Width <= maxWidth);
        }

        if (filter.MaxHeight.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Height <= filter.MaxHeight);
        }

        if (filter.IsLocked.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IsLocked == filter.IsLocked);
        }

        var tags = filter.Tags.ToList();
        var excludeTags = filter.ExcludeTags.ToList();

        if (filter.IsMovie.HasValue)
        {
            var shouldIncludeAllMovieTypes = filter.IsMovie.Value
                && (filter.IncludeItemTypes.Length == 0
                    || filter.IncludeItemTypes.Contains(BaseItemKind.Movie)
                    || filter.IncludeItemTypes.Contains(BaseItemKind.Trailer));

            if (!shouldIncludeAllMovieTypes)
            {
                baseQuery = baseQuery.Where(e => e.IsMovie == filter.IsMovie.Value);
            }
        }

        if (filter.IsSeries.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IsSeries == filter.IsSeries);
        }

        if (filter.IsSports.HasValue)
        {
            if (filter.IsSports.Value)
            {
                tags.Add("Sports");
            }
            else
            {
                excludeTags.Add("Sports");
            }
        }

        if (filter.IsNews.HasValue)
        {
            if (filter.IsNews.Value)
            {
                tags.Add("News");
            }
            else
            {
                excludeTags.Add("News");
            }
        }

        if (filter.IsKids.HasValue)
        {
            if (filter.IsKids.Value)
            {
                tags.Add("Kids");
            }
            else
            {
                excludeTags.Add("Kids");
            }
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var cleanedSearchTerm = filter.SearchTerm.GetCleanValue();
            var originalSearchTerm = filter.SearchTerm;
            if (SearchWildcardTerms.Any(f => cleanedSearchTerm.Contains(f)))
            {
                cleanedSearchTerm = $"%{cleanedSearchTerm.Trim('%')}%";
                var likeSearchTerm = $"%{originalSearchTerm.Trim('%')}%";
                baseQuery = baseQuery.Where(e => EF.Functions.Like(e.CleanName!, cleanedSearchTerm) || (e.OriginalTitle != null && EF.Functions.Like(e.OriginalTitle, likeSearchTerm)));
            }
            else
            {
                var likeSearchTerm = $"%{originalSearchTerm}%";
                baseQuery = baseQuery.Where(e => e.CleanName!.Contains(cleanedSearchTerm) || (e.OriginalTitle != null && EF.Functions.Like(e.OriginalTitle, likeSearchTerm)));
            }
        }

        if (filter.IsFolder.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IsFolder == filter.IsFolder);
        }

        var includeTypes = filter.IncludeItemTypes;

        // Only specify excluded types if no included types are specified
        if (filter.IncludeItemTypes.Length == 0)
        {
            var excludeTypes = filter.ExcludeItemTypes;
            if (excludeTypes.Length == 1)
            {
                if (_itemTypeLookup.BaseItemKindNames.TryGetValue(excludeTypes[0], out var excludeTypeName))
                {
                    baseQuery = baseQuery.Where(e => e.Type != excludeTypeName);
                }
            }
            else if (excludeTypes.Length > 1)
            {
                var excludeTypeName = new List<string>();
                foreach (var excludeType in excludeTypes)
                {
                    if (_itemTypeLookup.BaseItemKindNames.TryGetValue(excludeType, out var baseItemKindName))
                    {
                        excludeTypeName.Add(baseItemKindName!);
                    }
                }

                baseQuery = baseQuery.Where(e => !excludeTypeName.Contains(e.Type));
            }
        }
        else
        {
            string[] types = includeTypes.Select(f => _itemTypeLookup.BaseItemKindNames.GetValueOrDefault(f)).Where(e => e != null).ToArray()!;
            baseQuery = baseQuery.WhereOneOrMany(types, f => f.Type);
        }

        if (filter.ChannelIds.Count > 0)
        {
            baseQuery = baseQuery.Where(e => e.ChannelId != null && filter.ChannelIds.Contains(e.ChannelId.Value));
        }

        if (!filter.ParentId.IsEmpty())
        {
            baseQuery = baseQuery.Where(e => e.ParentId!.Value == filter.ParentId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Path))
        {
            var pathToQuery = GetPathToSave(filter.Path);
            baseQuery = baseQuery.Where(e => e.Path == pathToQuery);
        }

        if (!string.IsNullOrWhiteSpace(filter.PresentationUniqueKey))
        {
            baseQuery = baseQuery.Where(e => e.PresentationUniqueKey == filter.PresentationUniqueKey);
        }

        if (filter.MinCommunityRating.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.CommunityRating >= filter.MinCommunityRating);
        }

        if (filter.MinIndexNumber.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IndexNumber >= filter.MinIndexNumber);
        }

        if (filter.MinParentAndIndexNumber.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => (e.ParentIndexNumber == filter.MinParentAndIndexNumber.Value.ParentIndexNumber && e.IndexNumber >= filter.MinParentAndIndexNumber.Value.IndexNumber) || e.ParentIndexNumber > filter.MinParentAndIndexNumber.Value.ParentIndexNumber);
        }

        if (filter.MinDateCreated.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.DateCreated >= filter.MinDateCreated);
        }

        if (filter.MinDateLastSaved.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.DateLastSaved != null && e.DateLastSaved >= filter.MinDateLastSaved.Value);
        }

        if (filter.MinDateLastSavedForUser.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.DateLastSaved != null && e.DateLastSaved >= filter.MinDateLastSavedForUser.Value);
        }

        if (filter.IndexNumber.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IndexNumber == filter.IndexNumber.Value);
        }

        if (filter.ParentIndexNumber.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.ParentIndexNumber == filter.ParentIndexNumber.Value);
        }

        if (filter.ParentIndexNumberNotEquals.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.ParentIndexNumber != filter.ParentIndexNumberNotEquals.Value || e.ParentIndexNumber == null);
        }

        var minEndDate = filter.MinEndDate;
        var maxEndDate = filter.MaxEndDate;

        if (filter.HasAired.HasValue)
        {
            if (filter.HasAired.Value)
            {
                maxEndDate = DateTime.UtcNow;
            }
            else
            {
                minEndDate = DateTime.UtcNow;
            }
        }

        if (minEndDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.EndDate >= minEndDate);
        }

        if (maxEndDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.EndDate <= maxEndDate);
        }

        if (filter.MinStartDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.StartDate >= filter.MinStartDate.Value);
        }

        if (filter.MaxStartDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.StartDate <= filter.MaxStartDate.Value);
        }

        if (filter.MinPremiereDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.PremiereDate >= filter.MinPremiereDate.Value);
        }

        if (filter.MaxPremiereDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.PremiereDate <= filter.MaxPremiereDate.Value);
        }

        if (filter.TrailerTypes.Length > 0)
        {
            var trailerTypes = filter.TrailerTypes.Select(e => (int)e).ToArray();
            baseQuery = baseQuery.Where(e => e.TrailerTypes!.Any(w => trailerTypes.Contains(w.Id)));
        }

        if (filter.IsAiring.HasValue)
        {
            if (filter.IsAiring.Value)
            {
                baseQuery = baseQuery.Where(e => e.StartDate <= now && e.EndDate >= now);
            }
            else
            {
                baseQuery = baseQuery.Where(e => e.StartDate > now && e.EndDate < now);
            }
        }

        if (filter.PersonIds.Length > 0)
        {
            var peopleEntityIds = context.BaseItems
                .WhereOneOrMany(filter.PersonIds, b => b.Id)
                .Join(
                    context.Peoples,
                    b => b.Name,
                    p => p.Name,
                    (b, p) => p.Id);

            baseQuery = baseQuery
                .Where(e => context.PeopleBaseItemMap
                    .Any(m => m.ItemId == e.Id && peopleEntityIds.Contains(m.PeopleId)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Person))
        {
            baseQuery = baseQuery.Where(e => e.Peoples!.Any(f => f.People.Name == filter.Person));
        }

        if (!string.IsNullOrWhiteSpace(filter.MinSortName))
        {
            // this does not makes sense.
            // baseQuery = baseQuery.Where(e => e.SortName >= query.MinSortName);
            // whereClauses.Add("SortName>=@MinSortName");
            // statement?.TryBind("@MinSortName", query.MinSortName);
        }

        if (!string.IsNullOrWhiteSpace(filter.ExternalSeriesId))
        {
            baseQuery = baseQuery.Where(e => e.ExternalSeriesId == filter.ExternalSeriesId);
        }

        if (!string.IsNullOrWhiteSpace(filter.ExternalId))
        {
            baseQuery = baseQuery.Where(e => e.ExternalId == filter.ExternalId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            if (filter.UseRawName == true)
            {
                baseQuery = baseQuery.Where(e => e.Name == filter.Name);
            }
            else
            {
                var cleanName = filter.Name.GetCleanValue();
                baseQuery = baseQuery.Where(e => e.CleanName == cleanName);
            }
        }

        // These are the same, for now
        var nameContains = filter.NameContains;
        if (!string.IsNullOrWhiteSpace(nameContains))
        {
            if (SearchWildcardTerms.Any(f => nameContains.Contains(f)))
            {
                nameContains = $"%{nameContains.Trim('%')}%";
                baseQuery = baseQuery.Where(e => EF.Functions.Like(e.CleanName, nameContains) || EF.Functions.Like(e.OriginalTitle, nameContains));
            }
            else
            {
                var likeNameContains = $"%{nameContains}%";
                baseQuery = baseQuery.Where(e =>
                                    e.CleanName!.Contains(nameContains)
                                    || EF.Functions.Like(e.OriginalTitle, likeNameContains));
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.NameStartsWith))
        {
            var nameStartsWithLower = filter.NameStartsWith.ToLowerInvariant();
            baseQuery = baseQuery.Where(e => e.SortName!.ToLower().StartsWith(nameStartsWithLower));
        }

        if (!string.IsNullOrWhiteSpace(filter.NameStartsWithOrGreater))
        {
            var startsOrGreaterLower = filter.NameStartsWithOrGreater.ToLowerInvariant();
            baseQuery = baseQuery.Where(e => e.SortName!.ToLower().CompareTo(startsOrGreaterLower) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(filter.NameLessThan))
        {
            var lessThanLower = filter.NameLessThan.ToLowerInvariant();
            baseQuery = baseQuery.Where(e => e.SortName!.ToLower().CompareTo(lessThanLower) < 0);
        }

        if (filter.ImageTypes.Length > 0)
        {
            var imgTypes = filter.ImageTypes.Select(e => (ImageInfoImageType)e).ToArray();
            baseQuery = baseQuery.Where(e => e.Images!.Any(w => imgTypes.Contains(w.ImageType)));
        }

        if (filter.IsLiked.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => e.UserData!.Any(f => f.UserId == filter.User!.Id && f.Rating >= UserItemData.MinLikeValue));
        }

        if (filter.IsFavoriteOrLiked.HasValue)
        {
            var favoriteItemIds = context.UserData
                .Where(ud => ud.UserId == filter.User!.Id && ud.IsFavorite)
                .Select(ud => ud.ItemId);
            if (filter.IsFavoriteOrLiked.Value)
            {
                baseQuery = baseQuery.Where(e => favoriteItemIds.Contains(e.Id));
            }
            else
            {
                baseQuery = baseQuery.Where(e => !favoriteItemIds.Contains(e.Id));
            }
        }

        if (filter.IsFavorite.HasValue)
        {
            var favoriteItemIds = context.UserData
                .Where(ud => ud.UserId == filter.User!.Id && ud.IsFavorite)
                .Select(ud => ud.ItemId);
            if (filter.IsFavorite.Value)
            {
                baseQuery = baseQuery.Where(e => favoriteItemIds.Contains(e.Id));
            }
            else
            {
                baseQuery = baseQuery.Where(e => !favoriteItemIds.Contains(e.Id));
            }
        }

        if (filter.IsPlayed.HasValue)
        {
            // We should probably figure this out for all folders, but for right now, this is the only place where we need it
            if (filter.IncludeItemTypes.Length == 1 && filter.IncludeItemTypes[0] == BaseItemKind.Series)
            {
                // Get played series IDs by joining episodes to UserData via SeriesId (Guid foreign key).
                // Don't filter episodes by TopParentIds here - the series will be filtered by baseQuery anyway.
                // This allows the materialized list to be reused across library-scoped queries.
                var playedSeriesIdList = context.BaseItems
                    .Where(e => !e.IsFolder && !e.IsVirtualItem && e.SeriesId.HasValue)
                    .Join(
                        context.UserData.Where(ud => ud.UserId == filter.User!.Id && ud.Played),
                        episode => episode.Id,
                        ud => ud.ItemId,
                        (episode, ud) => episode.SeriesId!.Value)
                    .Distinct()
                    .ToList();

                if (filter.IsPlayed.Value)
                {
                    baseQuery = baseQuery.Where(s => playedSeriesIdList.Contains(s.Id));
                }
                else
                {
                    baseQuery = baseQuery.Where(s => !playedSeriesIdList.Contains(s.Id));
                }
            }
            else
            {
                var playedItemIds = context.UserData
                    .Where(ud => ud.UserId == filter.User!.Id && ud.Played)
                    .Select(ud => ud.ItemId);
                if (filter.IsPlayed.Value)
                {
                    baseQuery = baseQuery.Where(e => playedItemIds.Contains(e.Id));
                }
                else
                {
                    baseQuery = baseQuery.Where(e => !playedItemIds.Contains(e.Id));
                }
            }
        }

        if (filter.IsResumable.HasValue)
        {
            var resumableItemIds = context.UserData
                .Where(ud => ud.UserId == filter.User!.Id && ud.PlaybackPositionTicks > 0)
                .Select(ud => ud.ItemId);
            if (filter.IsResumable.Value)
            {
                baseQuery = baseQuery.Where(e => resumableItemIds.Contains(e.Id));
            }
            else
            {
                baseQuery = baseQuery.Where(e => !resumableItemIds.Contains(e.Id));
            }
        }

        if (filter.ArtistIds.Length > 0)
        {
            baseQuery = baseQuery.WhereReferencedItemMultipleTypes(context, [ItemValueType.Artist, ItemValueType.AlbumArtist], filter.ArtistIds);
        }

        if (filter.AlbumArtistIds.Length > 0)
        {
            baseQuery = baseQuery.WhereReferencedItem(context, ItemValueType.AlbumArtist, filter.AlbumArtistIds);
        }

        if (filter.ContributingArtistIds.Length > 0)
        {
            var contributingNames = context.BaseItems
                .Where(b => filter.ContributingArtistIds.Contains(b.Id))
                .Select(b => b.CleanName);

            baseQuery = baseQuery.Where(e =>
                e.ItemValues!.Any(ivm =>
                    ivm.ItemValue.Type == ItemValueType.Artist &&
                    contributingNames.Contains(ivm.ItemValue.CleanValue))
                &&
                !e.ItemValues!.Any(ivm =>
                    ivm.ItemValue.Type == ItemValueType.AlbumArtist &&
                    contributingNames.Contains(ivm.ItemValue.CleanValue)));
        }

        if (filter.AlbumIds.Length > 0)
        {
            var subQuery = context.BaseItems.WhereOneOrMany(filter.AlbumIds, f => f.Id);
            baseQuery = baseQuery.Where(e => subQuery.Any(f => f.Name == e.Album));
        }

        if (filter.ExcludeArtistIds.Length > 0)
        {
            baseQuery = baseQuery.WhereReferencedItemMultipleTypes(context, [ItemValueType.Artist, ItemValueType.AlbumArtist], filter.ExcludeArtistIds, true);
        }

        if (filter.GenreIds.Count > 0)
        {
            baseQuery = baseQuery.WhereReferencedItem(context, ItemValueType.Genre, filter.GenreIds.ToArray());
        }

        if (filter.Genres.Count > 0)
        {
            var cleanGenres = filter.Genres.Select(e => e.GetCleanValue()).ToArray().OneOrManyExpressionBuilder<ItemValueMap, string>(f => f.ItemValue.CleanValue);
            baseQuery = baseQuery
                    .Where(e => e.ItemValues!.AsQueryable().Where(f => f.ItemValue.Type == ItemValueType.Genre).Any(cleanGenres));
        }

        if (tags.Count > 0)
        {
            var cleanValues = tags.Select(e => e.GetCleanValue()).ToArray().OneOrManyExpressionBuilder<ItemValueMap, string>(f => f.ItemValue.CleanValue);
            baseQuery = baseQuery
                    .Where(e => e.ItemValues!.AsQueryable().Where(f => f.ItemValue.Type == ItemValueType.Tags).Any(cleanValues));
        }

        if (excludeTags.Count > 0)
        {
            var cleanValues = excludeTags.Select(e => e.GetCleanValue()).ToArray().OneOrManyExpressionBuilder<ItemValueMap, string>(f => f.ItemValue.CleanValue);
            baseQuery = baseQuery
                    .Where(e => !e.ItemValues!.AsQueryable().Where(f => f.ItemValue.Type == ItemValueType.Tags).Any(cleanValues));
        }

        if (filter.StudioIds.Length > 0)
        {
            baseQuery = baseQuery.WhereReferencedItem(context, ItemValueType.Studios, filter.StudioIds.ToArray());
        }

        if (filter.OfficialRatings.Length > 0)
        {
            var ratings = filter.OfficialRatings;
            baseQuery = baseQuery.WhereItemOrDescendantMatches(context, e => ratings.Contains(e.OfficialRating));
        }

        Expression<Func<BaseItemEntity, bool>>? minParentalRatingFilter = null;
        if (filter.MinParentalRating != null)
        {
            var min = filter.MinParentalRating;
            var minScore = min.Score;
            var minSubScore = min.SubScore ?? 0;

            minParentalRatingFilter = e =>
                e.InheritedParentalRatingValue == null ||
                e.InheritedParentalRatingValue > minScore ||
                (e.InheritedParentalRatingValue == minScore && (e.InheritedParentalRatingSubValue ?? 0) >= minSubScore);
        }

        Expression<Func<BaseItemEntity, bool>>? maxParentalRatingFilter = null;
        if (filter.MaxParentalRating != null)
        {
            var max = filter.MaxParentalRating;
            var maxScore = max.Score;
            var maxSubScore = max.SubScore ?? 0;

            maxParentalRatingFilter = e =>
                e.InheritedParentalRatingValue == null ||
                e.InheritedParentalRatingValue < maxScore ||
                (e.InheritedParentalRatingValue == maxScore && (e.InheritedParentalRatingSubValue ?? 0) <= maxSubScore);
        }

        if (filter.HasParentalRating ?? false)
        {
            if (minParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(minParentalRatingFilter);
            }

            if (maxParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(maxParentalRatingFilter);
            }
        }
        else if (filter.BlockUnratedItems.Length > 0)
        {
            var unratedItemTypes = filter.BlockUnratedItems.Select(f => f.ToString()).ToArray();
            Expression<Func<BaseItemEntity, bool>> unratedItemFilter = e => e.InheritedParentalRatingValue != null || !unratedItemTypes.Contains(e.UnratedType);

            if (minParentalRatingFilter != null && maxParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(unratedItemFilter.And(minParentalRatingFilter.And(maxParentalRatingFilter)));
            }
            else if (minParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(unratedItemFilter.And(minParentalRatingFilter));
            }
            else if (maxParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(unratedItemFilter.And(maxParentalRatingFilter));
            }
            else
            {
                baseQuery = baseQuery.Where(unratedItemFilter);
            }
        }
        else if (minParentalRatingFilter != null || maxParentalRatingFilter != null)
        {
            if (minParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(minParentalRatingFilter);
            }

            if (maxParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(maxParentalRatingFilter);
            }
        }
        else if (!filter.HasParentalRating ?? false)
        {
            baseQuery = baseQuery
                .Where(e => e.InheritedParentalRatingValue == null);
        }

        if (filter.HasOfficialRating.HasValue)
        {
            Expression<Func<BaseItemEntity, bool>> hasRating =
                e => e.OfficialRating != null && e.OfficialRating != string.Empty;

            baseQuery = filter.HasOfficialRating.Value
                ? baseQuery.WhereItemOrDescendantMatches(context, hasRating)
                : baseQuery.WhereNeitherItemNorDescendantMatches(context, hasRating);
        }

        if (filter.HasOverview.HasValue)
        {
            if (filter.HasOverview.Value)
            {
                baseQuery = baseQuery
                    .Where(e => e.Overview != null && e.Overview != string.Empty);
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.Overview == null || e.Overview == string.Empty);
            }
        }

        if (filter.HasOwnerId.HasValue)
        {
            if (filter.HasOwnerId.Value)
            {
                baseQuery = baseQuery
                    .Where(e => e.OwnerId != null);
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.OwnerId == null);
            }
        }
        else if (filter.OwnerIds.Length == 0 && filter.ExtraTypes.Length == 0)
        {
            // Exclude alternate versions from general queries. Alternate versions have
            // OwnerId set (pointing to their primary) but no ExtraType.
            // Extras (trailers, etc.) also have OwnerId but DO have ExtraType set - keep those.
            baseQuery = baseQuery.Where(e => e.OwnerId == null || e.ExtraType != null);
        }

        if (filter.OwnerIds.Length > 0)
        {
            baseQuery = baseQuery.Where(e => e.OwnerId != null && filter.OwnerIds.Contains(e.OwnerId.Value));
        }

        if (filter.ExtraTypes.Length > 0)
        {
            // Convert ExtraType enum to BaseItemExtraType enum via int cast (same underlying values)
            var extraTypeValues = filter.ExtraTypes.Select(e => (BaseItemExtraType?)(int)e).ToArray();
            baseQuery = baseQuery.Where(e => e.ExtraType != null && extraTypeValues.Contains(e.ExtraType));
        }

        if (!string.IsNullOrWhiteSpace(filter.HasNoAudioTrackWithLanguage))
        {
            var lang = filter.HasNoAudioTrackWithLanguage;
            var foldersWithAudio = DescendantQueryHelper.GetFolderIdsMatching(context, new HasMediaStreamType(MediaStreamTypeEntity.Audio, lang));

            baseQuery = baseQuery
                .Where(e =>
                    (!e.IsFolder && !e.MediaStreams!.Any(ms => ms.StreamType == MediaStreamTypeEntity.Audio && ms.Language == lang))
                    || (e.IsFolder && !foldersWithAudio.Contains(e.Id)));
        }

        if (!string.IsNullOrWhiteSpace(filter.HasNoInternalSubtitleTrackWithLanguage))
        {
            var lang = filter.HasNoInternalSubtitleTrackWithLanguage;
            var foldersWithSubtitles = DescendantQueryHelper.GetFolderIdsMatching(context, new HasMediaStreamType(MediaStreamTypeEntity.Subtitle, lang, IsExternal: false));

            baseQuery = baseQuery
                .Where(e =>
                    (!e.IsFolder && !e.MediaStreams!.Any(ms => ms.StreamType == MediaStreamTypeEntity.Subtitle && !ms.IsExternal && ms.Language == lang))
                    || (e.IsFolder && !foldersWithSubtitles.Contains(e.Id)));
        }

        if (!string.IsNullOrWhiteSpace(filter.HasNoExternalSubtitleTrackWithLanguage))
        {
            var lang = filter.HasNoExternalSubtitleTrackWithLanguage;
            var foldersWithSubtitles = DescendantQueryHelper.GetFolderIdsMatching(context, new HasMediaStreamType(MediaStreamTypeEntity.Subtitle, lang, IsExternal: true));

            baseQuery = baseQuery
                .Where(e =>
                    (!e.IsFolder && !e.MediaStreams!.Any(ms => ms.StreamType == MediaStreamTypeEntity.Subtitle && ms.IsExternal && ms.Language == lang))
                    || (e.IsFolder && !foldersWithSubtitles.Contains(e.Id)));
        }

        if (!string.IsNullOrWhiteSpace(filter.HasNoSubtitleTrackWithLanguage))
        {
            var lang = filter.HasNoSubtitleTrackWithLanguage;
            var foldersWithSubtitles = DescendantQueryHelper.GetFolderIdsMatching(context, new HasMediaStreamType(MediaStreamTypeEntity.Subtitle, lang));

            baseQuery = baseQuery
                .Where(e =>
                    (!e.IsFolder && !e.MediaStreams!.Any(ms => ms.StreamType == MediaStreamTypeEntity.Subtitle && ms.Language == lang))
                    || (e.IsFolder && !foldersWithSubtitles.Contains(e.Id)));
        }

        if (filter.HasSubtitles.HasValue)
        {
            var hasSubtitles = filter.HasSubtitles.Value;
            var foldersWithSubtitles = DescendantQueryHelper.GetFolderIdsMatching(context, new HasSubtitles());
            if (hasSubtitles)
            {
                baseQuery = baseQuery
                    .Where(e =>
                        (!e.IsFolder && e.MediaStreams!.Any(f => f.StreamType == MediaStreamTypeEntity.Subtitle))
                        || (e.IsFolder && foldersWithSubtitles.Contains(e.Id)));
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e =>
                        (!e.IsFolder && !e.MediaStreams!.Any(f => f.StreamType == MediaStreamTypeEntity.Subtitle))
                        || (e.IsFolder && !foldersWithSubtitles.Contains(e.Id)));
            }
        }

        if (filter.HasChapterImages.HasValue)
        {
            var hasChapterImages = filter.HasChapterImages.Value;
            var foldersWithChapterImages = DescendantQueryHelper.GetFolderIdsMatching(context, new HasChapterImages());
            if (hasChapterImages)
            {
                baseQuery = baseQuery
                    .Where(e =>
                        (!e.IsFolder && e.Chapters!.Any(f => f.ImagePath != null))
                        || (e.IsFolder && foldersWithChapterImages.Contains(e.Id)));
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e =>
                        (!e.IsFolder && !e.Chapters!.Any(f => f.ImagePath != null))
                        || (e.IsFolder && !foldersWithChapterImages.Contains(e.Id)));
            }
        }

        if (filter.HasDeadParentId.HasValue && filter.HasDeadParentId.Value)
        {
            baseQuery = baseQuery
                .Where(e => e.ParentId.HasValue && !context.BaseItems.Where(e => e.Id != EF.Constant(PlaceholderId)).Any(f => f.Id == e.ParentId.Value));
        }

        if (filter.IsDeadArtist.HasValue && filter.IsDeadArtist.Value)
        {
            baseQuery = baseQuery
                    .Where(e => !context.ItemValues.Where(f => _getAllArtistsValueTypes.Contains(f.Type)).Any(f => f.Value == e.Name));
        }

        if (filter.IsDeadStudio.HasValue && filter.IsDeadStudio.Value)
        {
            baseQuery = baseQuery
                    .Where(e => !context.ItemValues.Where(f => _getStudiosValueTypes.Contains(f.Type)).Any(f => f.Value == e.Name));
        }

        if (filter.IsDeadGenre.HasValue && filter.IsDeadGenre.Value)
        {
            baseQuery = baseQuery
                    .Where(e => !context.ItemValues.Where(f => _getGenreValueTypes.Contains(f.Type)).Any(f => f.Value == e.Name));
        }

        if (filter.IsDeadPerson.HasValue && filter.IsDeadPerson.Value)
        {
            baseQuery = baseQuery
                .Where(e => !context.Peoples.Any(f => f.Name == e.Name));
        }

        if (filter.Years.Length > 0)
        {
            baseQuery = baseQuery.WhereOneOrMany(filter.Years, e => e.ProductionYear!.Value);
        }

        var isVirtualItem = filter.IsVirtualItem ?? filter.IsMissing;
        if (isVirtualItem.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => e.IsVirtualItem == isVirtualItem.Value);
        }

        if (filter.IsSpecialSeason.HasValue)
        {
            if (filter.IsSpecialSeason.Value)
            {
                baseQuery = baseQuery
                    .Where(e => e.IndexNumber == 0);
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.IndexNumber != 0);
            }
        }

        if (filter.IsUnaired.HasValue)
        {
            if (filter.IsUnaired.Value)
            {
                baseQuery = baseQuery
                    .Where(e => e.PremiereDate >= now);
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.PremiereDate < now);
            }
        }

        if (filter.MediaTypes.Length > 0)
        {
            var mediaTypes = filter.MediaTypes.Select(f => f.ToString()).ToArray();
            baseQuery = baseQuery.WhereOneOrMany(mediaTypes, e => e.MediaType);
        }

        if (filter.ItemIds.Length > 0)
        {
            baseQuery = baseQuery.WhereOneOrMany(filter.ItemIds, e => e.Id);
        }

        if (filter.ExcludeItemIds.Length > 0)
        {
            baseQuery = baseQuery
                .Where(e => !filter.ExcludeItemIds.Contains(e.Id));
        }

        if (filter.ExcludeProviderIds is not null && filter.ExcludeProviderIds.Count > 0)
        {
            var exclude = filter.ExcludeProviderIds.Select(e => $"{e.Key}:{e.Value}").ToArray();
            baseQuery = baseQuery.Where(e => e.Provider!.Select(f => f.ProviderId + ":" + f.ProviderValue)!.All(f => !exclude.Contains(f)));
        }

        if (filter.HasAnyProviderId is not null && filter.HasAnyProviderId.Count > 0)
        {
            // Allow setting a null or empty value to get all items that have the specified provider set.
            var includeAny = filter.HasAnyProviderId.Where(e => string.IsNullOrEmpty(e.Value)).Select(e => e.Key).ToArray();
            if (includeAny.Length > 0)
            {
                baseQuery = baseQuery.Where(e => e.Provider!.Any(f => includeAny.Contains(f.ProviderId)));
            }

            var includeSelected = filter.HasAnyProviderId.Where(e => !string.IsNullOrEmpty(e.Value)).Select(e => $"{e.Key}:{e.Value}").ToArray();
            if (includeSelected.Length > 0)
            {
                baseQuery = baseQuery.Where(e => e.Provider!.Select(f => f.ProviderId + ":" + f.ProviderValue)!.Any(f => includeSelected.Contains(f)));
            }
        }

        if (filter.HasImdbId.HasValue)
        {
            baseQuery = filter.HasImdbId.Value
                ? baseQuery.Where(e => e.Provider!.Any(f => f.ProviderId.ToLower() == ImdbProviderName))
                : baseQuery.Where(e => e.Provider!.All(f => f.ProviderId.ToLower() != ImdbProviderName));
        }

        if (filter.HasTmdbId.HasValue)
        {
            baseQuery = filter.HasTmdbId.Value
                ? baseQuery.Where(e => e.Provider!.Any(f => f.ProviderId.ToLower() == TmdbProviderName))
                : baseQuery.Where(e => e.Provider!.All(f => f.ProviderId.ToLower() != TmdbProviderName));
        }

        if (filter.HasTvdbId.HasValue)
        {
            baseQuery = filter.HasTvdbId.Value
                ? baseQuery.Where(e => e.Provider!.Any(f => f.ProviderId.ToLower() == TvdbProviderName))
                : baseQuery.Where(e => e.Provider!.All(f => f.ProviderId.ToLower() != TvdbProviderName));
        }

        var queryTopParentIds = filter.TopParentIds;

        if (queryTopParentIds.Length > 0)
        {
            var includedItemByNameTypes = GetItemByNameTypesInQuery(filter);
            var enableItemsByName = (filter.IncludeItemsByName ?? false) && includedItemByNameTypes.Count > 0;
            if (enableItemsByName && includedItemByNameTypes.Count > 0)
            {
                baseQuery = baseQuery.Where(e => includedItemByNameTypes.Contains(e.Type) || queryTopParentIds.Any(w => w == e.TopParentId!.Value));
            }
            else
            {
                baseQuery = baseQuery.WhereOneOrMany(queryTopParentIds, e => e.TopParentId!.Value);
            }
        }

        if (filter.AncestorIds.Length > 0)
        {
            var ancestorFilter = filter.AncestorIds.OneOrManyExpressionBuilder<AncestorId, Guid>(f => f.ParentItemId);
            baseQuery = baseQuery.Where(e => e.Parents!.AsQueryable().Any(ancestorFilter));
        }

        if (!string.IsNullOrWhiteSpace(filter.AncestorWithPresentationUniqueKey))
        {
            baseQuery = baseQuery
                .Where(e => context.BaseItems.Where(e => e.Id != EF.Constant(PlaceholderId)).Where(f => f.PresentationUniqueKey == filter.AncestorWithPresentationUniqueKey).Any(f => f.Children!.Any(w => w.ItemId == e.Id)));
        }

        if (!string.IsNullOrWhiteSpace(filter.SeriesPresentationUniqueKey))
        {
            baseQuery = baseQuery
                .Where(e => e.SeriesPresentationUniqueKey == filter.SeriesPresentationUniqueKey);
        }

        if (filter.ExcludeInheritedTags.Length > 0)
        {
            var excludedTags = filter.ExcludeInheritedTags.Select(e => e.GetCleanValue()).ToArray();
            baseQuery = baseQuery.Where(e =>
                !context.ItemValuesMap.Any(f =>
                    f.ItemValue.Type == ItemValueType.Tags
                    && excludedTags.Contains(f.ItemValue.CleanValue)
                    && (f.ItemId == e.Id
                        || (e.SeriesId.HasValue && f.ItemId == e.SeriesId.Value)
                        || e.Parents!.Any(p => f.ItemId == p.ParentItemId)
                        || (e.TopParentId.HasValue && f.ItemId == e.TopParentId.Value))));
        }

        if (filter.IncludeInheritedTags.Length > 0)
        {
            var includeTags = filter.IncludeInheritedTags.Select(e => e.GetCleanValue()).ToArray();
            var isPlaylistOnlyQuery = includeTypes.Length == 1 && includeTypes.FirstOrDefault() == BaseItemKind.Playlist;
            baseQuery = baseQuery.Where(e =>
                context.ItemValuesMap.Any(f =>
                    f.ItemValue.Type == ItemValueType.Tags
                    && includeTags.Contains(f.ItemValue.CleanValue)
                    && (f.ItemId == e.Id
                        || (e.SeriesId.HasValue && f.ItemId == e.SeriesId.Value)
                        || e.Parents!.Any(p => f.ItemId == p.ParentItemId)
                        || (e.TopParentId.HasValue && f.ItemId == e.TopParentId.Value)))

                // A playlist should be accessible to its owner regardless of allowed tags
                || (isPlaylistOnlyQuery && e.Data!.Contains($"OwnerUserId\":\"{filter.User!.Id:N}\"")));
        }

        if (filter.SeriesStatuses.Length > 0)
        {
            var seriesStatus = filter.SeriesStatuses.Select(e => e.ToString()).ToArray();
            baseQuery = baseQuery
                .Where(e => seriesStatus.Any(f => e.Data!.Contains(f)));
        }

        if (filter.BoxSetLibraryFolders.Length > 0)
        {
            var boxsetFolders = filter.BoxSetLibraryFolders.Select(e => e.ToString("N", CultureInfo.InvariantCulture)).ToArray();
            baseQuery = baseQuery
                .Where(e => boxsetFolders.Any(f => e.Data!.Contains(f)));
        }

        if (filter.VideoTypes.Length > 0)
        {
            var videoTypeBs = filter.VideoTypes.Select(vt => $"\"VideoType\":\"{vt}\"").ToArray();
            Expression<Func<BaseItemEntity, bool>> hasVideoType = e => videoTypeBs.Any(f => e.Data!.Contains(f));
            baseQuery = baseQuery.WhereItemOrDescendantMatches(context, hasVideoType);
        }

        if (filter.Is3D.HasValue)
        {
            Expression<Func<BaseItemEntity, bool>> is3D = e => e.Data!.Contains("Video3DFormat");

            baseQuery = filter.Is3D.Value
                ? baseQuery.WhereItemOrDescendantMatches(context, is3D)
                : baseQuery.WhereNeitherItemNorDescendantMatches(context, is3D);
        }

        if (filter.IsPlaceHolder.HasValue)
        {
            Expression<Func<BaseItemEntity, bool>> isPlaceHolder = e => e.Data!.Contains("IsPlaceHolder\":true");

            baseQuery = filter.IsPlaceHolder.Value
                ? baseQuery.WhereItemOrDescendantMatches(context, isPlaceHolder)
                : baseQuery.WhereNeitherItemNorDescendantMatches(context, isPlaceHolder);
        }

        if (filter.HasSpecialFeature.HasValue)
        {
            var itemsWithExtras = context.BaseItems
                .Where(extra => extra.OwnerId != null)
                .Select(extra => extra.OwnerId!.Value)
                .Distinct();

            Expression<Func<BaseItemEntity, bool>> hasExtras = e => itemsWithExtras.Contains(e.Id);

            baseQuery = filter.HasSpecialFeature.Value
                ? baseQuery.WhereItemOrDescendantMatches(context, hasExtras)
                : baseQuery.WhereNeitherItemNorDescendantMatches(context, hasExtras);
        }

        if (filter.HasTrailer.HasValue)
        {
            var trailerOwnerIds = context.BaseItems
                .Where(extra => extra.ExtraType == BaseItemExtraType.Trailer && extra.OwnerId != null)
                .Select(extra => extra.OwnerId!.Value);

            Expression<Func<BaseItemEntity, bool>> hasTrailer = e => trailerOwnerIds.Contains(e.Id);

            baseQuery = filter.HasTrailer.Value
                ? baseQuery.WhereItemOrDescendantMatches(context, hasTrailer)
                : baseQuery.WhereNeitherItemNorDescendantMatches(context, hasTrailer);
        }

        if (filter.HasThemeSong.HasValue)
        {
            var themeSongOwnerIds = context.BaseItems
                .Where(extra => extra.ExtraType == BaseItemExtraType.ThemeSong && extra.OwnerId != null)
                .Select(extra => extra.OwnerId!.Value);

            Expression<Func<BaseItemEntity, bool>> hasThemeSong = e => themeSongOwnerIds.Contains(e.Id);

            baseQuery = filter.HasThemeSong.Value
                ? baseQuery.WhereItemOrDescendantMatches(context, hasThemeSong)
                : baseQuery.WhereNeitherItemNorDescendantMatches(context, hasThemeSong);
        }

        if (filter.HasThemeVideo.HasValue)
        {
            var themeVideoOwnerIds = context.BaseItems
                .Where(extra => extra.ExtraType == BaseItemExtraType.ThemeVideo && extra.OwnerId != null)
                .Select(extra => extra.OwnerId!.Value);

            Expression<Func<BaseItemEntity, bool>> hasThemeVideo = e => themeVideoOwnerIds.Contains(e.Id);

            baseQuery = filter.HasThemeVideo.Value
                ? baseQuery.WhereItemOrDescendantMatches(context, hasThemeVideo)
                : baseQuery.WhereNeitherItemNorDescendantMatches(context, hasThemeVideo);
        }

        if (filter.AiredDuringSeason.HasValue)
        {
            var seasonNumber = filter.AiredDuringSeason.Value;
            if (seasonNumber < 1)
            {
                baseQuery = baseQuery.Where(e => e.ParentIndexNumber == seasonNumber);
            }
            else
            {
                var seasonStr = seasonNumber.ToString(CultureInfo.InvariantCulture);
                baseQuery = baseQuery.Where(e =>
                    e.ParentIndexNumber == seasonNumber
                    || (e.Data != null && (
                        e.Data.Contains("\"AirsAfterSeasonNumber\":" + seasonStr)
                        || e.Data.Contains("\"AirsBeforeSeasonNumber\":" + seasonStr))));
            }
        }

        if (filter.AdjacentTo.HasValue && !filter.AdjacentTo.Value.IsEmpty())
        {
            var adjacentToId = filter.AdjacentTo.Value;
            var targetItem = context.BaseItems.Where(e => e.Id == adjacentToId).Select(e => new { e.SortName, e.Id }).FirstOrDefault();
            if (targetItem is not null)
            {
                var targetSortName = targetItem.SortName ?? string.Empty;
                var prevId = context.BaseItems
                    .Where(e => string.Compare(e.SortName, targetSortName) < 0)
                    .OrderByDescending(e => e.SortName)
                    .Select(e => e.Id)
                    .FirstOrDefault();

                var nextId = context.BaseItems
                    .Where(e => string.Compare(e.SortName, targetSortName) > 0)
                    .OrderBy(e => e.SortName)
                    .Select(e => e.Id)
                    .FirstOrDefault();

                var adjacentIds = new List<Guid> { adjacentToId };
                if (prevId != Guid.Empty)
                {
                    adjacentIds.Add(prevId);
                }

                if (nextId != Guid.Empty)
                {
                    adjacentIds.Add(nextId);
                }

                baseQuery = baseQuery.Where(e => adjacentIds.Contains(e.Id));
            }
        }

        return baseQuery;
    }

    /// <inheritdoc/>
    public async Task<bool> ItemExistsAsync(Guid id)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            return await dbContext.BaseItems.AnyAsync(f => f.Id == id).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public bool GetIsPlayed(User user, Guid id, bool recursive)
    {
        using var dbContext = _dbProvider.CreateDbContext();

        if (recursive)
        {
            var descendantIds = DescendantQueryHelper.GetAllDescendantIds(dbContext, id);

            return dbContext.BaseItems
                    .Where(e => descendantIds.Contains(e.Id) && !e.IsFolder && !e.IsVirtualItem)
                    .All(f => f.UserData!.Any(e => e.UserId == user.Id && e.Played));
        }

        return dbContext.BaseItems.Where(e => e.ParentId == id).All(f => f.UserData!.Any(e => e.UserId == user.Id && e.Played));
    }

    /// <inheritdoc/>
    public int GetPlayedCount(InternalItemsQuery filter, Guid ancestorId)
    {
        ArgumentNullException.ThrowIfNull(filter.User);
        using var dbContext = _dbProvider.CreateDbContext();

        var baseQuery = BuildAccessFilteredDescendantsQuery(dbContext, filter, ancestorId);
        return baseQuery.Count(b => b.UserData!.Any(u => u.UserId == filter.User.Id && u.Played));
    }

    /// <inheritdoc/>
    public int GetTotalCount(InternalItemsQuery filter, Guid ancestorId)
    {
        using var dbContext = _dbProvider.CreateDbContext();

        var baseQuery = BuildAccessFilteredDescendantsQuery(dbContext, filter, ancestorId);
        return baseQuery.Count();
    }

    /// <inheritdoc/>
    public (int Played, int Total) GetPlayedAndTotalCount(InternalItemsQuery filter, Guid ancestorId)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(filter.User);
        using var dbContext = _dbProvider.CreateDbContext();

        var baseQuery = BuildAccessFilteredDescendantsQuery(dbContext, filter, ancestorId);
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
        baseQuery = ApplyAccessFiltering(dbContext, baseQuery, filter);

        return GetPlayedAndTotalCountFromQuery(baseQuery, filter.User.Id);
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

        // Build access filter from user preferences (parental ratings, blocked/allowed tags, etc.)
        var filter = new InternalItemsQuery(user);

        // Get all non-folder, non-virtual descendants via AncestorIds table
        var baseQuery = dbContext.BaseItems
            .Where(b => dbContext.AncestorIds
                .Any(a => folderIdsArray.Contains(a.ParentItemId) && a.ItemId == b.Id))
            .Where(b => !b.IsFolder && !b.IsVirtualItem);

        // Apply the same access filtering as per-item path
        baseQuery = ApplyAccessFiltering(dbContext, baseQuery, filter);

        // Join back with AncestorIds to group by parent folder ID and compute counts
        var results = dbContext.AncestorIds
            .Where(a => folderIdsArray.Contains(a.ParentItemId))
            .Join(
                baseQuery,
                a => a.ItemId,
                b => b.Id,
                (a, b) => new { a.ParentItemId, b.Id, b.UserData })
            .GroupBy(x => x.ParentItemId)
            .Select(g => new
            {
                FolderId = g.Key,
                Total = g.Count(),
                Played = g.Count(x => x.UserData!.Any(ud => ud.UserId == user.Id && ud.Played))
            })
            .ToDictionary(x => x.FolderId, x => (x.Played, x.Total));

        return results;
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
            .Select(lc => lc.ChildId)
            .ToList();
    }

    private static (int Played, int Total) GetPlayedAndTotalCountFromQuery(IQueryable<BaseItemEntity> query, Guid userId)
    {
        // GroupBy with a constant key aggregates all rows into a single group for server-side counting.
        // OrderBy is required before FirstOrDefault to avoid EF Core warnings about unpredictable results.
        var result = query
            .Select(b => b.UserData!.Any(u => u.UserId == userId && u.Played))
            .GroupBy(_ => 1)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Total = g.Count(),
                Played = g.Count(isPlayed => isPlayed)
            })
            .OrderBy(_ => 1)
            .FirstOrDefault();

        return result is null ? (0, 0) : (result.Played, result.Total);
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

        // Convert to array for EF Core Contains support
        var parentIdsArray = parentIds.ToArray();

        // Count hierarchical children (immediate children via ParentId)
        var hierarchicalCounts = dbContext.BaseItems
            .Where(b => b.ParentId.HasValue && parentIdsArray.Contains(b.ParentId.Value))
            .GroupBy(b => b.ParentId!.Value)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.ParentId, x => x.Count);

        // Count linked children (BoxSets, Playlists, etc.)
        var linkedCounts = dbContext.LinkedChildren
            .Where(lc => parentIdsArray.Contains(lc.ParentId))
            .GroupBy(lc => lc.ParentId)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.ParentId, x => x.Count);

        // Merge results
        var result = new Dictionary<Guid, int>();
        foreach (var parentId in parentIds)
        {
            var hierarchicalCount = hierarchicalCounts.GetValueOrDefault(parentId, 0);
            var linkedCount = linkedCounts.GetValueOrDefault(parentId, 0);

            // If there are linked children, use that count (matches Folder.GetChildCount logic)
            // Otherwise use hierarchical count
            result[parentId] = linkedCount > 0 ? linkedCount : hierarchicalCount;
        }

        return result;
    }

    /// <summary>
    /// Builds a query for descendants of an ancestor with user access filtering applied.
    /// Uses recursive CTE to traverse both hierarchical (AncestorIds) and linked (LinkedChildren) relationships.
    /// </summary>
    private IQueryable<BaseItemEntity> BuildAccessFilteredDescendantsQuery(
        JellyfinDbContext context,
        InternalItemsQuery filter,
        Guid ancestorId)
    {
        // Use recursive CTE to get all descendants (hierarchical and linked)
        var allDescendantIds = DescendantQueryHelper.GetAllDescendantIds(context, ancestorId);

        var baseQuery = context.BaseItems
            .Where(b => allDescendantIds.Contains(b.Id) && !b.IsFolder && !b.IsVirtualItem);

        return ApplyAccessFiltering(context, baseQuery, filter);
    }

    /// <summary>
    /// Applies user access filtering to a query.
    /// Includes TopParentIds, parental rating, and tag filtering.
    /// </summary>
    private IQueryable<BaseItemEntity> ApplyAccessFiltering(
        JellyfinDbContext context,
        IQueryable<BaseItemEntity> baseQuery,
        InternalItemsQuery filter)
    {
        // Apply TopParentIds filtering (library folder access)
        if (filter.TopParentIds.Length > 0)
        {
            var topParentIds = filter.TopParentIds;
            baseQuery = baseQuery.Where(e => topParentIds.Contains(e.TopParentId!.Value));
        }

        // Apply parental rating filtering
        if (filter.MaxParentalRating is not null)
        {
            var maxScore = filter.MaxParentalRating.Score;
            var maxSubScore = filter.MaxParentalRating.SubScore ?? 0;

            baseQuery = baseQuery.Where(e =>
                e.InheritedParentalRatingValue == null ||
                e.InheritedParentalRatingValue < maxScore ||
                (e.InheritedParentalRatingValue == maxScore && (e.InheritedParentalRatingSubValue ?? 0) <= maxSubScore));
        }

        // Apply block unrated items filtering
        if (filter.BlockUnratedItems.Length > 0)
        {
            var unratedItemTypes = filter.BlockUnratedItems.Select(f => f.ToString()).ToArray();
            baseQuery = baseQuery.Where(e =>
                e.InheritedParentalRatingValue != null || !unratedItemTypes.Contains(e.UnratedType));
        }

        // Apply excluded tags filtering (blocked tags)
        if (filter.ExcludeInheritedTags.Length > 0)
        {
            var excludedTags = filter.ExcludeInheritedTags.Select(e => e.GetCleanValue()).ToArray();
            baseQuery = baseQuery.Where(e =>
                !context.ItemValuesMap.Any(f =>
                    f.ItemValue.Type == ItemValueType.Tags
                    && excludedTags.Contains(f.ItemValue.CleanValue)
                    && (f.ItemId == e.Id
                        || (e.SeriesId.HasValue && f.ItemId == e.SeriesId.Value)
                        || e.Parents!.Any(p => f.ItemId == p.ParentItemId)
                        || (e.TopParentId.HasValue && f.ItemId == e.TopParentId.Value))));
        }

        // Apply included tags filtering (allowed tags - item must have at least one)
        if (filter.IncludeInheritedTags.Length > 0)
        {
            var includeTags = filter.IncludeInheritedTags.Select(e => e.GetCleanValue()).ToArray();
            baseQuery = baseQuery.Where(e =>
                context.ItemValuesMap.Any(f =>
                    f.ItemValue.Type == ItemValueType.Tags
                    && includeTags.Contains(f.ItemValue.CleanValue)
                    && (f.ItemId == e.Id
                        || (e.SeriesId.HasValue && f.ItemId == e.SeriesId.Value)
                        || e.Parents!.Any(p => f.ItemId == p.ParentItemId)
                        || (e.TopParentId.HasValue && f.ItemId == e.TopParentId.Value))));
        }

        return baseQuery;
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, MusicArtist[]> FindArtists(IReadOnlyList<string> artistNames)
    {
        using var dbContext = _dbProvider.CreateDbContext();

        var artists = dbContext.BaseItems.Where(e => e.Type == _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist]!)
            .Where(e => artistNames.Contains(e.Name))
            .ToArray();

        var lookup = artists
            .GroupBy(e => e.Name!)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => DeserializeBaseItem(f)).Where(dto => dto is not null).Cast<MusicArtist>().ToArray());

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
    public IReadOnlyList<Guid> GetManualLinkedParentIds(Guid childId)
    {
        using var context = _dbProvider.CreateDbContext();
        return context.LinkedChildren
            .Where(lc => lc.ChildId == childId && lc.ChildType == DbLinkedChildType.Manual)
            .Select(lc => lc.ParentId)
            .Distinct()
            .ToList();
    }

    /// <inheritdoc/>
    public int RerouteLinkedChildren(Guid fromChildId, Guid toChildId)
    {
        using var context = _dbProvider.CreateDbContext();

        // Get parents that already reference toChildId (to avoid duplicates)
        var parentsWithTarget = context.LinkedChildren
            .Where(lc => lc.ChildId == toChildId && lc.ChildType == DbLinkedChildType.Manual)
            .Select(lc => lc.ParentId)
            .ToHashSet();

        // Update references that won't create duplicates
        var updated = context.LinkedChildren
            .Where(lc => lc.ChildId == fromChildId
                && lc.ChildType == DbLinkedChildType.Manual
                && !parentsWithTarget.Contains(lc.ParentId))
            .ExecuteUpdate(s => s.SetProperty(e => e.ChildId, toChildId));

        // Remove references that would be duplicates
        context.LinkedChildren
            .Where(lc => lc.ChildId == fromChildId
                && lc.ChildType == DbLinkedChildType.Manual
                && parentsWithTarget.Contains(lc.ParentId))
            .ExecuteDelete();

        return updated;
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
            context.LinkedChildren.Add(new LinkedChildEntity
            {
                ParentId = parentId,
                ChildId = childId,
                ChildType = dbChildType,
                SortOrder = null
            });
        }
        else
        {
            existingLink.ChildType = dbChildType;
        }

        context.SaveChanges();
    }
}
