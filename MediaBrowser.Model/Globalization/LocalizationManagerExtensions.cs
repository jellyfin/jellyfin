using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Model.Globalization
{
    /// <summary>
    /// Extension methods for <see cref="ILocalizationManager"/>.
    /// </summary>
    public static class LocalizationManagerExtensions
    {
        /// <summary>
        /// Gets every code a media stream in the given language can be tagged with, the most specific first.
        /// </summary>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="language">The language, as stored in a setting or a preference.</param>
        /// <returns>The language codes.</returns>
        public static IReadOnlyList<string> GetMediaStreamLanguageCodes(this ILocalizationManager localizationManager, string language)
        {
            var culture = localizationManager.FindLanguageInfo(language);
            if (culture is null)
            {
                return [language];
            }

            List<string> codes = [];
            var tag = culture.TwoLetterISOLanguageName;
            var separator = tag.IndexOf('-', StringComparison.Ordinal);
            if (separator > 0)
            {
                codes.Add(tag);
            }

            codes.AddRange(culture.ThreeLetterISOLanguageNames);

            // ffprobe tags a regional track with the code of its base language, a fr-CA track as "fre".
            if (separator > 0 && localizationManager.FindLanguageInfo(tag[..separator]) is { } baseCulture)
            {
                codes.AddRange(baseCulture.ThreeLetterISOLanguageNames);
            }

            // A stream scanned with a legacy code (pob) keeps it until the item is refreshed.
            codes.Add(language);

            return codes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
