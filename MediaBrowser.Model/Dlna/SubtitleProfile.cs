using System;
using System.Linq;
using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="SubtitleProfile" />.
    /// </summary>
    public class SubtitleProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleProfile"/> class.
        /// </summary>
        public SubtitleProfile()
        {
            Format = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleProfile"/> class.
        /// </summary>
        /// <param name="format">The format<see cref="string"/>.</param>
        /// <param name="method">The method<see cref="SubtitleDeliveryMethod"/>.</param>
        public SubtitleProfile(string format, SubtitleDeliveryMethod method)
        {
            Format = format;
            Method = method;
        }

        /// <summary>
        /// Gets or sets the Format.
        /// </summary>
        [XmlAttribute("format")]
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets the Method.
        /// </summary>
        [XmlAttribute("method")]
        public SubtitleDeliveryMethod Method { get; set; }

        /// <summary>
        /// Gets or sets the Didl mode..
        /// </summary>
        [XmlAttribute("didlMode")]
        public string? DidlMode { get; set; }

        /// <summary>
        /// Gets or sets the Language.
        /// </summary>
        [XmlAttribute("language")]
        public string? Language { get; set; }

        /// <summary>
        /// Gets or sets the Container.
        /// </summary>
        [XmlAttribute("container")]
        public string? Container { get; set; }

        /// <summary>
        /// Returns the languages.
        /// </summary>
        /// <returns>Array of languages.</returns>
        public string[] GetLanguages()
        {
            return ContainerProfile.SplitValue(Language ?? string.Empty);
        }

        /// <summary>
        /// Checks to see if the language <paramref name="subLanguage"/> is supported.
        /// </summary>
        /// <param name="subLanguage">The subtitle language to check.</param>
        /// <returns>True if supported.</returns>
        public bool SupportsLanguage(string subLanguage)
        {
            if (string.IsNullOrEmpty(Language))
            {
                return true;
            }

            if (string.IsNullOrEmpty(subLanguage))
            {
                subLanguage = "und";
            }

            var languages = GetLanguages();
            return languages.Length == 0 || languages.Contains(subLanguage, StringComparer.OrdinalIgnoreCase);
        }
    }
}
