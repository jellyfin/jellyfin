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
    private readonly IDbContextFactory<SqliteJellyfinDbContext>? _sqliteContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteFtsProvider"/> class.
    /// </summary>
    /// <param name="sqliteContextFactory">The SQLite-specific context factory.</param>
    public SqliteFtsProvider(IDbContextFactory<SqliteJellyfinDbContext>? sqliteContextFactory = null)
    {
        _sqliteContextFactory = sqliteContextFactory;
    }

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

        // Try to use the passed context if it's a SqliteJellyfinDbContext
        // Otherwise fall back to creating a new context from the factory
        IQueryable<BaseItemFtsEntity> ftsQuery;
        SqliteJellyfinDbContext? ownedContext = null;

        if (context is SqliteJellyfinDbContext sqliteContext)
        {
            ftsQuery = sqliteContext.BaseItemFts;
        }
        else if (_sqliteContextFactory != null)
        {
            ownedContext = _sqliteContextFactory.CreateDbContext();
            ftsQuery = ownedContext.BaseItemFts;
        }
        else
        {
            return null;
        }

        try
        {
            var ftsIds = ftsQuery
                .AsNoTracking()
                .Where(fts => fts.Match == ftsMatchQuery)
                .OrderBy(fts => fts.Rank)
                .Select(fts => fts.Id)
                .ToList(); // Materialize the FTS query results

            return query.Where(item => ftsIds.Contains(((BaseItemEntity)(object)item).Id));
        }
        finally
        {
            ownedContext?.Dispose();
        }
    }
}
