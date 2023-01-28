namespace Jellyfin.Data.Dtos;

using System;
using System.Text.RegularExpressions;

/// <summary>
/// A dto representing a sanitized search term.
/// </summary>
public class SearchTermDto : IParsable<SearchTermDto>
{
    // https://www.sqlite.org/fts5.html: 3.1. FTS5 Strings
    private static Regex _fullTextSearchReplace = new Regex(@"[^\sa-zA-Z0-9_\u001a\u0080-\uffff]+");

    /// <summary>
    /// Gets the sanitized search term.
    /// </summary>
    public string? Value { get; init; }

    private static string? SanitizeSearchTerm(string? searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            return searchTerm;
        }

        // Remove reserved characters
        // ToLower() to turn reserved words "AND", "OR", and "NOT" into normal fts5 strings
        return _fullTextSearchReplace.Replace(searchTerm, " ").Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Checks if the sanitized search term holds a value.
    /// </summary>
    /// <returns><b>True</b> if the sanitized search term is null or empty.</returns>
    public bool IsNullOrEmpty()
    {
        return string.IsNullOrEmpty(Value);
    }

#pragma warning disable CS1591

    public static SearchTermDto Parse(string s, IFormatProvider? provider)
    {
        if (!TryParse(s, provider, out var result))
        {
           throw new ArgumentException("Could not parse supplied value.", nameof(s));
        }

        return result;
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out SearchTermDto result)
    {
        result = new SearchTermDto { Value = SanitizeSearchTerm(s) };
        return true;
    }
}
