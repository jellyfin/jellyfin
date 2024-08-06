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
        public static IEnumerable<RemoteImageInfo> OrderByLanguageDescending(this IEnumerable<RemoteImageInfo> remoteImageInfos, string requestedLanguage)
        {
            if (string.IsNullOrWhiteSpace(requestedLanguage))
            {
                // Default to English if no requested language is specified.
                requestedLanguage = "en";
            }

            var isRequestedLanguageEn = string.Equals(requestedLanguage, "en", StringComparison.OrdinalIgnoreCase);

            return remoteImageInfos.OrderByDescending(i =>
                {
                    if (string.Equals(requestedLanguage, i.Language, StringComparison.OrdinalIgnoreCase))
                    {
                        return 3;
                    }

                    if (string.IsNullOrEmpty(i.Language))
                    {
                        // Assume empty image language is likely to be English.
                        return isRequestedLanguageEn ? 3 : 2;
                    }

                    if (!isRequestedLanguageEn && string.Equals(i.Language, "en", StringComparison.OrdinalIgnoreCase))
                    {
                        // Prioritize English over non-requested languages.
                        return 2;
                    }

                    return 0;
                })
                .ThenByDescending(i => i.CommunityRating ?? 0)
                .ThenByDescending(i => i.VoteCount ?? 0);
        }
    }
}
