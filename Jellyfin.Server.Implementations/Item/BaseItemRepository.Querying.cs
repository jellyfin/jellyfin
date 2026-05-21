#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.EntityFrameworkCore;
using BaseItemDto = MediaBrowser.Controller.Entities.BaseItem;

namespace Jellyfin.Server.Implementations.Item;

public sealed partial class BaseItemRepository
{
    /// <inheritdoc />
    public IReadOnlyList<Guid> GetItemIdsList(InternalItemsQuery filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        PrepareFilterQuery(filter);

        using var context = _dbProvider.CreateDbContext();
        return ApplyQueryFilter(context.BaseItems.AsNoTracking().Where(e => e.Id != EF.Constant(PlaceholderId)), context, filter).Select(e => e.Id).ToArray();
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

        result.Items = dbQuery.AsEnumerable().Where(e => e != null).Select(w => DeserializeBaseItem(w, filter.SkipDeserialization)).Where(dto => dto != null).ToArray()!;
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
            var orderedIds = dbQuery.AsNoTracking().Select(e => e.Id).ToList();
            if (orderedIds.Count == 0)
            {
                return Array.Empty<BaseItemDto>();
            }

            var itemsById = ApplyNavigations(context.BaseItems.AsNoTracking().WhereOneOrMany(orderedIds, e => e.Id), filter)
                .AsSplitQuery()
                .AsEnumerable()
                .Select(w => DeserializeBaseItem(w, filter.SkipDeserialization))
                .Where(dto => dto != null)
                .ToDictionary(i => i!.Id);

            return orderedIds.Where(itemsById.ContainsKey).Select(id => itemsById[id]).ToArray()!;
        }

        dbQuery = ApplyNavigations(dbQuery, filter);

        return dbQuery.AsEnumerable().Where(e => e != null).Select(w => DeserializeBaseItem(w, filter.SkipDeserialization)).Where(dto => dto != null).ToArray()!;
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

        var limit = filter.Limit;
        using var context = _dbProvider.CreateDbContext();

        var baseQuery = PrepareItemQuery(context, filter);
        baseQuery = TranslateQuery(baseQuery, context, filter);

        if (collectionType == CollectionType.tvshows)
        {
            return GetLatestTvShowItems(context, baseQuery, filter, limit);
        }

        if (collectionType is CollectionType.movies)
        {
            // Group by PresentationUniqueKey, pick the newest item per group.
            var topGroupItems = baseQuery
                .Where(e => e.PresentationUniqueKey != null)
                .GroupBy(e => e.PresentationUniqueKey)
                .Select(g => new
                {
                    MaxDate = g.Max(e => e.DateCreated),
                    FirstId = g.OrderByDescending(e => e.DateCreated).ThenByDescending(e => e.Id).Select(e => e.Id).First()
                })
                .OrderByDescending(g => g.MaxDate);

            var firstIdsQuery = filter.Limit.HasValue
                ? topGroupItems.Take(filter.Limit.Value).Select(g => g.FirstId)
                : topGroupItems.Select(g => g.FirstId);

            return LoadLatestByIds(context, firstIdsQuery, filter);
        }

        // Albums whose Id is the parent of any track matching the user's filter.
        var albumIdsWithMatchingTrack = context.AncestorIds
            .Join(baseQuery, ai => ai.ItemId, t => t.Id, (ai, _) => ai.ParentItemId);

        var musicAlbumTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicAlbum]!;
        var topAlbumsQuery = context.BaseItems.AsNoTracking()
            .Where(album => album.Type == musicAlbumTypeName)
            .Where(album => albumIdsWithMatchingTrack.Contains(album.Id))
            .OrderByDescending(album => album.DateCreated)
            .ThenByDescending(album => album.Id);

        var albumIdsQuery = filter.Limit.HasValue
            ? topAlbumsQuery.Take(filter.Limit.Value).Select(a => a.Id)
            : topAlbumsQuery.Select(a => a.Id);

        return LoadLatestByIds(context, albumIdsQuery, filter);
    }

    // Keeping idsQuery deferred lets EF emit `WHERE Id IN (<subquery>)`.
    private IReadOnlyList<BaseItemDto> LoadLatestByIds(
        JellyfinDbContext context,
        IQueryable<Guid> idsQuery,
        InternalItemsQuery filter)
    {
        var itemsQuery = ApplyNavigations(
            context.BaseItems.AsNoTracking().Where(e => idsQuery.Contains(e.Id)),
            filter);

        return itemsQuery
            .OrderByDescending(e => e.DateCreated)
            .ThenByDescending(e => e.Id)
            .AsEnumerable()
            .Select(w => DeserializeBaseItem(w, filter.SkipDeserialization))
            .Where(dto => dto != null)
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
    private IReadOnlyList<BaseItemDto> GetLatestTvShowItems(JellyfinDbContext context, IQueryable<BaseItemEntity> baseQuery, InternalItemsQuery filter, int? limit)
    {
        // Episodes added within this window are considered "recently added together"
        const double RecentAdditionWindowHours = 24.0;

        // Step 1: Find the top N series with recently added content, ordered by most recent addition
        var topSeriesWithDates = baseQuery
            .Where(e => e.SeriesName != null)
            .GroupBy(e => e.SeriesName)
            .Select(g => new { SeriesName = g.Key!, MaxDate = g.Max(e => e.DateCreated) })
            .OrderByDescending(g => g.MaxDate);

        if (limit.HasValue)
        {
            topSeriesWithDates = topSeriesWithDates.Take(limit.Value).OrderByDescending(g => g.MaxDate);
        }

        // Materialize series names and cutoff to avoid embedding the GroupBy+OrderBy
        // expression tree as a subquery inside the episode query.
        var topSeriesData = topSeriesWithDates
            .Select(g => new { g.SeriesName, g.MaxDate })
            .ToList();
        var topSeriesNames = topSeriesData.Select(g => g.SeriesName).ToList();

        // Compute a global date cutoff: the oldest series' max date minus the window.
        // Episodes before this cutoff cannot be in any series' "recent additions" window,
        // so we can safely exclude them to avoid loading ancient episodes.
        var globalCutoff = topSeriesData.Count > 0
            ? topSeriesData.Min(g => g.MaxDate)?.AddHours(-RecentAdditionWindowHours)
            : null;

        // Restrict to episodes of the top series, optionally bounded by the global cutoff.
        var episodeQuery = baseQuery.Where(e => e.SeriesName != null && topSeriesNames.Contains(e.SeriesName));
        if (globalCutoff is not null)
        {
            episodeQuery = episodeQuery.Where(e => e.DateCreated >= globalCutoff);
        }

        // Lightweight projection: only the columns needed for the in-memory analysis below.
        var allEpisodes = episodeQuery
            .OrderByDescending(e => e.DateCreated)
            .ThenByDescending(e => e.Id)
            .Select(e => new { e.Id, e.SeriesName, e.DateCreated, e.SeasonId, e.SeriesId })
            .AsEnumerable();

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

        // Step 5: Apply the container selection logic for each series.
        // For each series, decide which entity best represents the recent additions:
        //   - 1 episode added → show the Episode itself
        //   - Multiple episodes in 1 season (multi-season series) → show the Season
        //   - Multiple episodes in 1 season (single-season series) → show the Series
        //   - Episodes across multiple seasons → show the Series
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

        var finalResults = results
            .OrderByDescending(r => r.MaxDate)
            .ThenByDescending(r => r.Entity.Id);

        if (limit.HasValue)
        {
            finalResults = finalResults
            .Take(limit.Value)
            .OrderByDescending(r => r.MaxDate)
            .ThenByDescending(r => r.Entity.Id);
        }

        return finalResults
            .Select(r => DeserializeBaseItem(r.Entity, filter.SkipDeserialization))
            .Where(dto => dto is not null)
            .ToArray()!;
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public QueryFiltersLegacy GetQueryFiltersLegacy(InternalItemsQuery filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        PrepareFilterQuery(filter);

        using var context = _dbProvider.CreateDbContext();
        var baseQuery = PrepareItemQuery(context, filter);
        baseQuery = TranslateQuery(baseQuery, context, filter);

        var matchingItemIds = baseQuery.Select(e => e.Id);

        var years = baseQuery
            .Where(e => e.ProductionYear != null && e.ProductionYear > 0)
            .Select(e => e.ProductionYear!.Value)
            .Distinct()
            .OrderBy(y => y)
            .ToArray();

        var officialRatings = baseQuery
            .Where(e => e.OfficialRating != null && e.OfficialRating != string.Empty)
            .Select(e => e.OfficialRating!)
            .Distinct()
            .OrderBy(r => r)
            .ToArray();

        var tags = context.ItemValuesMap
            .Where(ivm => ivm.ItemValue.Type == ItemValueType.Tags)
            .Where(ivm => matchingItemIds.Contains(ivm.ItemId))
            .Select(ivm => ivm.ItemValue)
            .GroupBy(iv => iv.CleanValue)
            .Select(g => g.Min(iv => iv.Value))
            .OrderBy(t => t)
            .ToArray();

        var genres = context.ItemValuesMap
            .Where(ivm => ivm.ItemValue.Type == ItemValueType.Genre)
            .Where(ivm => matchingItemIds.Contains(ivm.ItemId))
            .Select(ivm => ivm.ItemValue)
            .GroupBy(iv => iv.CleanValue)
            .Select(g => g.Min(iv => iv.Value))
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
}
