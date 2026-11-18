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
        // Get IDs of items that directly match the condition
        var directMatchIds = context.BaseItems.Where(condition).Select(b => b.Id);

        // Get parent IDs where a descendant (via AncestorIds) matches
        var ancestorMatchIds = context.AncestorIds
            .Where(a => directMatchIds.Contains(a.ItemId))
            .Select(a => a.ParentItemId);

        // Get parent IDs where a linked child matches
        var linkedMatchIds = context.LinkedChildren
            .Where(lc => directMatchIds.Contains(lc.ChildId))
            .Select(lc => lc.ParentId);

        var allMatchingIds = directMatchIds
            .Concat(ancestorMatchIds)
            .Concat(linkedMatchIds)
            .Distinct();

        return query.Where(e => allMatchingIds.Contains(e.Id));
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
        // Get IDs of items that directly match the condition
        var directMatchIds = context.BaseItems.Where(condition).Select(b => b.Id);

        // Get parent IDs where a descendant (via AncestorIds) matches
        var ancestorMatchIds = context.AncestorIds
            .Where(a => directMatchIds.Contains(a.ItemId))
            .Select(a => a.ParentItemId);

        // Get parent IDs where a linked child matches
        var linkedMatchIds = context.LinkedChildren
            .Where(lc => directMatchIds.Contains(lc.ChildId))
            .Select(lc => lc.ParentId);

        var allMatchingIds = directMatchIds
            .Concat(ancestorMatchIds)
            .Concat(linkedMatchIds)
            .Distinct();

        return query.Where(e => !allMatchingIds.Contains(e.Id));
    }
}
