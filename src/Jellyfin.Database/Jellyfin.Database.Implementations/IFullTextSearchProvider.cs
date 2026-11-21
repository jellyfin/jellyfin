using System.Linq;

namespace Jellyfin.Database.Implementations;

/// <summary>
/// Provides full-text search capabilities for a database provider.
/// </summary>
public interface IFullTextSearchProvider
{
    /// <summary>
    /// Applies full-text search filtering to a queryable.
    /// This method modifies the query to include FTS filtering using database-specific techniques.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to search.</typeparam>
    /// <param name="query">The base queryable to filter.</param>
    /// <param name="context">The database context to use for querying FTS tables.</param>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="options">Search options including searchable columns.</param>
    /// <returns>A filtered queryable, or null if FTS is not applicable for this entity type.</returns>
    IQueryable<TEntity>? ApplyFullTextSearch<TEntity>(
        IQueryable<TEntity> query,
        JellyfinDbContext context,
        string searchTerm,
        FtsSearchOptions options)
        where TEntity : class;
}
