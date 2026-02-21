using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.MatchCriteria;

namespace Jellyfin.Database.Implementations;

/// <summary>
/// Provides methods for querying item hierarchies using iterative traversal.
/// Uses AncestorIds and LinkedChildren tables for parent-child traversal.
/// </summary>
public static class DescendantQueryHelper
{
    /// <summary>
    /// Gets a queryable of all descendant IDs for a parent item.
    /// Traverses AncestorIds and LinkedChildren to find all descendants.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="parentId">Parent item ID.</param>
    /// <returns>Queryable of descendant item IDs.</returns>
    public static IQueryable<Guid> GetAllDescendantIds(JellyfinDbContext context, Guid parentId)
    {
        ArgumentNullException.ThrowIfNull(context);

        var descendants = TraverseHierarchyDown(context, [parentId]);

        descendants.Remove(parentId);

        return descendants.AsQueryable();
    }

    /// <summary>
    /// Gets a queryable of all owned descendant IDs for a parent item.
    /// Traverses only AncestorIds (hierarchical ownership), NOT LinkedChildren (associations).
    /// Use this for deletion to avoid destroying items that are merely linked (e.g. movies in a BoxSet).
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="parentId">Parent item ID.</param>
    /// <returns>Queryable of owned descendant item IDs.</returns>
    public static IQueryable<Guid> GetOwnedDescendantIds(JellyfinDbContext context, Guid parentId)
    {
        ArgumentNullException.ThrowIfNull(context);

        var descendants = TraverseHierarchyDownOwned(context, [parentId]);

        descendants.Remove(parentId);

        return descendants.AsQueryable();
    }

    /// <summary>
    /// Gets a queryable of all folder IDs that have any descendant matching the specified criteria.
    /// Can be used in LINQ .Contains() expressions.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="criteria">The matching criteria to apply.</param>
    /// <returns>Queryable of folder IDs.</returns>
    public static IQueryable<Guid> GetFolderIdsMatching(JellyfinDbContext context, FolderMatchCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(criteria);
        var matchingItemIds = criteria switch
        {
            HasSubtitles => context.MediaStreamInfos
                .Where(ms => ms.StreamType == MediaStreamTypeEntity.Subtitle)
                .Select(ms => ms.ItemId)
                .Distinct()
                .ToHashSet(),
            HasChapterImages => context.Chapters
                .Where(c => c.ImagePath != null)
                .Select(c => c.ItemId)
                .Distinct()
                .ToHashSet(),
            HasMediaStreamType m => GetMatchingMediaStreamItemIds(context, m),
            _ => throw new ArgumentOutOfRangeException(nameof(criteria), $"Unknown criteria type: {criteria.GetType().Name}")
        };

        var ancestors = TraverseHierarchyUp(context, matchingItemIds);

        return ancestors.AsQueryable();
    }

    private static HashSet<Guid> GetMatchingMediaStreamItemIds(JellyfinDbContext context, HasMediaStreamType criteria)
    {
        var query = context.MediaStreamInfos
            .Where(ms => ms.StreamType == criteria.StreamType && ms.Language == criteria.Language);

        if (criteria.IsExternal.HasValue)
        {
            var isExternal = criteria.IsExternal.Value;
            query = query.Where(ms => ms.IsExternal == isExternal);
        }

        return query.Select(ms => ms.ItemId).Distinct().ToHashSet();
    }

    /// <summary>
    /// Traverses DOWN the hierarchy from parent folders to find all descendants.
    /// </summary>
    private static HashSet<Guid> TraverseHierarchyDown(JellyfinDbContext context, ICollection<Guid> startIds)
    {
        var visited = new HashSet<Guid>(startIds);
        var folderStack = new HashSet<Guid>(startIds);

        while (folderStack.Count != 0)
        {
            var currentFolders = folderStack.ToArray();
            folderStack.Clear();

            var directChildren = context.AncestorIds
                .WhereOneOrMany(currentFolders, e => e.ParentItemId)
                .Select(e => e.ItemId)
                .ToArray();

            var linkedChildren = context.LinkedChildren
                .WhereOneOrMany(currentFolders, e => e.ParentId)
                .Select(e => e.ChildId)
                .ToArray();

            var allChildren = directChildren.Concat(linkedChildren).Distinct().ToArray();

            if (allChildren.Length == 0)
            {
                break;
            }

            var childFolders = context.BaseItems
                .WhereOneOrMany(allChildren, e => e.Id)
                .Where(e => e.IsFolder)
                .Select(e => e.Id)
                .ToHashSet();

            foreach (var childId in allChildren)
            {
                if (visited.Add(childId) && childFolders.Contains(childId))
                {
                    folderStack.Add(childId);
                }
            }
        }

        return visited;
    }

    /// <summary>
    /// Traverses DOWN the hierarchy using only AncestorIds (ownership), not LinkedChildren.
    /// </summary>
    private static HashSet<Guid> TraverseHierarchyDownOwned(JellyfinDbContext context, ICollection<Guid> startIds)
    {
        var visited = new HashSet<Guid>(startIds);
        var folderStack = new HashSet<Guid>(startIds);

        while (folderStack.Count != 0)
        {
            var currentFolders = folderStack.ToArray();
            folderStack.Clear();

            var directChildren = context.AncestorIds
                .WhereOneOrMany(currentFolders, e => e.ParentItemId)
                .Select(e => e.ItemId)
                .ToArray();

            if (directChildren.Length == 0)
            {
                break;
            }

            var childFolders = context.BaseItems
                .WhereOneOrMany(directChildren, e => e.Id)
                .Where(e => e.IsFolder)
                .Select(e => e.Id)
                .ToHashSet();

            foreach (var childId in directChildren)
            {
                if (visited.Add(childId) && childFolders.Contains(childId))
                {
                    folderStack.Add(childId);
                }
            }
        }

        return visited;
    }

    /// <summary>
    /// Traverses UP the hierarchy from items to find all ancestor folders.
    /// </summary>
    private static HashSet<Guid> TraverseHierarchyUp(JellyfinDbContext context, ICollection<Guid> startIds)
    {
        var ancestors = new HashSet<Guid>();
        var itemStack = new HashSet<Guid>(startIds);

        while (itemStack.Count != 0)
        {
            var currentItems = itemStack.ToArray();
            itemStack.Clear();

            var ancestorParents = context.AncestorIds
                .WhereOneOrMany(currentItems, e => e.ItemId)
                .Select(e => e.ParentItemId)
                .ToArray();

            var linkedParents = context.LinkedChildren
                .WhereOneOrMany(currentItems, e => e.ChildId)
                .Select(e => e.ParentId)
                .ToArray();

            foreach (var parentId in ancestorParents.Concat(linkedParents))
            {
                if (ancestors.Add(parentId))
                {
                    itemStack.Add(parentId);
                }
            }
        }

        return ancestors;
    }
}
