using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Database.Providers.Sqlite.Extensions;

/// <summary>
/// Helper class for FTS5 query sanitization and escaping.
/// </summary>
public static class Fts5QueryBuilder
{
    /// <summary>
    /// Escapes and sanitizes a search term for FTS5 queries.
    /// Uses prefix matching and removes special characters that cause syntax errors.
    /// </summary>
    /// <param name="searchTerm">The raw search term.</param>
    /// <returns>A properly escaped FTS5 query string.</returns>
    public static string Escape(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return string.Empty;
        }

        var words = searchTerm.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (words.Length == 0)
        {
            return string.Empty;
        }

        var escapedWords = new List<string>();
        foreach (var word in words)
        {
            var cleanWord = new string(word.Where(c =>
                char.IsLetterOrDigit(c) ||
                c == '-' ||
                c == '_' ||
                c == '\'' ||
                c == '.').ToArray());

            if (string.IsNullOrWhiteSpace(cleanWord))
            {
                continue;
            }

            var escapedWord = cleanWord.Replace("\"", "\"\"", StringComparison.Ordinal);
            escapedWords.Add($"{escapedWord}*");
        }

        if (escapedWords.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(" ", escapedWords);
    }
}
