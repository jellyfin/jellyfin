#pragma warning disable RS0030 // Do not use banned APIs
#pragma warning disable CA1304 // Specify CultureInfo
#pragma warning disable CA1311 // Specify a culture or use an invariant version
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
using Jellyfin.Server.Implementations.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.EntityFrameworkCore;
using BaseItemEntity = Jellyfin.Database.Implementations.Entities.BaseItemEntity;

namespace Jellyfin.Server.Implementations.Item;

public sealed partial class BaseItemRepository
{
    /// <inheritdoc />
    public IQueryable<BaseItemEntity> PrepareItemQuery(JellyfinDbContext context, InternalItemsQuery filter)
    {
        IQueryable<BaseItemEntity> dbQuery = context.BaseItems.AsNoTracking();
        dbQuery = dbQuery.AsSingleQuery();

        return dbQuery;
    }

    private IQueryable<BaseItemEntity> ApplyQueryFilter(IQueryable<BaseItemEntity> dbQuery, JellyfinDbContext context, InternalItemsQuery filter)
    {
        dbQuery = TranslateQuery(dbQuery, context, filter);
        dbQuery = ApplyGroupingFilter(context, dbQuery, filter);
        dbQuery = ApplyAdjacencyFilter(context, dbQuery, filter);
        dbQuery = ApplyQueryPaging(dbQuery, filter);
        dbQuery = ApplyNavigations(dbQuery, filter);
        return dbQuery;
    }

    /// <summary>
    /// Trims an ordered query down to the AdjacentTo item and its immediate neighbours.
    /// </summary>
    private IQueryable<BaseItemEntity> ApplyAdjacencyFilter(JellyfinDbContext context, IQueryable<BaseItemEntity> dbQuery, InternalItemsQuery filter)
    {
        if (filter.AdjacentTo.IsNullOrEmpty())
        {
            return dbQuery;
        }

        // Adjacency is relative to the result set and the order the query asked for, so the ids have
        // to be read back in that order.
        var orderedIds = dbQuery.Select(e => e.Id).ToList();
        var index = orderedIds.IndexOf(filter.AdjacentTo.Value);
        if (index < 0)
        {
            // The item isn't part of this result set, so it has no neighbours in it either.
            return dbQuery.Take(0);
        }

        var start = Math.Max(index - 1, 0);
        var adjacentIds = orderedIds.GetRange(start, Math.Min(index + 2, orderedIds.Count) - start);

        var adjacentQuery = context.BaseItems.AsNoTracking().AsSingleQuery().Where(e => adjacentIds.Contains(e.Id));

        return ApplyOrder(adjacentQuery, filter, context);
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

    private IQueryable<BaseItemEntity> ApplyGroupingFilter(JellyfinDbContext context, IQueryable<BaseItemEntity> dbQuery, InternalItemsQuery filter)
    {
        // Collapse duplicates sharing a presentation key (e.g. alternate versions), preferring the
        // primary version (PrimaryVersionId is null) so detail pages and actions target it instead
        // of an arbitrary alternate. Keep the grouped ids as an IQueryable sub-select; materializing
        // to a List would inline one bound parameter per id and hit SQLite's variable cap.
        var enableGroupByPresentationUniqueKey = EnableGroupByPresentationUniqueKey(filter);
        if (enableGroupByPresentationUniqueKey && filter.GroupBySeriesPresentationUniqueKey)
        {
            var groupedIds = dbQuery.GroupBy(e => new { e.PresentationUniqueKey, e.SeriesPresentationUniqueKey })
                .Select(g => g.Where(e => e.PrimaryVersionId == null).Min(e => (Guid?)e.Id) ?? g.Min(e => (Guid?)e.Id));
            dbQuery = context.BaseItems.AsNoTracking().Where(e => groupedIds.Contains(e.Id));
        }
        else if (enableGroupByPresentationUniqueKey)
        {
            var groupedIds = dbQuery.GroupBy(e => e.PresentationUniqueKey)
                .Select(g => g.Where(e => e.PrimaryVersionId == null).Min(e => (Guid?)e.Id) ?? g.Min(e => (Guid?)e.Id));
            dbQuery = context.BaseItems.AsNoTracking().Where(e => groupedIds.Contains(e.Id));
        }
        else if (filter.GroupBySeriesPresentationUniqueKey)
        {
            var groupedIds = dbQuery.GroupBy(e => e.SeriesPresentationUniqueKey).Select(e => e.Min(x => x.Id));
            dbQuery = context.BaseItems.AsNoTracking().Where(e => groupedIds.Contains(e.Id));
        }
        else
        {
            dbQuery = dbQuery.Distinct();
        }

        if (filter.CollapseBoxSetItems == true)
        {
            dbQuery = ApplyBoxSetCollapsing(context, dbQuery, filter.CollapseBoxSetItemTypes);

            // Name filters run after collapse so BoxSets match by their own name, not a child's.
            dbQuery = ApplyNameFilters(dbQuery, filter);
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

        // Categorize items in currentIds in a single pass to avoid multiple correlated EXISTS over BaseItems.
        var categorized = context.BaseItems
            .AsNoTracking()
            .Where(bi => currentIds.Contains(bi.Id))
            .Select(bi => new
            {
                bi.Id,
                IsCollapsible = collapsibleTypeNames.Contains(bi.Type),
                IsBoxSet = bi.Type == boxSetTypeName
            });

        var collapsibleChildIds = categorized.Where(c => c.IsCollapsible).Select(c => c.Id);

        // Single JOIN: manual links to BoxSet parents, restricted to currentIds children.
        var manualBoxSetLinks = context.LinkedChildren
            .Where(lc => lc.ChildType == Database.Implementations.Entities.LinkedChildType.Manual
                && currentIds.Contains(lc.ChildId))
            .Join(
                context.BaseItems.Where(bs => bs.Type == boxSetTypeName),
                lc => lc.ParentId,
                bs => bs.Id,
                (lc, bs) => new { lc.ChildId, lc.ParentId });

        var childrenInBoxSet = manualBoxSetLinks.Select(x => x.ChildId).Distinct();

        // Items whose type is NOT collapsible (always kept in results)
        var nonCollapsibleIds = categorized.Where(c => !c.IsCollapsible).Select(c => c.Id);

        // Collapsible items that are not a BoxSet themselves and not a manual child of any BoxSet
        var collapsibleNotInBoxSet = categorized
            .Where(c => c.IsCollapsible && !c.IsBoxSet)
            .Select(c => c.Id)
            .Where(id => !childrenInBoxSet.Contains(id));

        // BoxSet IDs containing at least one collapsible child item from currentIds
        var boxSetIds = manualBoxSetLinks
            .Where(x => collapsibleChildIds.Contains(x.ChildId))
            .Select(x => x.ParentId)
            .Distinct();

        var collapsedIds = nonCollapsibleIds.Union(collapsibleNotInBoxSet).Union(boxSetIds);
        return context.BaseItems.AsNoTracking().Where(e => collapsedIds.Contains(e.Id));
    }

    private static IQueryable<BaseItemEntity> ApplyBoxSetCollapsingAll(
        JellyfinDbContext context,
        IQueryable<Guid> currentIds,
        string boxSetTypeName)
    {
        // Single JOIN: manual links to BoxSet parents, restricted to currentIds children.
        var manualBoxSetLinks = context.LinkedChildren
            .Where(lc => lc.ChildType == Database.Implementations.Entities.LinkedChildType.Manual
                && currentIds.Contains(lc.ChildId))
            .Join(
                context.BaseItems.Where(bs => bs.Type == boxSetTypeName),
                lc => lc.ParentId,
                bs => bs.Id,
                (lc, bs) => new { lc.ChildId, lc.ParentId });

        var childrenInBoxSet = manualBoxSetLinks.Select(x => x.ChildId).Distinct();
        var boxSetIds = manualBoxSetLinks.Select(x => x.ParentId).Distinct();

        // Items in currentIds that are not BoxSets themselves and not a manual child of any BoxSet
        var notInBoxSet = context.BaseItems
            .AsNoTracking()
            .Where(e => currentIds.Contains(e.Id) && e.Type != boxSetTypeName)
            .Select(e => e.Id)
            .Where(id => !childrenInBoxSet.Contains(id));

        var collapsedIds = notInBoxSet.Union(boxSetIds);
        return context.BaseItems.AsNoTracking().Where(e => collapsedIds.Contains(e.Id));
    }

    private static IQueryable<BaseItemEntity> ApplyNameFilters(IQueryable<BaseItemEntity> dbQuery, InternalItemsQuery filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.NameStartsWith))
        {
            var nameStartsWithLower = filter.NameStartsWith.ToLowerInvariant();
            dbQuery = dbQuery.Where(e => e.SortName!.ToLower().StartsWith(nameStartsWithLower));
        }

        if (!string.IsNullOrWhiteSpace(filter.NameStartsWithOrGreater))
        {
            var startsOrGreaterLower = filter.NameStartsWithOrGreater.ToLowerInvariant();
            dbQuery = dbQuery.Where(e => e.SortName!.ToLower().CompareTo(startsOrGreaterLower) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(filter.NameLessThan))
        {
            var lessThanLower = filter.NameLessThan.ToLowerInvariant();
            dbQuery = dbQuery.Where(e => e.SortName!.ToLower().CompareTo(lessThanLower) < 0);
        }

        return dbQuery;
    }

    /// <inheritdoc />
    public IQueryable<BaseItemEntity> ApplyNavigations(IQueryable<BaseItemEntity> dbQuery, InternalItemsQuery filter)
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

        // Include LinkedChildEntities for container types and videos that use them (BoxSet, Playlist,
        // CollectionFolder for manual linking; every video type for alternate versions).
        // When IncludeItemTypes is empty (any type may be returned), always include them to ensure
        // LinkedChildren are loaded before items are saved back, preventing accidental deletion.
        var linkedChildTypes = new[]
        {
            BaseItemKind.BoxSet,
            BaseItemKind.Playlist,
            BaseItemKind.CollectionFolder,
            BaseItemKind.Video,
            BaseItemKind.Movie,
            BaseItemKind.Episode,
            BaseItemKind.MusicVideo,
            BaseItemKind.Trailer
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

    /// <inheritdoc />
    public IQueryable<BaseItemEntity> ApplyOrder(IQueryable<BaseItemEntity> query, InternalItemsQuery filter, JellyfinDbContext context)
    {
        var orderBy = filter.OrderBy.Where(e => e.OrderBy != ItemSortBy.Default).ToArray();
        var hasSearch = !string.IsNullOrEmpty(filter.SearchTerm);

        // SeriesDatePlayed requires special handling to avoid correlated subqueries.
        // Instead of running a MAX() subquery per-row in ORDER BY, we pre-aggregate
        // max played dates per series in one query and left-join it.
        if (!hasSearch && orderBy.Any(o => o.OrderBy == ItemSortBy.SeriesDatePlayed))
        {
            return ApplySeriesDatePlayedOrder(query, filter, context, orderBy);
        }

        IOrderedQueryable<BaseItemEntity>? orderedQuery = null;

        if (hasSearch)
        {
            var relevanceExpression = OrderMapper.MapSearchRelevanceOrder(filter.SearchTerm!);
            orderedQuery = query.OrderBy(relevanceExpression);
        }

        // Folders carry no played flag of their own, so these two keys go through the same predicate
        // the isPlayed filter uses rather than through the stored-column lookup in OrderMapper.
        Expression<Func<BaseItemEntity, object?>> MapOrderByField(ItemSortBy sortBy) => sortBy switch
        {
            ItemSortBy.IsPlayed when filter.User is not null
                => AsOrderKey(BuildIsPlayedFilter(context, filter.User)),
            ItemSortBy.IsUnplayed when filter.User is not null
                => AsOrderKey(BuildIsPlayedFilter(context, filter.User).Not()),
            _ => OrderMapper.MapOrderByField(sortBy, filter, context)
        };

        if (orderBy.Length > 0)
        {
            var firstOrdering = orderBy[0];
            var expression = MapOrderByField(firstOrdering.OrderBy);

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
                expression = MapOrderByField(item.OrderBy);
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

    private IQueryable<BaseItemEntity> ApplySeriesDatePlayedOrder(
        IQueryable<BaseItemEntity> query,
        InternalItemsQuery filter,
        JellyfinDbContext context,
        (ItemSortBy OrderBy, SortOrder SortOrder)[] orderBy)
    {
        // Pre-aggregate max played date per series key in ONE query.
        // This generates a single: SELECT SeriesPresentationUniqueKey, MAX(LastPlayedDate) ... GROUP BY
        // instead of a correlated subquery per outer row.
        IQueryable<UserData> userDataQuery = filter.User is not null
            ? context.UserData.Where(ud => ud.UserId == filter.User.Id && ud.Played)
            : context.UserData.Where(ud => ud.Played);

        var seriesMaxDates = userDataQuery
            .Join(
                context.BaseItems,
                ud => ud.ItemId,
                bi => bi.Id,
                (ud, bi) => new { bi.SeriesPresentationUniqueKey, ud.LastPlayedDate })
            .Where(x => x.SeriesPresentationUniqueKey != null)
            .GroupBy(x => x.SeriesPresentationUniqueKey)
            .Select(g => new { SeriesKey = g.Key!, MaxDate = g.Max(x => x.LastPlayedDate) });

        var joined = query.LeftJoin(
            seriesMaxDates,
            e => e.PresentationUniqueKey,
            s => s.SeriesKey,
            (e, s) => new { Item = e, MaxDate = s != null ? s.MaxDate : (DateTime?)null });

        var seriesSort = orderBy.First(o => o.OrderBy == ItemSortBy.SeriesDatePlayed);

        return seriesSort.SortOrder == SortOrder.Ascending
            ? joined.OrderBy(x => x.MaxDate).ThenBy(x => x.Item.SortName).Select(x => x.Item)
            : joined.OrderByDescending(x => x.MaxDate).ThenBy(x => x.Item.SortName).Select(x => x.Item);
    }

    /// <summary>
    /// Builds a query for descendants of an ancestor with user access filtering applied.
    /// Uses recursive CTE to traverse both hierarchical (AncestorIds) and linked (LinkedChildren) relationships.
    /// </summary>
    /// <inheritdoc />
    public IQueryable<BaseItemEntity> BuildAccessFilteredDescendantsQuery(
        JellyfinDbContext context,
        InternalItemsQuery filter,
        Guid ancestorId)
    {
        // Use recursive CTE to get all descendants (hierarchical and linked)
        var allDescendantIds = DescendantQueryHelper.GetAllDescendantIds(context, ancestorId);

        var baseQuery = context.BaseItems
            .AsNoTracking()
            .Where(b => allDescendantIds.Contains(b.Id))
            .Where(DescendantQueryHelper.IsCountableLeaf);

        return ApplyAccessFiltering(context, baseQuery, filter);
    }

    /// <summary>
    /// Checks whether the user restricts access to items by parental rating or tags.
    /// </summary>
    /// <param name="filter">The query filter.</param>
    /// <returns><c>true</c> if the query carries parental restrictions.</returns>
    private static bool RequiresParentalRestrictions(InternalItemsQuery filter)
        => filter.IncludeInheritedTags.Length > 0
            || filter.ExcludeInheritedTags.Length > 0
            || filter.MaxParentalRating is not null
            || filter.BlockUnratedItems.Length > 0;

    /// <summary>
    /// Applies user access filtering to a query.
    /// Includes TopParentIds, parental rating, and tag filtering.
    /// </summary>
    /// <inheritdoc />
    public IQueryable<BaseItemEntity> ApplyAccessFiltering(
        JellyfinDbContext context,
        IQueryable<BaseItemEntity> baseQuery,
        InternalItemsQuery filter)
    {
        baseQuery = ApplyTopParentFiltering(context, baseQuery, filter);

        baseQuery = ApplyParentalRestrictions(context, baseQuery, filter);

        // Exclude alternate versions (have PrimaryVersionId set) and owned non-extra items.
        // Extras (trailers, etc.) have OwnerId set but also have ExtraType set — keep those.
        if (!filter.IncludeOwnedItems)
        {
            baseQuery = baseQuery.Where(e => e.PrimaryVersionId == null && (e.OwnerId == null || e.ExtraType != null));
        }

        return baseQuery;
    }

    /// <summary>
    /// Restricts a query to the libraries the user may open, exempting requested by-name items.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="baseQuery">The query to filter.</param>
    /// <param name="filter">The query filter.</param>
    /// <returns>The filtered query.</returns>
    private IQueryable<BaseItemEntity> ApplyTopParentFiltering(
        JellyfinDbContext context,
        IQueryable<BaseItemEntity> baseQuery,
        InternalItemsQuery filter)
    {
        var queryTopParentIds = filter.TopParentIds;
        if (queryTopParentIds.Length == 0)
        {
            return baseQuery;
        }

        var exemptedItemByNameTypes = GetExemptedItemByNameTypes(filter);
        if (exemptedItemByNameTypes.Count == 0)
        {
            return baseQuery.WhereOneOrMany(queryTopParentIds, e => e.TopParentId!.Value);
        }

        baseQuery = baseQuery.Where(e => exemptedItemByNameTypes.Contains(e.Type) || queryTopParentIds.Any(w => w == e.TopParentId!.Value));
        if (filter.UserHasContentRestrictions)
        {
            baseQuery = ApplyItemByNameAccessFiltering(baseQuery, context, filter, exemptedItemByNameTypes, queryTopParentIds);
        }

        return baseQuery;
    }

    /// <summary>
    /// Returns the by-name types a query asks for, which carry no TopParentId to filter on.
    /// </summary>
    /// <param name="filter">The query filter.</param>
    /// <returns>The type names exempt from library filtering.</returns>
    private List<string> GetExemptedItemByNameTypes(InternalItemsQuery filter)
    {
        var includedItemByNameTypes = GetItemByNameTypesInQuery(filter);
        if ((filter.IncludeItemsByName ?? false) && includedItemByNameTypes.Count > 0)
        {
            return includedItemByNameTypes;
        }

        return _itemByNameKinds.Where(filter.IncludeItemTypes.Contains).Select(e => _itemTypeLookup.BaseItemKindNames[e]!).ToList();
    }

    /// <summary>
    /// Keeps a by-name row only when at least one item behind its name is reachable for the user.
    /// </summary>
    /// <param name="baseQuery">The query to filter.</param>
    /// <param name="context">The database context.</param>
    /// <param name="filter">The query filter.</param>
    /// <param name="itemByNameTypes">The exempted by-name type names.</param>
    /// <param name="topParentIds">The libraries the user may open.</param>
    /// <returns>The filtered query.</returns>
    private IQueryable<BaseItemEntity> ApplyItemByNameAccessFiltering(
        IQueryable<BaseItemEntity> baseQuery,
        JellyfinDbContext context,
        InternalItemsQuery filter,
        IReadOnlyList<string> itemByNameTypes,
        Guid[] topParentIds)
    {
        // IncludeOwnedItems: a credit on an alternate version of a reachable movie still counts.
        var accessibleItems = ApplyAccessFiltering(
            context,
            context.BaseItems.AsNoTracking(),
            new InternalItemsQuery(filter.User) { TopParentIds = topParentIds, IncludeOwnedItems = true });

        // People and artists are reached through the credits pointing at them.
        foreach (var kind in _creditedByNameKinds)
        {
            var typeName = _itemTypeLookup.BaseItemKindNames[kind];
            if (!itemByNameTypes.Contains(typeName))
            {
                continue;
            }

            baseQuery = baseQuery.Where(e => e.Type != typeName
                || context.Peoples.Any(p => p.ItemId.Equals(e.Id)
                    && context.PeopleBaseItemMap.Any(m => m.PeopleId == p.Id && accessibleItems.Any(i => i.Id == m.ItemId))));
        }

        foreach (var (kind, valueTypes) in _itemByNameValueTypes)
        {
            var typeName = _itemTypeLookup.BaseItemKindNames[kind];
            if (!itemByNameTypes.Contains(typeName))
            {
                continue;
            }

            baseQuery = baseQuery.Where(e => e.Type != typeName
                || context.ItemValues.Any(v => valueTypes.Contains(v.Type) && v.CleanValue == e.CleanName
                    && context.ItemValuesMap.Any(m => m.ItemValueId == v.ItemValueId && accessibleItems.Any(i => i.Id == m.ItemId))));
        }

        return baseQuery;
    }

    /// <summary>
    /// Applies the user's parental rating and tag restrictions to a query.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="baseQuery">The query to filter.</param>
    /// <param name="filter">The query filter.</param>
    /// <returns>The filtered query.</returns>
    private IQueryable<BaseItemEntity> ApplyParentalRestrictions(
        JellyfinDbContext context,
        IQueryable<BaseItemEntity> baseQuery,
        InternalItemsQuery filter)
    {
        // Apply parental rating filtering
        if (filter.MaxParentalRating is not null)
        {
            baseQuery = baseQuery.Where(BuildMaxParentalRatingFilter(context, filter.MaxParentalRating));
        }

        // Apply block unrated items filtering
        if (filter.BlockUnratedItems.Length > 0)
        {
            var unratedItemTypes = filter.BlockUnratedItems.Select(f => f.ToString()).ToArray();
            baseQuery = baseQuery.Where(e =>
                e.InheritedParentalRatingValue != null || !unratedItemTypes.Contains(e.UnratedType));
        }

        // Apply excluded tags filtering (blocked tags).
        // Pre-build the blocked-item-id set as a sub-select; then four index-seek Contains checks
        // instead of one EXISTS over a 4-way OR predicate that defeats index seeks.
        if (filter.ExcludeInheritedTags.Length > 0)
        {
            var excludedTags = filter.ExcludeInheritedTags.Select(e => e.GetCleanValue()).ToArray();
            var blockedTagItemIds = context.ItemValuesMap
                .Where(f => f.ItemValue.Type == ItemValueType.Tags && excludedTags.Contains(f.ItemValue.CleanValue))
                .Select(f => f.ItemId);

            baseQuery = baseQuery.Where(e =>
                !blockedTagItemIds.Contains(e.Id)
                && !(e.SeriesId.HasValue && blockedTagItemIds.Contains(e.SeriesId.Value))
                && !e.Parents!.Any(p => blockedTagItemIds.Contains(p.ParentItemId))
                && !(e.TopParentId.HasValue && blockedTagItemIds.Contains(e.TopParentId.Value)));
        }

        // Apply included tags filtering (allowed tags - item must have at least one).
        if (filter.IncludeInheritedTags.Length > 0)
        {
            var includeTags = filter.IncludeInheritedTags.Select(e => e.GetCleanValue()).ToArray();
            var personTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Person];
            var allowedTagItemIds = context.ItemValuesMap
                .Where(f => f.ItemValue.Type == ItemValueType.Tags && includeTags.Contains(f.ItemValue.CleanValue))
                .Select(f => f.ItemId);

            baseQuery = baseQuery.Where(e =>
                allowedTagItemIds.Contains(e.Id)
                || (e.SeriesId.HasValue && allowedTagItemIds.Contains(e.SeriesId.Value))
                || e.Parents!.Any(p => allowedTagItemIds.Contains(p.ParentItemId))
                || (e.TopParentId.HasValue && allowedTagItemIds.Contains(e.TopParentId.Value))

                // People don't carry the tags of the media they appear in and would never match
                || e.Type == personTypeName);
        }

        return baseQuery;
    }

    /// <summary>
    /// Builds a filter expression for max parental rating that handles both rated items
    /// and unrated BoxSets/Playlists (which check linked children's ratings).
    /// </summary>
    private static Expression<Func<BaseItemEntity, bool>> BuildMaxParentalRatingFilter(
        JellyfinDbContext context,
        ParentalRatingScore maxRating)
    {
        var maxScore = maxRating.Score;
        var maxSubScore = maxRating.SubScore ?? 0;
        var linkedChildren = context.LinkedChildren;

        return e =>
            // Item has a rating: check against limit
            (e.InheritedParentalRatingValue != null
                && (e.InheritedParentalRatingValue < maxScore
                    || (e.InheritedParentalRatingValue == maxScore && (e.InheritedParentalRatingSubValue ?? 0) <= maxSubScore)))
            // Item has no rating
            || (e.InheritedParentalRatingValue == null
                && (
                    // No linked children (not a BoxSet/Playlist): pass as unrated
                    !linkedChildren.Any(lc => lc.ParentId == e.Id)
                    // Has linked children: at least one child must be within limits
                    || linkedChildren.Any(lc => lc.ParentId == e.Id
                        && (lc.Child!.InheritedParentalRatingValue == null
                            || lc.Child.InheritedParentalRatingValue < maxScore
                            || (lc.Child.InheritedParentalRatingValue == maxScore
                                && (lc.Child.InheritedParentalRatingSubValue ?? 0) <= maxSubScore)))));
    }

    /// <inheritdoc />
    public IQueryable<BaseItemEntity> GetAccessFilteredLeafItemsQuery(JellyfinDbContext context, User user, bool includeOwnedItems = false)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(user);

        var leafItems = context.BaseItems
            .AsNoTracking()
            .Where(DescendantQueryHelper.IsCountableLeaf);

        return ApplyAccessFiltering(context, leafItems, new InternalItemsQuery(user) { IncludeOwnedItems = includeOwnedItems });
    }

    private static Expression<Func<BaseItemEntity, object?>> AsOrderKey(Expression<Func<BaseItemEntity, bool>> predicate)
        => Expression.Lambda<Func<BaseItemEntity, object?>>(Expression.Convert(predicate.Body, typeof(object)), predicate.Parameters);

    /// <inheritdoc />
    public Expression<Func<BaseItemEntity, bool>> BuildHasDescendantFilter(JellyfinDbContext context, IQueryable<BaseItemEntity> descendants)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(descendants);

        // Descendants are reachable through the ancestor chain and - for BoxSets and Playlists - as
        // linked children, which can themselves be folders contributing their own descendants.
        // Every step is a correlated index seek, so only the rows the outer query keeps are visited
        // and a folder is left as soon as its first matching descendant is found.
        return e => context.AncestorIds.Any(a => a.ParentItemId == e.Id && descendants.Any(d => d.Id == a.ItemId))
            || context.LinkedChildren.Any(lc => lc.ParentId == e.Id
                && (descendants.Any(d => d.Id == lc.ChildId)
                    || context.AncestorIds.Any(a => a.ParentItemId == lc.ChildId && descendants.Any(d => d.Id == a.ItemId))));
    }
}
