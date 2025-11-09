using System;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Database.Providers.Sqlite.Extensions;

/// <summary>
/// Extension methods for FTS5 full-text search.
/// </summary>
public static class SqliteFtsExtensions
{
    /// <summary>
    /// Searches BaseItems using FTS5 full-text search.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="searchTerm">The search term.</param>
    /// <returns>An IQueryable of BaseItemEntity matching the search.</returns>
    public static IQueryable<BaseItemEntity> SearchFts(
        this JellyfinDbContext context,
        string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return context.BaseItems;
        }

        var ftsQuery = Fts5QueryBuilder.Escape(searchTerm.Trim());

        var matchingIds = context.BaseItemsFts
            .Where(fts => fts.Match == ftsQuery)
            .Select(fts => fts.Id);

        return context.BaseItems.Where(item => matchingIds.Contains(item.Id));
    }
}
