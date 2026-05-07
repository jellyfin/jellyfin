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
        /// Orders <see cref="RemoteImageInfo"/> by preferred languages in descending order.
        /// </summary>
        /// <param name="remoteImageInfos">The remote image infos.</param>
        /// <param name="requestedMetadataLanguage">The requested metadata language for fallback if no preferred languages are specified.</param>
        /// <param name="preferredImageLanguages">Array of preferred image languages (e.g., ["en", "de", "fr", "nolang"]). If null or empty, uses requestedLanguage.</param>
        /// <returns>The ordered remote image infos.</returns>
        public static IEnumerable<RemoteImageInfo> OrderByLanguageDescending(
            this IEnumerable<RemoteImageInfo> remoteImageInfos,
            string requestedMetadataLanguage,
            string[]? preferredImageLanguages)
        {
            if (string.IsNullOrWhiteSpace(requestedMetadataLanguage))
            {
                // Default to English if no requested language is specified.
                requestedMetadataLanguage = "en";
            }

            string[] languages = preferredImageLanguages?.Length > 0
                ? preferredImageLanguages
                : [requestedMetadataLanguage, "en"];

            return remoteImageInfos.OrderByDescending(i =>
                {
                    // Image priority ordering:
                    //  - Images that match preferred image languages (in order of preference)
                    //  - Images with no language if nolang is not in preferred image languages
                    //  - Images that don't match the requested language

                    for (int index = 0; index < languages.Length; index++)
                    {
                        if (string.Equals(languages[index], i.Language, StringComparison.OrdinalIgnoreCase) ||
                            (string.Equals(languages[index], "nolang", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(i.Language)))
                        {
                            // Return a high priority value, with earlier languages getting higher values
                            return 1 + (languages.Length - index);
                        }
                    }

                    return string.IsNullOrEmpty(i.Language) ? 1 : 0;
                })
                .ThenByDescending(i => Math.Round(i.CommunityRating ?? 0, 1))
                .ThenByDescending(i => i.VoteCount ?? 0);
        }
    }
}
