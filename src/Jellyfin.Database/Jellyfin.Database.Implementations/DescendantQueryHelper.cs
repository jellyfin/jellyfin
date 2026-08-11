using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.MatchCriteria;

namespace Jellyfin.Database.Implementations;

/// <summary>
/// Provides methods for querying item hierarchies.
/// Uses AncestorIds and LinkedChildren tables for parent-child traversal.
/// </summary>
public static class DescendantQueryHelper
{
    /// <summary>
    /// Gets the predicate identifying items that count toward played/total aggregation:
    /// real leaf media, i.e. neither folders nor virtual items (missing or unaired episodes).
    /// Shared by the per-item and batched count paths so they cannot diverge.
    /// </summary>
    public static Expression<Func<BaseItemEntity, bool>> IsCountableLeaf { get; } =
        b => !b.IsFolder && !b.IsVirtualItem;

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

        var (closureRoots, linkRoots) = ResolveLinkedRoots(context, parentId);

        var hierarchyDescendants = ClosureDescendants(context, closureRoots);

        var linkedDescendants = context.LinkedChildren
            .WhereOneOrMany(linkRoots, e => e.ParentId)
            .Select(e => e.ChildId);

        return hierarchyDescendants
            .Concat(linkedDescendants)
            .Where(e => !e.Equals(parentId))
            .Distinct();
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

        return ClosureDescendants(context, [parentId])
            .Where(e => !e.Equals(parentId))
            .Distinct();
    }

    /// <summary>
    /// Gets all owned descendant IDs for multiple parent items in a single traversal.
    /// More efficient than calling <see cref="GetOwnedDescendantIds"/> per parent because
    /// it performs one traversal for all seeds instead of N separate traversals.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="parentIds">Parent item IDs.</param>
    /// <returns>Set of all owned descendant item IDs (excluding the parent IDs themselves).</returns>
    public static HashSet<Guid> GetOwnedDescendantIdsBatch(JellyfinDbContext context, IReadOnlyList<Guid> parentIds)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(parentIds);

        if (parentIds.Count == 0)
        {
            return [];
        }

        var descendants = ClosureDescendants(context, parentIds)
            .Distinct()
            .ToHashSet();

        // The callers want only descendants, and an item is never its own descendant.
        descendants.ExceptWith(parentIds);

        return descendants;
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
                .Select(ms => ms.ItemId),
            HasChapterImages => context.Chapters
                .Where(c => c.ImagePath != null)
                .Select(c => c.ItemId),
            HasMediaStreamType m => GetMatchingMediaStreamItemIds(context, m),
            _ => throw new ArgumentOutOfRangeException(nameof(criteria), $"Unknown criteria type: {criteria.GetType().Name}")
        };

        // One hop up the closure covers every ancestor level.
        var hierarchyAncestors = context.AncestorIds
            .Where(e => matchingItemIds.Contains(e.ItemId))
            .Select(e => e.ParentItemId);

        var linkParents = ResolveLinkParents(context, matchingItemIds, hierarchyAncestors);

        // The link parents are resolved ids, so they are read back as a sub-select to keep the result
        // composable. An id without a BaseItem row could never match a caller's row anyway.
        var linkedParents = context.BaseItems
            .WhereOneOrMany(linkParents, e => e.Id)
            .Select(e => e.Id);

        var linkedParentAncestors = context.AncestorIds
            .WhereOneOrMany(linkParents, e => e.ItemId)
            .Select(e => e.ParentItemId);

        var seamAncestors = context.AncestorIds
            .Where(e => hierarchyAncestors.Contains(e.ItemId) || linkedParentAncestors.Contains(e.ItemId))
            .Select(e => e.ParentItemId);

        return hierarchyAncestors
            .Concat(linkedParents)
            .Concat(linkedParentAncestors)
            .Concat(seamAncestors)
            .Distinct();
    }

    private static IQueryable<Guid> GetMatchingMediaStreamItemIds(JellyfinDbContext context, HasMediaStreamType criteria)
    {
        var query = context.MediaStreamInfos
            .Where(ms => ms.StreamType == criteria.StreamType
                   && (criteria.Language.Contains(ms.Language)
                       || (criteria.Language.Contains("und") && string.IsNullOrEmpty(ms.Language)))); // und = undetermined

        if (criteria.IsExternal.HasValue)
        {
            var isExternal = criteria.IsExternal.Value;
            query = query.Where(ms => ms.IsExternal == isExternal);
        }

        return query.Select(ms => ms.ItemId);
    }

    private static IQueryable<Guid> ClosureDescendants(JellyfinDbContext context, IReadOnlyList<Guid> roots)
    {
        var direct = context.AncestorIds
            .WhereOneOrMany(roots, e => e.ParentItemId)
            .Select(e => e.ItemId);

        // An item carries its own chain plus its collection folders, never the UserRootFolder.
        var indirect = context.AncestorIds
            .Where(e => direct.Contains(e.ParentItemId))
            .Select(e => e.ItemId);

        return direct.Concat(indirect);
    }

    /// <summary>
    /// Resolves every folder that reaches one of the matching items through a linked edge.
    /// </summary>
    /// <returns>The ids of the folders whose linked children lead, at any depth, to a matching item.</returns>
    private static List<Guid> ResolveLinkParents(JellyfinDbContext context, IQueryable<Guid> matchingItemIds, IQueryable<Guid> ancestorsOfMatches)
    {
        // A link sits above the closure as well as above another link: a BoxSet holds a Series whose
        // episode matches, and another BoxSet holds that BoxSet. So the hop repeats until it stops
        // finding anything new, and each hop takes the links landing on the set itself or on a folder
        // that contains it. Only folders owning linked children are ever collected, which bounds this
        // by the number of BoxSets and Playlists rather than by the item count.
        var resolved = context.LinkedChildren
            .Where(e => matchingItemIds.Contains(e.ChildId) || ancestorsOfMatches.Contains(e.ChildId))
            .Select(e => e.ParentId)
            .Distinct()
            .ToHashSet();

        var frontier = resolved.ToList();

        while (frontier.Count != 0)
        {
            var containingFolders = context.AncestorIds
                .WhereOneOrMany(frontier, e => e.ItemId)
                .Select(e => e.ParentItemId);

            var directLinkParents = context.LinkedChildren
                .WhereOneOrMany(frontier, e => e.ChildId)
                .Select(e => e.ParentId);

            var indirectLinkParents = context.LinkedChildren
                .Where(e => containingFolders.Contains(e.ChildId))
                .Select(e => e.ParentId);

            var next = directLinkParents
                .Concat(indirectLinkParents)
                .Distinct()
                .ToArray();

            frontier = [];
            foreach (var id in next)
            {
                // Cyclic links (a BoxSet holding itself, directly or not) terminate on the resolved set.
                if (resolved.Add(id))
                {
                    frontier.Add(id);
                }
            }
        }

        return [.. resolved];
    }

    /// <summary>
    /// Resolves the roots the descendant sub-selects have to be anchored on.
    /// </summary>
    /// <returns>
    /// The roots whose AncestorIds closure belongs to the result, and the roots whose LinkedChildren
    /// belong to the result.
    /// </returns>
    private static (List<Guid> ClosureRoots, List<Guid> LinkRoots) ResolveLinkedRoots(JellyfinDbContext context, Guid parentId)
    {
        // A folder found through the closure needs no closure hop of its own.
        var closureRoots = new List<Guid> { parentId };
        var linkRoots = new List<Guid> { parentId };
        var visited = new HashSet<Guid> { parentId };
        var frontier = new List<Guid> { parentId };

        while (frontier.Count != 0)
        {
            var closureIds = ClosureDescendants(context, frontier);

            var linkedIds = context.LinkedChildren
                .WhereOneOrMany(frontier, e => e.ParentId)
                .Select(e => e.ChildId);

            // Folders that own linked children, i.e. the only items whose links are worth following.
            var linkOwners = context.BaseItems
                .Where(e => e.IsFolder
                    && (closureIds.Contains(e.Id) || linkedIds.Contains(e.Id))
                    && context.LinkedChildren.Any(l => l.ParentId.Equals(e.Id)))
                .Select(e => e.Id)
                .ToArray();

            var linkedFolders = context.BaseItems
                .Where(e => e.IsFolder && linkedIds.Contains(e.Id))
                .Select(e => e.Id)
                .ToHashSet();

            frontier = [];
            foreach (var id in linkOwners.Concat(linkedFolders))
            {
                if (!visited.Add(id))
                {
                    continue;
                }

                frontier.Add(id);
                linkRoots.Add(id);

                // Only a folder reached through a link contributes a closure that is not covered by
                // the roots already collected.
                if (linkedFolders.Contains(id))
                {
                    closureRoots.Add(id);
                }
            }
        }

        return (closureRoots, linkRoots);
    }
}
