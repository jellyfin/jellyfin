using System;
using System.Linq;
using Jellyfin.Database.Implementations.MatchCriteria;

namespace Jellyfin.Database.Implementations;

/// <summary>
/// Provider interface for descendant queries using recursive CTEs.
/// Each database provider implements this with provider-specific SQL.
/// </summary>
public interface IDescendantQueryProvider
{
    /// <summary>
    /// Gets a queryable of all descendant IDs for a parent item.
    /// Uses recursive CTE to traverse AncestorIds and LinkedChildren infinitely.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="parentId">Parent item ID.</param>
    /// <returns>Queryable of descendant item IDs.</returns>
    IQueryable<Guid> GetAllDescendantIds(JellyfinDbContext context, Guid parentId);

    /// <summary>
    /// Gets a queryable of all folder IDs that have any descendant matching the specified criteria.
    /// Uses recursive CTE for infinite depth traversal. Can be used in LINQ .Contains() expressions.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="criteria">The matching criteria to apply.</param>
    /// <returns>Queryable of folder IDs.</returns>
    IQueryable<Guid> GetFolderIdsMatching(JellyfinDbContext context, FolderMatchCriteria criteria);
}
