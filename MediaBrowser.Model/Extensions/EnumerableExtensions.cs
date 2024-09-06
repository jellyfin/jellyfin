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
                        return 4;
                    }

                    if (string.IsNullOrEmpty(i.Language))
                    {
                        return 3;
                    }

                    if (string.Equals(i.Language, "en", StringComparison.OrdinalIgnoreCase))
                    {
                        return 2;
                    }

                    return 0;
                })
                .ThenByDescending(i => i.CommunityRating ?? 0)
                .ThenByDescending(i => i.VoteCount ?? 0);
        }
    }
}
