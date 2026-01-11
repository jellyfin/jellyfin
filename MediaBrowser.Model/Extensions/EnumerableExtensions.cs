using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Model.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IEnumerable{T}"/>.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Orders <see cref="RemoteImageInfo"/> by requested language in descending order, prioritizing "en" over other non-matches.
        /// </summary>
        /// <param name="remoteImageInfos">The remote image infos.</param>
        /// <param name="requestedLanguage">The requested language for the images.</param>
        /// <returns>The ordered remote image infos.</returns>
        private static IEnumerable<RemoteImageInfo> OrderByLanguageDescending(this IEnumerable<RemoteImageInfo> remoteImageInfos, string requestedLanguage)
        {
            if (string.IsNullOrWhiteSpace(requestedLanguage))
            {
                // Default to English if no requested language is specified.
                requestedLanguage = "en";
            }

            return remoteImageInfos.OrderByDescending(i =>
                {
                    // Image priority ordering:
                    //  - Images that match the requested language
                    //  - Images with no language
                    //  - TODO: Images that match the original language
                    //  - Images in English
                    //  - Images that don't match the requested language

                    if (string.Equals(requestedLanguage, i.Language, StringComparison.OrdinalIgnoreCase))
                    {
                        return 3;
                    }

                    if (string.IsNullOrEmpty(i.Language))
                    {
                        return 2;
                    }

                    if (string.Equals(i.Language, "en", StringComparison.OrdinalIgnoreCase))
                    {
                        return 1;
                    }

                    return 0;
                })
                .ThenByDescending(i => Math.Round(i.CommunityRating ?? 0, 1))
                .ThenByDescending(i => i.VoteCount ?? 0);
        }

        /// <summary>
        /// Orders <see cref="RemoteImageInfo"/> by preferred languages in descending order.
        /// </summary>
        /// <param name="remoteImageInfos">The remote image infos.</param>
        /// <param name="requestedLanguage">The requested language for fallback if no preferred languages are specified.</param>
        /// <param name="preferredLanguages">Array of preferred languages (e.g., ["en", "de", "fr", "nolang"]). If null or empty, uses requestedLanguage.</param>
        /// <returns>The ordered remote image infos.</returns>
        public static IEnumerable<RemoteImageInfo> OrderByLanguageDescending(
            this IEnumerable<RemoteImageInfo> remoteImageInfos,
            string requestedLanguage,
            string[]? preferredLanguages)
        {
            // If no preferred languages are configured, fall back to the original behavior
            if (preferredLanguages is null || preferredLanguages.Length == 0)
            {
                return remoteImageInfos.OrderByLanguageDescending(requestedLanguage);
            }

            return remoteImageInfos.OrderByDescending(i =>
                {
                    // Image priority ordering:
                    //  - Images that match preferred image languages (in order of preference)
                    //  - Images with no language if nolang is not in preferred image languages
                    //  - Images that don't match the requested language

                    for (int index = 0; index < preferredLanguages.Length; index++)
                    {
                        if (string.Equals(preferredLanguages[index], i.Language, StringComparison.OrdinalIgnoreCase) ||
                            (string.Equals(preferredLanguages[index], "nolang", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(i.Language)))
                        {
                            // Return a high priority value, with earlier languages getting higher values
                            return 1 + (preferredLanguages.Length - index);
                        }
                    }

                    if (string.IsNullOrEmpty(i.Language))
                    {
                        return 1;
                    }

                    return 0;
                })
                .ThenByDescending(i => Math.Round(i.CommunityRating ?? 0, 1))
                .ThenByDescending(i => i.VoteCount ?? 0);
        }
    }
}
