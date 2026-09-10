using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Configuration;
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
        /// <param name="preferredImageLanguages">Array of preferred image languages. If null or empty, uses requestedMetadataLanguage.</param>
        /// <param name="originalLanguage">The original language of the media, used for OriginalLanguage option type.</param>
        /// <returns>The ordered remote image infos.</returns>
        public static IEnumerable<RemoteImageInfo> OrderByLanguageDescending(
            this IEnumerable<RemoteImageInfo> remoteImageInfos,
            string requestedMetadataLanguage,
            ImageLanguageOption[]? preferredImageLanguages = null,
            string? originalLanguage = null)
        {
            if (string.IsNullOrWhiteSpace(requestedMetadataLanguage))
            {
                // Default to English if no requested language is specified.
                requestedMetadataLanguage = "en";
            }

            if (preferredImageLanguages is null or { Length: 0 })
            {
                var languageOptions = new List<ImageLanguageOption>
                {
                    new() { OptionType = ImageLanguageType.LanguageCode, Language = requestedMetadataLanguage }
                };

                if (!string.Equals(requestedMetadataLanguage, "en", StringComparison.OrdinalIgnoreCase))
                {
                    languageOptions.Add(new() { OptionType = ImageLanguageType.LanguageCode, Language = "en" });
                }

                preferredImageLanguages = languageOptions.ToArray();
            }

            return remoteImageInfos
                .OrderByDescending(i => GetLanguagePriority(i.Language, preferredImageLanguages, originalLanguage))
                .ThenByDescending(i => Math.Round(i.CommunityRating ?? 0, 1))
                .ThenByDescending(i => i.VoteCount ?? 0);
        }

        private static int GetLanguagePriority(string? imageLanguage, ImageLanguageOption[] preferredImageLanguages, string? originalLanguage)
        {
            // Image priority ordering:
            //  - Images that match preferred image languages (in order of preference)
            //  - Images with no language if option type NoLanguage is not in preferred image languages
            //  - Images that don't match the requested language

            for (int index = 0; index < preferredImageLanguages.Length; index++)
            {
                var option = preferredImageLanguages[index];

                bool isMatch = option.OptionType switch
                {
                    ImageLanguageType.LanguageCode => !string.IsNullOrEmpty(option.Language) && string.Equals(option.Language, imageLanguage, StringComparison.OrdinalIgnoreCase),
                    ImageLanguageType.NoLanguage => string.IsNullOrEmpty(imageLanguage),
                    ImageLanguageType.OriginalLanguage => !string.IsNullOrEmpty(originalLanguage) && string.Equals(originalLanguage, imageLanguage, StringComparison.OrdinalIgnoreCase),
                    _ => false
                };

                if (isMatch)
                {
                    // Return a high priority value, with earlier languages getting higher values
                    return 1 + (preferredImageLanguages.Length - index);
                }
            }

            return string.IsNullOrEmpty(imageLanguage) ? 1 : 0;
        }
    }
}
