using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Providers.Sqlite.Entities;
using Jellyfin.Database.Providers.Sqlite.Extensions;

namespace Jellyfin.Database.Providers.Sqlite;

/// <summary>
/// SQLite FTS5 full-text search implementation.
/// Uses a hybrid approach: limits FTS results to prevent overwhelming JOIN operations,
/// while still leveraging database-side filtering for optimal performance.
/// </summary>
public class SqliteFtsProvider : IFullTextSearchProvider
{
    /// <inheritdoc />
    public IQueryable<TEntity>? ApplyFullTextSearch<TEntity>(
        IQueryable<TEntity> query,
        JellyfinDbContext context,
        string searchTerm,
        FtsSearchOptions options)
        where TEntity : class
    {
        Console.WriteLine($"[FTS] ApplyFullTextSearch called with searchTerm='{searchTerm}'");

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            Console.WriteLine("[FTS] SearchTerm is null/whitespace, returning null");
            return null;
        }

        // Only support BaseItemEntity for now (can be extended later)
        if (typeof(TEntity) != typeof(BaseItemEntity))
        {
            Console.WriteLine($"[FTS] TEntity is {typeof(TEntity).Name}, not BaseItemEntity, returning null");
            return null;
        }

        var ftsMatchQuery = Fts5QueryBuilder.Escape(searchTerm.Trim());

        if (string.IsNullOrWhiteSpace(ftsMatchQuery))
        {
            Console.WriteLine("[FTS] FTS query is null/whitespace after escape, returning null");
            return null;
        }

        Console.WriteLine($"[FTS] Building FTS filter with query: '{ftsMatchQuery}'");

        // Get FTS results ordered by rank (most relevant first)
        // Use a reasonable limit to prevent overwhelming the query
        // The input query is already filtered, so we take more results to account for additional filtering
        var ftsLimit = options.Limit > 0 ? options.Limit : -1;

        Console.WriteLine($"[FTS] Fetching up to {ftsLimit} FTS results");

        /*var ftsIds = context.Set<BaseItemFtsEntity>()
            .Where(fts => fts.Match == ftsMatchQuery)
            .OrderBy(fts => fts.Rank) // Lower rank = better match
            .Select(fts => fts.Id)
            .ToHashSet(); // Materialize limited FTS results*/

        // Console.WriteLine($"[FTS] Retrieved {ftsIds.Count} FTS IDs, filtering main query");

        // Filter the already-filtered query by FTS results
        var result = query
        .Join(
            context.Set<BaseItemFtsEntity>().Where(fts => fts.Match == ftsMatchQuery),
            item => ((BaseItemEntity)(object)item).Id,
            fts => fts.Id,
            (item, fts) => new { item, fts })
        .OrderBy(x => x.fts.Rank)
        .Select(x => x.item);

        Console.WriteLine("[FTS] Filter applied successfully");
        return result;
    }
}
