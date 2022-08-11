#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Model.Globalization
{
    /// <summary>
    /// Class CultureDto.
    /// </summary>
    public class CultureDto
    {
        public CultureDto(string name, string displayName, string twoLetterISOLanguageName, IReadOnlyList<string> threeLetterISOLanguageNames)
        {
            Name = name;
            DisplayName = displayName;
            TwoLetterISOLanguageName = twoLetterISOLanguageName;
            ThreeLetterISOLanguageNames = threeLetterISOLanguageNames;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        /// <value>The display name.</value>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the name of the two letter ISO language.
        /// </summary>
        /// <value>The name of the two letter ISO language.</value>
        public string TwoLetterISOLanguageName { get; }

        /// <summary>
        /// Gets the name of the three letter ISO language.
        /// </summary>
        /// <value>The name of the three letter ISO language.</value>
        public string? ThreeLetterISOLanguageName
        {
            get
            {
                var vals = ThreeLetterISOLanguageNames;
                if (vals.Count > 0)
                {
                    return vals[0];
                }

                return null;
            }
        }

        public IReadOnlyList<string> ThreeLetterISOLanguageNames { get; }
    }
}
