using System;
using System.Text;

namespace Jellyfin.Database.Implementations;

/// <summary>
/// Builds <c>LIKE</c> patterns that do not depend on an engine's case rules or default escape character, and that
/// match the same rows on any engine whose <c>lower()</c> folds only ASCII letters.
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
    /// as SQLite's <c>lower()</c> does. Backslashes are matched literally, while <c>%</c> and <c>_</c> keep their
    /// wildcard meaning. NUL characters are dropped, as they are not stored and some engines reject them in a
    /// parameter.
    /// </summary>
    /// <param name="term">The text to look for.</param>
    /// <returns>The pattern, ASCII-lower-cased, with <c>\</c> escaped.</returns>
    public static string ContainsIgnoreAsciiCase(string term)
    {
        ArgumentNullException.ThrowIfNull(term);

        var pattern = new StringBuilder(term.Length + 2).Append('%');
        foreach (var c in term)
        {
            if (c == '\0')
            {
                continue;
            }

            if (c == '\\')
            {
                pattern.Append('\\');
            }

            pattern.Append(char.IsAsciiLetterUpper(c) ? (char)(c + ('a' - 'A')) : c);
        }

        return pattern.Append('%').ToString();
    }
}
