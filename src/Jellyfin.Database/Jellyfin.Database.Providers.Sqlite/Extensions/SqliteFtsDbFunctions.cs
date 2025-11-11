namespace Jellyfin.Database.Providers.Sqlite.Extensions;

/// <summary>
/// Custom EF Core database functions for SQLite FTS5 operations.
/// These methods are mapped to SQL functions and should never be called directly in C# code.
/// </summary>
/// <remarks>
/// Currently empty as FTS implementation uses direct JOIN approach with BaseItemFtsEntity.
/// This class is kept for potential future custom FTS functions.
/// </remarks>
public static class SqliteFtsDbFunctions
{
}
