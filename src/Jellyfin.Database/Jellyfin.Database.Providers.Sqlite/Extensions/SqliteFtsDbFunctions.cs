using System;

namespace Jellyfin.Database.Providers.Sqlite.Extensions;

/// <summary>
/// Custom EF Core database functions for SQLite FTS5 operations.
/// These methods are mapped to SQL functions and should never be called directly in C# code.
/// </summary>
public static class SqliteFtsDbFunctions
{
    /// <summary>
    /// Checks if an item ID exists in the FTS5 search results.
    /// This function is translated to SQL: EXISTS (SELECT 1 FROM BaseItems_fts WHERE Id = @itemId AND BaseItems_fts MATCH @searchQuery).
    /// </summary>
    /// <param name="itemId">The ID of the item to check.</param>
    /// <param name="searchQuery">The FTS5 search query string (already escaped).</param>
    /// <returns>True if the item matches the search query.</returns>
    /// <exception cref="NotSupportedException">Always thrown if called directly in C# code.</exception>
    /// <remarks>
    /// This method should only be used within LINQ queries where it will be translated to SQL.
    /// Example usage:
    /// <code>
    /// var results = context.BaseItems
    ///     .Where(item => SqliteFtsDbFunctions.MatchesFts(item.Id, "avatar*"))
    ///     .ToList();
    /// </code>
    ///
    /// This translates to SQL:
    /// <code>
    /// SELECT * FROM BaseItems
    /// WHERE EXISTS (
    ///     SELECT 1 FROM BaseItems_fts
    ///     WHERE BaseItems_fts.Id = BaseItems.Id
    ///     AND BaseItems_fts MATCH 'avatar*'
    /// )
    /// </code>
    /// </remarks>
    public static bool MatchesFts(Guid itemId, string searchQuery)
    {
        throw new NotSupportedException(
            "This method is a database function placeholder and should only be used in LINQ queries. " +
            "It will be translated to SQL by Entity Framework Core.");
    }
}
