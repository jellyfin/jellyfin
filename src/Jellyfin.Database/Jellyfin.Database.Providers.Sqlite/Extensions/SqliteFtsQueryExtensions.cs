using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Database.Providers.Sqlite.Extensions;

/// <summary>
/// Extension methods for FTS5 full-text search.
/// </summary>
public static class SqliteFtsQueryExtensions
{
    /// <summary>
    /// Performs full-text search using FTS5 for optimized searching.
    /// </summary>
    /// <param name="query">The base queryable.</param>
    /// <param name="context">The database context.</param>
    /// <param name="searchTerm">The search term.</param>
    /// <returns>An IQueryable of BaseItemEntity matching the search term.</returns>
    public static IQueryable<BaseItemEntity> SearchByTextCaseInsensitive(
        this IQueryable<BaseItemEntity> query,
        JellyfinDbContext context,
        string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return query;
        }

        var ftsQuery = Fts5QueryBuilder.Escape(searchTerm.Trim());

        if (string.IsNullOrWhiteSpace(ftsQuery))
        {
            return query;
        }

        HashSet<Guid> matchingIds;
        try
        {
            matchingIds = context.BaseItemsFts
                .Where(fts => fts.Match == ftsQuery)
                .Select(fts => fts.Id)
                .ToHashSet();
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
            return query.Where(item => false);
        }

        if (matchingIds.Count == 0)
        {
            return query.Where(item => false);
        }

        return query.Where(item => matchingIds.Contains(item.Id));
    }
}
