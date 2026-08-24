using System;

namespace MediaBrowser.Providers.Manager;

/// <summary>
/// Helpers for comparing the language of fetched metadata with the language that was requested.
/// </summary>
internal static class MetadataLanguageUtils
{
    /// <summary>
    /// Gets the language subtag of a language tag, e.g. "es" for "es-ES".
    /// </summary>
    /// <param name="language">The language tag.</param>
    /// <returns>The language subtag, lowercased, or <c>null</c> if none was given.</returns>
    public static string? GetLanguageSubtag(string? language)
    {
        if (string.IsNullOrEmpty(language))
        {
            return null;
        }

        var separator = language.IndexOf('-', StringComparison.Ordinal);

        return (separator == -1 ? language : language[..separator]).ToLowerInvariant();
    }

    /// <summary>
    /// Determines whether a provider result can be considered to be in the requested language.
    /// </summary>
    /// <param name="resultLanguage">The language the provider reported for its result, if any.</param>
    /// <param name="preferredLanguage">The language that was requested, if any.</param>
    /// <returns><c>true</c> if the result is in the requested language or either language is unknown.</returns>
    public static bool MatchesPreferredLanguage(string? resultLanguage, string? preferredLanguage)
    {
        // A provider that doesn't report a language cannot be judged, assume it honored the request
        if (string.IsNullOrEmpty(resultLanguage) || string.IsNullOrEmpty(preferredLanguage))
        {
            return true;
        }

        // Compare on the language subtag only so that e.g. "es" matches "es-ES"
        return string.Equals(GetLanguageSubtag(resultLanguage), GetLanguageSubtag(preferredLanguage), StringComparison.Ordinal);
    }
}
