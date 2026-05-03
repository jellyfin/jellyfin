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
        dbQuery = ApplyQueryPaging(dbQuery, filter);
        dbQuery = ApplyNavigations(dbQuery, filter);
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

    private IQueryable<BaseItemEntity> ApplyGroupingFilter(JellyfinDbContext context, IQueryable<BaseItemEntity> dbQuery, InternalItemsQuery filter)
    {
        // Collapse duplicates sharing a presentation key (e.g. alternate versions) by picking
        // the min Id per group. Keep the grouped ids as an IQueryable sub-select; materializing
        // to a List would inline one bound parameter per id and hit SQLite's variable cap.
        var enableGroupByPresentationUniqueKey = EnableGroupByPresentationUniqueKey(filter);
        if (enableGroupByPresentationUniqueKey && filter.GroupBySeriesPresentationUniqueKey)
        {
            var groupedIds = dbQuery.GroupBy(e => new { e.PresentationUniqueKey, e.SeriesPresentationUniqueKey }).Select(e => e.Min(x => x.Id));
            dbQuery = context.BaseItems.AsNoTracking().Where(e => groupedIds.Contains(e.Id));
        }
        else if (enableGroupByPresentationUniqueKey)
        {
            var groupedIds = dbQuery.GroupBy(e => e.PresentationUniqueKey).Select(e => e.Min(x => x.Id));
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
            .Where(b => allDescendantIds.Contains(b.Id) && !b.IsFolder && !b.IsVirtualItem);

        return ApplyAccessFiltering(context, baseQuery, filter);
    }

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
        // Apply TopParentIds filtering (library folder access)
        if (filter.TopParentIds.Length > 0)
        {
            var topParentIds = filter.TopParentIds;
            baseQuery = baseQuery.Where(e => topParentIds.Contains(e.TopParentId!.Value));
        }

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
            var allowedTagItemIds = context.ItemValuesMap
                .Where(f => f.ItemValue.Type == ItemValueType.Tags && includeTags.Contains(f.ItemValue.CleanValue))
                .Select(f => f.ItemId);

            baseQuery = baseQuery.Where(e =>
                allowedTagItemIds.Contains(e.Id)
                || (e.SeriesId.HasValue && allowedTagItemIds.Contains(e.SeriesId.Value))
                || e.Parents!.Any(p => allowedTagItemIds.Contains(p.ParentItemId))
                || (e.TopParentId.HasValue && allowedTagItemIds.Contains(e.TopParentId.Value)));
        }

        // Exclude alternate versions (have PrimaryVersionId set) and owned non-extra items.
        // Extras (trailers, etc.) have OwnerId set but also have ExtraType set — keep those.
        if (!filter.IncludeOwnedItems)
        {
            baseQuery = baseQuery.Where(e => e.PrimaryVersionId == null && (e.OwnerId == null || e.ExtraType != null));
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
    public IQueryable<Guid> GetFullyPlayedFolderIdsQuery(JellyfinDbContext context, IQueryable<Guid> folderIds, User user)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(folderIds);
        ArgumentNullException.ThrowIfNull(user);

        var filter = new InternalItemsQuery(user);
        var userId = user.Id;

        var leafItems = context.BaseItems
            .AsNoTracking()
            .Where(b => !b.IsFolder && !b.IsVirtualItem);
        leafItems = ApplyAccessFiltering(context, leafItems, filter);

        var playedLeafItems = leafItems
            .Select(b => new { b.Id, Played = b.UserData!.Any(ud => ud.UserId == userId && ud.Played) });

        var ancestorLeaves = context.AncestorIds
            .Where(a => folderIds.Contains(a.ParentItemId))
            .Join(
                playedLeafItems,
                a => a.ItemId,
                b => b.Id,
                (a, b) => new { FolderId = a.ParentItemId, b.Id, b.Played });

        var linkedLeaves = context.LinkedChildren
            .Where(lc => folderIds.Contains(lc.ParentId))
            .Join(
                playedLeafItems,
                lc => lc.ChildId,
                b => b.Id,
                (lc, b) => new { FolderId = lc.ParentId, b.Id, b.Played });

        var linkedFolderLeaves = context.LinkedChildren
            .Where(lc => folderIds.Contains(lc.ParentId))
            .Join(
                context.BaseItems.Where(b => b.IsFolder),
                lc => lc.ChildId,
                b => b.Id,
                (lc, b) => new { lc.ParentId, FolderChildId = b.Id })
            .Join(
                context.AncestorIds,
                x => x.FolderChildId,
                a => a.ParentItemId,
                (x, a) => new { x.ParentId, DescendantId = a.ItemId })
            .Join(
                playedLeafItems,
                x => x.DescendantId,
                b => b.Id,
                (x, b) => new { FolderId = x.ParentId, b.Id, b.Played });

        return ancestorLeaves
            .Union(linkedLeaves)
            .Union(linkedFolderLeaves)
            .GroupBy(x => x.FolderId)
            .Where(g => g.Select(x => x.Id).Distinct().Count() == g.Where(x => x.Played).Select(x => x.Id).Distinct().Count())
            .Select(g => g.Key);
    }
}
