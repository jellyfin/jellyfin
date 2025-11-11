using System;
using System.Text;

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

        var result = new StringBuilder(searchTerm.Length * 2);
        var firstWord = true;

        foreach (var word in words)
        {
            var cleanWord = CleanWord(word);

            if (string.IsNullOrWhiteSpace(cleanWord))
            {
                continue;
            }

            if (!firstWord)
            {
                result.Append(' ');
            }

            var escapedWord = cleanWord.Replace("\"", "\"\"", StringComparison.Ordinal);
            result.Append(escapedWord).Append('*');
            firstWord = false;
        }

        return result.Length > 0 ? result.ToString() : string.Empty;
    }

    private static string CleanWord(string word)
    {
        var cleaned = new StringBuilder(word.Length);

        foreach (var c in word)
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '\'' || c == '.')
            {
                cleaned.Append(c);
            }
        }

        return cleaned.ToString();
    }
}
