using System;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Database.Providers.Sqlite.Extensions;

/// <summary>
/// Custom EF Core database functions for SQLite FTS5 operations.
/// These methods are mapped to SQL functions and should never be called directly in C# code.
/// </summary>
public static class SqliteFtsDbFunctions
{
    /// <summary>
    /// Performs a full-text search match against the FTS5 index.
    /// This function is translated to SQL and executes on the database.
    /// </summary>
    /// <param name="ftsTableName">The name of the FTS5 table to search.</param>
    /// <param name="searchQuery">The FTS5 search query string.</param>
    /// <returns>True if the search matches, false otherwise.</returns>
    /// <exception cref="NotSupportedException">Always thrown if called directly in C# code.</exception>
    /// <remarks>
    /// This method should only be used within LINQ queries where it will be translated to SQL.
    /// The SQL translation performs a subquery lookup in the FTS5 index.
    ///
    /// Example usage:
    /// <code>
    /// var results = context.BaseItems
    ///     .Where(item => SqliteFtsDbFunctions.FtsMatch("BaseItems_fts", "avatar"))
    ///     .ToList();
    /// </code>
    ///
    /// This translates to SQL:
    /// <code>
    /// SELECT * FROM BaseItems
    /// WHERE rowid IN (SELECT rowid FROM BaseItems_fts WHERE BaseItems_fts MATCH 'avatar')
    /// </code>
    /// </remarks>
    public static bool FtsMatch(string ftsTableName, string searchQuery)
    {
        throw new NotSupportedException(
            "This method is a database function placeholder and should only be used in LINQ queries. " +
            "It will be translated to SQL by Entity Framework Core.");
    }
}
