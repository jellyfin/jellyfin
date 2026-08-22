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

        return AllDescendants(context, [parentId])
            .Where(e => !e.Equals(parentId))
            .Distinct();
    }

    /// <summary>
    /// Gets all descendant IDs for multiple parent items in a single traversal.
    /// Traverses AncestorIds and LinkedChildren, like <see cref="GetAllDescendantIds"/>, but resolves
    /// the roots once for all seeds instead of once per seed.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="parentIds">Parent item IDs.</param>
    /// <returns>Set of all descendant item IDs (excluding the parent IDs themselves).</returns>
    public static HashSet<Guid> GetAllDescendantIdsBatch(JellyfinDbContext context, IReadOnlyList<Guid> parentIds)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(parentIds);

        if (parentIds.Count == 0)
        {
            return [];
        }

        var descendants = AllDescendants(context, parentIds)
            .Distinct()
            .ToHashSet();

        descendants.ExceptWith(parentIds);

        return descendants;
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

        // Both sides of a version group can hold a folder a caller would see as matching: the
        // alternate carries its own AncestorIds rows and may sit in a different library than the
        // primary it is reported against, and the primary is the item that becomes visible.
        var reportedItemIds = MatchingMediaOwnerIds(context, criteria)
            .Concat(GetPrimaryVersionIdsMatching(context, criteria))
            .Distinct();

        // One hop up the closure covers every ancestor level.
        var hierarchyAncestors = context.AncestorIds
            .Where(e => reportedItemIds.Contains(e.ItemId))
            .Select(e => e.ParentItemId);

        var linkParents = ResolveLinkParents(context, reportedItemIds, hierarchyAncestors);

        // Read back as a sub-select so the result stays composable. Off the primary key, which is one
        // row per id: LinkedChildren would yield one row per link and lean on the outer Distinct.
        var linkedParents = context.BaseItems
            .WhereOneOrMany(linkParents, e => e.Id)
            .Select(e => e.Id);

        var linkedParentAncestors = context.AncestorIds
            .WhereOneOrMany(linkParents, e => e.ItemId)
            .Select(e => e.ParentItemId);

        // The chain an item carries stops at its collection folders, so this hop crosses that seam to
        // the UserRootFolder above them. One statement for both sides beats a sub-select per side.
        var seamAncestors = context.AncestorIds
            .Where(e => hierarchyAncestors.Contains(e.ItemId) || linkedParentAncestors.Contains(e.ItemId))
            .Select(e => e.ParentItemId);

        return hierarchyAncestors
            .Concat(linkedParents)
            .Concat(linkedParentAncestors)
            .Concat(seamAncestors)
            .Distinct();
    }

    /// <summary>
    /// Gets a queryable of the IDs of the primary versions whose alternate version's media matches the
    /// criteria.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="criteria">The matching criteria to apply.</param>
    /// <returns>Queryable of primary version item IDs.</returns>
    /// <remarks>
    /// For callers that already test an item's own media with their own indexed predicate: this covers
    /// exactly what such a predicate misses, and the filtered PrimaryVersionId index keeps it to the few
    /// items that have versions at all.
    /// </remarks>
    public static IQueryable<Guid> GetPrimaryVersionIdsMatching(JellyfinDbContext context, FolderMatchCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(criteria);

        // Anchored on the alternates rather than on the matches: "has a primary version" is served by
        // the partial PrimaryVersionId index, which holds only the few items that are second files, so
        // this costs a seek each into the stream index instead of a second pass over every stream row.
        var alternates = context.BaseItems.Where(v => v.PrimaryVersionId.HasValue);

        if (criteria is HasChapterImages)
        {
            return alternates
                .Where(v => context.Chapters.Any(c => c.ItemId.Equals(v.Id) && c.ImagePath != null))
                .Select(v => v.PrimaryVersionId!.Value);
        }

        var matchingStreams = MatchingMediaStreams(context, criteria);

        return alternates
            .Where(v => matchingStreams.Any(ms => ms.ItemId.Equals(v.Id)))
            .Select(v => v.PrimaryVersionId!.Value);
    }

    // The ids of the items whose own media matches. Kept to the stream and chapter tables so their
    // covering indexes answer this outright: projecting the BaseItems navigation instead would add a
    // primary-key lookup per stream row rather than one per matching item, and the leading key of both
    // indexes leaves the ids already grouped, so the Distinct costs no sort.
    private static IQueryable<Guid> MatchingMediaOwnerIds(JellyfinDbContext context, FolderMatchCriteria criteria)
        => criteria is HasChapterImages
            ? context.Chapters
                .Where(c => c.ImagePath != null)
                .Select(c => c.ItemId)
                .Distinct()
            : MatchingMediaStreams(context, criteria)
                .Select(ms => ms.ItemId)
                .Distinct();

    // The stream rows a criteria matches. One definition, so the owner projection and the alternate
    // projection cannot drift apart despite reading it from opposite ends.
    private static IQueryable<MediaStreamInfo> MatchingMediaStreams(JellyfinDbContext context, FolderMatchCriteria criteria)
        => criteria switch
        {
            HasSubtitles => context.MediaStreamInfos
                .Where(ms => ms.StreamType == MediaStreamTypeEntity.Subtitle),
            HasMediaStreamType m => GetMatchingMediaStreams(context, m),
            _ => throw new ArgumentOutOfRangeException(nameof(criteria), $"Unknown criteria type: {criteria.GetType().Name}")
        };

    private static IQueryable<MediaStreamInfo> GetMatchingMediaStreams(JellyfinDbContext context, HasMediaStreamType criteria)
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

        return query;
    }

    private static IQueryable<Guid> AllDescendants(JellyfinDbContext context, IReadOnlyList<Guid> parentIds)
    {
        var (closureRoots, linkRoots) = ResolveLinkedRoots(context, parentIds);

        var linkedDescendants = context.LinkedChildren
            .WhereOneOrMany(linkRoots, e => e.ParentId)
            .Select(e => e.ChildId);

        return ClosureDescendants(context, closureRoots)
            .Concat(linkedDescendants);
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

    // Resolves the folders whose linked children lead, at any depth, to a matching item.
    private static List<Guid> ResolveLinkParents(JellyfinDbContext context, IQueryable<Guid> matchingItemIds, IQueryable<Guid> ancestorsOfMatches)
    {
        // An alternate version is a second file for the item that links it, not a child of it, so that
        // edge is not walked. It is also the one link a non-folder owns, and there is one per remuxed
        // movie: walking it would swell this list from the BoxSet and Playlist count to the item count,
        // and the list is bound into every statement the returned queryable is embedded in.
        var containerLinks = context.LinkedChildren
            .Where(e => e.ChildType != LinkedChildType.LocalAlternateVersion
                && e.ChildType != LinkedChildType.LinkedAlternateVersion);

        // A link sits above the closure and above another link alike, so the hop repeats until nothing
        // new turns up.
        var resolved = containerLinks
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

            var directLinkParents = containerLinks
                .WhereOneOrMany(frontier, e => e.ChildId)
                .Select(e => e.ParentId);

            var indirectLinkParents = containerLinks
                .Where(e => containingFolders.Contains(e.ChildId))
                .Select(e => e.ParentId);

            var next = directLinkParents
                .Concat(indirectLinkParents)
                .Distinct()
                .ToArray();

            frontier = [];
            foreach (var id in next)
            {
                // Cyclic links terminate on the resolved set.
                if (resolved.Add(id))
                {
                    frontier.Add(id);
                }
            }
        }

        return [.. resolved];
    }

    // Resolves the roots the descendant sub-selects are anchored on: those contributing their closure,
    // and those contributing their linked children.
    private static (List<Guid> ClosureRoots, List<Guid> LinkRoots) ResolveLinkedRoots(JellyfinDbContext context, IReadOnlyList<Guid> parentIds)
    {
        var visited = new HashSet<Guid>(parentIds);
        var closureRoots = visited.ToList();
        var linkRoots = visited.ToList();
        var frontier = visited.ToList();

        while (frontier.Count != 0)
        {
            var closureIds = ClosureDescendants(context, frontier);

            var linkedIds = context.LinkedChildren
                .WhereOneOrMany(frontier, e => e.ParentId)
                .Select(e => e.ChildId);

            var linkedFolders = context.BaseItems
                .Where(e => e.IsFolder && linkedIds.Contains(e.Id))
                .Select(e => e.Id)
                .ToHashSet();

            // Folders whose own links have to be followed. Driven off LinkedChildren because owning a
            // link is the rare property, so the folder check only reaches rows that can qualify. That
            // check stays: a non-folder owns links too (a movie and its alternate versions).
            var linkOwners = context.LinkedChildren
                .Where(e => (closureIds.Contains(e.ParentId) || linkedIds.Contains(e.ParentId))
                    && e.Parent!.IsFolder)
                .Select(e => e.ParentId)
                .Distinct()
                .ToArray();

            frontier = [];
            foreach (var id in linkOwners.Concat(linkedFolders))
            {
                if (!visited.Add(id))
                {
                    continue;
                }

                frontier.Add(id);
                linkRoots.Add(id);

                // Only a folder reached through a link adds a closure the roots so far do not cover.
                if (linkedFolders.Contains(id))
                {
                    closureRoots.Add(id);
                }
            }
        }

        return (closureRoots, linkRoots);
    }
}
