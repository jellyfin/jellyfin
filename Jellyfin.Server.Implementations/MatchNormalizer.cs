using System;
using System.Text;
using System.Text.RegularExpressions;
using Jellyfin.Extensions;

namespace Jellyfin.Server.Implementations;

/// <summary>
/// Computes the values of the columns that hold a normalized copy of a text, so that the text can be matched without
/// the database folding its case.
/// </summary>
/// <remarks>
/// <para>
/// Whatever stores a text must store the copy with it, and whatever looks a text up must normalize the term the same
/// way, or they will not find each other's rows. The same input always gives the same output.
/// </para>
/// <para>
/// NUL characters are removed first, because the columns never hold them and an update that bypasses the save
/// sanitizer would otherwise store one.
/// </para>
/// </remarks>
public static partial class MatchNormalizer
{
    /// <summary>
    /// Gets the value of <c>CleanOriginalTitle</c>: the original title in the form of <c>CleanName</c>.
    /// </summary>
    /// <param name="originalTitle">The original title, or <c>null</c> if the item has none.</param>
    /// <returns>The normalized title, which is <c>null</c> exactly if <paramref name="originalTitle"/> is, and can be empty.</returns>
    public static string? CleanOriginalTitle(string? originalTitle)
    {
        return originalTitle?.RemoveNullCharacters().GetCleanValue();
    }

    /// <summary>
    /// Gets the value of <c>NameLower</c>: the name of a person in lower case.
    /// </summary>
    /// <param name="name">The name of the person.</param>
    /// <returns>The name in lower case, using the invariant culture.</returns>
    public static string NameLower(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return name.RemoveNullCharacters().ToLowerInvariant();
    }

    /// <summary>
    /// Builds a <c>LIKE</c> pattern that matches the values of <c>CleanOriginalTitle</c> whose source contains the
    /// term. The term is normalized as <see cref="CleanOriginalTitle"/> does, except that <c>%</c> and <c>_</c> stay
    /// wildcards. The pattern holds no escape character, because everything else that <c>LIKE</c> treats specially
    /// is turned into a space.
    /// </summary>
    /// <param name="term">The text to look for.</param>
    /// <returns>
    /// The pattern, or <c>null</c> if the term holds no letter, digit, <c>%</c> or <c>_</c>. A pattern for such a term
    /// would match every title, and a term of that kind has nothing to look for.
    /// </returns>
    public static string? ContainsCleanOriginalTitlePattern(string term)
    {
        ArgumentNullException.ThrowIfNull(term);

        var lowered = term.RemoveNullCharacters().RemoveDiacritics().ToLowerInvariant();
        var spaced = NotWordOrWildcardRegex().Replace(lowered, " ");
        var collapsed = WhitespaceRegex().Replace(spaced, " ").Trim();
        if (collapsed.Length == 0)
        {
            return null;
        }

        return new StringBuilder(collapsed.Length + 2).Append('%').Append(collapsed).Append('%').ToString();
    }

    [GeneratedRegex(@"[^\p{L}\p{N}\s%_]")]
    private static partial Regex NotWordOrWildcardRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
