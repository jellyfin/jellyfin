#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Linq;
using System.Linq.Expressions;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Server.Implementations.Item;

/// <summary>
/// Extension methods for applying folder-aware filters that check items and their descendants.
/// </summary>
internal static class FolderAwareFilterExtensions
{
    /// <summary>
    /// Filters items where either the item matches the condition (for non-folders)
    /// or any descendant matches (for folders). Uses reverse traversal through AncestorIds.
    /// </summary>
    /// <param name="query">The query to filter.</param>
    /// <param name="context">The database context.</param>
    /// <param name="condition">The condition to check on BaseItemEntity.</param>
    /// <returns>Filtered query.</returns>
    public static IQueryable<BaseItemEntity> WhereItemOrDescendantMatches(
        this IQueryable<BaseItemEntity> query,
        JellyfinDbContext context,
        Expression<Func<BaseItemEntity, bool>> condition)
    {
        // Use correlated Any() subqueries instead of UNION + Contains for better index utilization
        var matchingIds = context.BaseItems.Where(condition).Select(b => b.Id);

        return query.Where(e =>
            matchingIds.Contains(e.Id)
            || context.AncestorIds.Any(a => a.ParentItemId == e.Id && matchingIds.Contains(a.ItemId))
            || context.LinkedChildren.Any(lc => lc.ParentId == e.Id && matchingIds.Contains(lc.ChildId)));
    }

    /// <summary>
    /// Filters items where neither the item matches the condition (for non-folders)
    /// nor any descendant matches (for folders). Uses reverse traversal for infinite depth.
    /// </summary>
    /// <param name="query">The query to filter.</param>
    /// <param name="context">The database context.</param>
    /// <param name="condition">The condition that should NOT match.</param>
    /// <returns>Filtered query.</returns>
    public static IQueryable<BaseItemEntity> WhereNeitherItemNorDescendantMatches(
        this IQueryable<BaseItemEntity> query,
        JellyfinDbContext context,
        Expression<Func<BaseItemEntity, bool>> condition)
    {
        var matchingIds = context.BaseItems.Where(condition).Select(b => b.Id);

        return query.Where(e =>
            !matchingIds.Contains(e.Id)
            && !context.AncestorIds.Any(a => a.ParentItemId == e.Id && matchingIds.Contains(a.ItemId))
            && !context.LinkedChildren.Any(lc => lc.ParentId == e.Id && matchingIds.Contains(lc.ChildId)));
    }
}
