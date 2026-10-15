using System;
using System.Text;

namespace Jellyfin.Database.Implementations;

/// <summary>
/// Builds <c>LIKE</c> patterns that do not depend on an engine's case rules, default escape character or character
/// classes, and that match the same rows on any engine whose <c>lower()</c> folds only ASCII letters.
/// <c>[</c> is always escaped: it opens a character class in the <c>LIKE</c> of some engines, such as SQL Server, and
/// on the others an escaped character that has no special meaning matches itself.
/// Match a mixed-case column against these patterns with <see cref="JellyfinDbFunctions.LikeIgnoreAsciiCase"/>, and an
/// already lower-case column with <c>EF.Functions.Like</c>, passing <see cref="Escape"/> as the escape character.
/// </summary>
public static class LikePatterns
{
    /// <summary>
    /// The escape character to pass to <c>EF.Functions.Like</c> along with a pattern from this class.
    /// </summary>
    public const string Escape = "\\";

    /// <summary>
    /// Builds a pattern matching values that contain <paramref name="term"/>. Only ASCII letters are lower-cased,
    /// as SQLite's <c>lower()</c> does. Backslashes and <c>[</c> are matched literally, while <c>%</c> and <c>_</c>
    /// keep their wildcard meaning. NUL characters are dropped, as they are not stored and some engines reject them in
    /// a parameter.
    /// </summary>
    /// <param name="term">The text to look for.</param>
    /// <returns>The pattern, ASCII-lower-cased, with <c>\</c> and <c>[</c> escaped.</returns>
    public static string ContainsIgnoreAsciiCase(string term)
        => Build(term, contains: true);

    /// <summary>
    /// Builds a pattern matching values that start with <paramref name="term"/>. Only ASCII letters are lower-cased,
    /// as SQLite's <c>lower()</c> does. Backslashes, <c>[</c>, <c>%</c> and <c>_</c> are matched literally. NUL
    /// characters are dropped, as they are not stored and some engines reject them in a parameter.
    /// </summary>
    /// <param name="term">The text the value must start with.</param>
    /// <returns>The pattern, ASCII-lower-cased, with <c>\</c>, <c>[</c>, <c>%</c> and <c>_</c> escaped.</returns>
    internal static string StartsWithIgnoreAsciiCase(string term)
        => Build(term, contains: false);

    private static string Build(string term, bool contains)
    {
        ArgumentNullException.ThrowIfNull(term);

        var pattern = new StringBuilder(term.Length + 2);
        if (contains)
        {
            pattern.Append('%');
        }

        foreach (var c in term)
        {
            if (c == '\0')
            {
                continue;
            }

            var isWildcard = c is '%' or '_';
            if (c is '\\' or '[' || (!contains && isWildcard))
            {
                pattern.Append('\\');
            }

            pattern.Append(char.IsAsciiLetterUpper(c) ? (char)(c + ('a' - 'A')) : c);
        }

        return pattern.Append('%').ToString();
    }
}
