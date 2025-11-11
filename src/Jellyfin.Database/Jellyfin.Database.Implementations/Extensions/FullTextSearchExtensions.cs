using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Implementations.Extensions;

/// <summary>
/// Extension methods for full-text search (database-agnostic).
/// </summary>
public static class FullTextSearchExtensions
{
    /// <summary>
    /// Performs full-text search using the database provider's FTS implementation if available.
    /// Falls back to LIKE-based search if FTS is not supported.
    /// This method composes expressions for optimal SQL generation without materialization.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to search.</typeparam>
    /// <param name="query">The base queryable.</param>
    /// <param name="context">The database context.</param>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="limit">The limit.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <param name="searchableColumns">Column selectors to search in (e.g., e => e.Name, e => e.Tags).</param>
    /// <returns>An IQueryable filtered by the search term.</returns>
    public static IQueryable<TEntity> SearchFullText<TEntity>(
        this IQueryable<TEntity> query,
        JellyfinDbContext context,
        string searchTerm,
        int? limit,
        ILogger? logger,
        params Expression<Func<TEntity, string?>>[] searchableColumns)
        where TEntity : class
    {
        return SearchFullText(query, context, searchTerm, limit, null, logger, searchableColumns);
    }

    /// <summary>
    /// Performs full-text search using the database provider's FTS implementation if available.
    /// Falls back to LIKE-based search if FTS is not supported.
    /// This method composes expressions for optimal SQL generation without materialization.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to search.</typeparam>
    /// <param name="query">The base queryable.</param>
    /// <param name="context">The database context.</param>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="limit">The limit.</param>
    /// <param name="ftsOptions">Optional FTS-specific options for filtering.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <param name="searchableColumns">Column selectors to search in (e.g., e => e.Name, e => e.Tags).</param>
    /// <returns>An IQueryable filtered by the search term.</returns>
    public static IQueryable<TEntity> SearchFullText<TEntity>(
        this IQueryable<TEntity> query,
        JellyfinDbContext context,
        string searchTerm,
        int? limit,
        FtsSearchOptions? ftsOptions,
        ILogger? logger,
        params Expression<Func<TEntity, string?>>[] searchableColumns)
        where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return query;
        }

        var ftsProvider = context.DatabaseProvider.FullTextSearchProvider;

        if (ftsProvider != null)
        {
            var options = ftsOptions ?? new FtsSearchOptions();
            options.SearchableColumns = ExtractColumnNames(searchableColumns);
            options.UseStemming = true;
            options.UsePrefixMatching = true;
            options.Limit = limit ?? -1;

            var ftsQuery = ftsProvider.ApplyFullTextSearch(query, context, searchTerm, options);
            if (ftsQuery != null)
            {
                return ftsQuery;
            }

            logger?.LogDebug("FTS provider returned null for search term '{SearchTerm}', falling back to LIKE", searchTerm);
        }
        else
        {
            logger?.LogDebug("No FTS provider available for database, using LIKE fallback for search term '{SearchTerm}'", searchTerm);
        }

        // Fallback: LIKE-based search on all specified columns
        return query.Where(BuildLikeFallbackExpression(searchTerm, searchableColumns));
    }

    private static string[] ExtractColumnNames<TEntity>(Expression<Func<TEntity, string?>>[] columnSelectors)
    {
        var names = new string[columnSelectors.Length];
        for (int i = 0; i < columnSelectors.Length; i++)
        {
            if (columnSelectors[i].Body is MemberExpression memberExpr)
            {
                names[i] = memberExpr.Member.Name;
            }
            else
            {
                throw new ArgumentException($"Column selector at index {i} is not a simple property access.");
            }
        }

        return names;
    }

    private static Expression<Func<TEntity, bool>> BuildLikeFallbackExpression<TEntity>(
        string searchTerm,
        Expression<Func<TEntity, string?>>[] columnSelectors)
    {
        var normalizedSearch = searchTerm.Trim();
        var parameter = Expression.Parameter(typeof(TEntity), "e");

        Expression? combinedExpression = null;

        foreach (var columnSelector in columnSelectors)
        {
            var columnExpression = new ParameterReplacer(columnSelector.Parameters[0], parameter)
                .Visit(columnSelector.Body);

            var notNullCheck = Expression.NotEqual(columnExpression, Expression.Constant(null, typeof(string)));

            var containsMethod = typeof(string).GetMethod(
                nameof(string.Contains),
                [typeof(string), typeof(StringComparison)])!;

            var containsCall = Expression.Call(
                columnExpression,
                containsMethod,
                Expression.Constant(normalizedSearch),
                Expression.Constant(StringComparison.OrdinalIgnoreCase));

            var columnCondition = Expression.AndAlso(notNullCheck, containsCall);

            combinedExpression = combinedExpression == null
                ? columnCondition
                : Expression.OrElse(combinedExpression, columnCondition);
        }

        return Expression.Lambda<Func<TEntity, bool>>(
            combinedExpression ?? Expression.Constant(false),
            parameter);
    }

    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}
