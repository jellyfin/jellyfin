using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Providers.Sqlite.Entities;
using Jellyfin.Database.Providers.Sqlite.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Database.Providers.Sqlite;

/// <summary>
/// SQLite FTS5 full-text search implementation.
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
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return null;
        }

        // Only support BaseItemEntity for now (can be extended later)
        if (typeof(TEntity) != typeof(BaseItemEntity))
        {
            return null;
        }

        var ftsMatchQuery = Fts5QueryBuilder.Escape(searchTerm.Trim());

        if (string.IsNullOrWhiteSpace(ftsMatchQuery))
        {
            return null;
        }

        var ftsIds = context.Set<BaseItemFtsEntity>()
            .AsNoTracking()
            .Where(fts => fts.Match == ftsMatchQuery)
            .OrderBy(fts => fts.Rank)
            .Select(fts => fts.Id);

        var result = query.Where(item => ftsIds.Contains(((BaseItemEntity)(object)item).Id));

        return result;
    }
}
