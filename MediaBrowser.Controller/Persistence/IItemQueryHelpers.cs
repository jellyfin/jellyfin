using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Persistence;

/// <summary>
/// Provides shared query-building methods used by extracted item services.
/// Implemented by <c>BaseItemRepository</c>.
/// </summary>
public interface IItemQueryHelpers
{
    /// <summary>
    /// Translates an <see cref="InternalItemsQuery"/> into EF Core filter expressions.
    /// </summary>
    /// <param name="baseQuery">The base queryable to filter.</param>
    /// <param name="context">The database context.</param>
    /// <param name="filter">The query filter.</param>
    /// <returns>The filtered queryable.</returns>
    IQueryable<BaseItemEntity> TranslateQuery(
        IQueryable<BaseItemEntity> baseQuery,
        JellyfinDbContext context,
        InternalItemsQuery filter);

    /// <summary>
    /// Prepares a base query for items from the context.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="filter">The query filter.</param>
    /// <returns>The prepared queryable.</returns>
    IQueryable<BaseItemEntity> PrepareItemQuery(JellyfinDbContext context, InternalItemsQuery filter);

    /// <summary>
    /// Applies user access filtering (library access, parental controls, tags) to a query.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="baseQuery">The base queryable to filter.</param>
    /// <param name="filter">The query filter containing access settings.</param>
    /// <returns>The access-filtered queryable.</returns>
    IQueryable<BaseItemEntity> ApplyAccessFiltering(
        JellyfinDbContext context,
        IQueryable<BaseItemEntity> baseQuery,
        InternalItemsQuery filter);

    /// <summary>
    /// Applies navigation property includes to a query based on filter options.
    /// </summary>
    /// <param name="dbQuery">The queryable to apply navigations to.</param>
    /// <param name="filter">The query filter.</param>
    /// <returns>The queryable with navigation includes.</returns>
    IQueryable<BaseItemEntity> ApplyNavigations(
        IQueryable<BaseItemEntity> dbQuery,
        InternalItemsQuery filter);

    /// <summary>
    /// Applies ordering to a query based on filter options.
    /// </summary>
    /// <param name="query">The queryable to order.</param>
    /// <param name="filter">The query filter.</param>
    /// <param name="context">The database context.</param>
    /// <returns>The ordered queryable.</returns>
    IQueryable<BaseItemEntity> ApplyOrder(
        IQueryable<BaseItemEntity> query,
        InternalItemsQuery filter,
        JellyfinDbContext context);

    /// <summary>
    /// Builds a query for descendants of an ancestor with user access filtering applied.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="filter">The query filter.</param>
    /// <param name="ancestorId">The ancestor item ID.</param>
    /// <returns>The filtered descendant queryable.</returns>
    IQueryable<BaseItemEntity> BuildAccessFilteredDescendantsQuery(
        JellyfinDbContext context,
        InternalItemsQuery filter,
        Guid ancestorId);

    /// <summary>
    /// Builds a query for the playable leaf items a user can access.
    /// </summary>
    /// <param name="context">The database context the resulting query is bound to.</param>
    /// <param name="user">The user to filter accessible items for.</param>
    /// <param name="includeOwnedItems">Whether to include alternate versions and owned items.</param>
    /// <returns>The access-filtered leaf item queryable.</returns>
    IQueryable<BaseItemEntity> GetAccessFilteredLeafItemsQuery(
        JellyfinDbContext context,
        User user,
        bool includeOwnedItems = false);

    /// <summary>
    /// Builds a filter matching items that have at least one of <paramref name="descendants"/> below them.
    /// </summary>
    /// <param name="context">The database context the resulting filter is bound to.</param>
    /// <param name="descendants">A query yielding the descendants to look for.</param>
    /// <returns>A filter expression matching items with a matching descendant.</returns>
    Expression<Func<BaseItemEntity, bool>> BuildHasDescendantFilter(
        JellyfinDbContext context,
        IQueryable<BaseItemEntity> descendants);

    /// <summary>
    /// Reads the collections a result set's items own - their links, provider ids, locked fields,
    /// user data and images - one statement per collection, and attaches them.
    /// </summary>
    /// <remarks>
    /// Item queries do not join these, because together their row count is the product of each
    /// item's child counts and every row in it repeats the whole item row. Any path that
    /// materializes items has to call this, or they come back with those collections unread - which
    /// renders wrong rather than failing, so it is covered by a test that enumerates the query
    /// entry points.
    /// </remarks>
    /// <param name="context">The database context to read from.</param>
    /// <param name="filter">The query whose DtoOptions decide which collections to read.</param>
    /// <param name="items">The materialized items to attach the collections to.</param>
    /// <returns>The same items, for chaining.</returns>
    IReadOnlyList<BaseItem> LoadCollections(JellyfinDbContext context, InternalItemsQuery filter, IReadOnlyList<BaseItem> items);

    /// <summary>
    /// Deserializes a <see cref="BaseItemEntity"/> into a <see cref="BaseItem"/>.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <param name="skipDeserialization">Whether to skip JSON deserialization.</param>
    /// <returns>The deserialized item, or null.</returns>
    BaseItem? DeserializeBaseItem(BaseItemEntity entity, bool skipDeserialization = false);

    /// <summary>
    /// Prepares a filter query by adjusting limits and virtual item settings.
    /// </summary>
    /// <param name="query">The query to prepare.</param>
    void PrepareFilterQuery(InternalItemsQuery query);
}
