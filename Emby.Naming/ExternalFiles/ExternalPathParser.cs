using System;
using System.IO;
using System.Linq;
using Emby.Naming.Common;
using Jellyfin.Extensions;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Globalization;

namespace Emby.Naming.ExternalFiles
{
    /// <summary>
    /// External media file parser class.
    /// </summary>
    public class ExternalPathParser
    {
        private readonly NamingOptions _namingOptions;
        private readonly DlnaProfileType _type;
        private readonly ILocalizationManager _localizationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalPathParser"/> class.
        /// </summary>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="namingOptions">The <see cref="NamingOptions"/> object containing FileExtensions, MediaDefaultFlags, MediaForcedFlags and MediaFlagDelimiters.</param>
        /// <param name="type">The <see cref="DlnaProfileType"/> of the parsed file.</param>
        public ExternalPathParser(NamingOptions namingOptions, ILocalizationManager localizationManager, DlnaProfileType type)
        {
            _localizationManager = localizationManager;
            _namingOptions = namingOptions;
            _type = type;
        }

        /// <summary>
        /// Parse filename and extract information.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <param name="extraString">Part of the filename only containing the extra information.</param>
        /// <returns>Returns null or an <see cref="ExternalPathParserResult"/> object if parsing is successful.</returns>
        public ExternalPathParserResult? ParseFile(string path, string? extraString)
        {
            if (path.Length == 0)
            {
                return null;
            }

            var extension = Path.GetExtension(path.AsSpan());
            if (!(_type == DlnaProfileType.Subtitle && _namingOptions.SubtitleFileExtensions.Contains(extension, StringComparison.OrdinalIgnoreCase))
                && !(_type == DlnaProfileType.Audio && _namingOptions.AudioFileExtensions.Contains(extension, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            var pathInfo = new ExternalPathParserResult(path);

            if (string.IsNullOrEmpty(extraString))
            {
                return pathInfo;
            }

            foreach (var separator in _namingOptions.MediaFlagDelimiters)
            {
                var languageString = extraString;
                var titleString = string.Empty;
                const int SeparatorLength = 1;

                while (languageString.Length > 0)
                {
                    int lastSeparator = languageString.LastIndexOf(separator);

                    if (lastSeparator == -1)
                    {
                          break;
                    }

                    string currentSlice = languageString[lastSeparator..];
                    string currentSliceWithoutSeparator = currentSlice[SeparatorLength..];

                    if (_namingOptions.MediaDefaultFlags.Any(s => currentSliceWithoutSeparator.Contains(s, StringComparison.OrdinalIgnoreCase)))
                    {
                        pathInfo.IsDefault = true;
                        extraString = extraString.Replace(currentSlice, string.Empty, StringComparison.OrdinalIgnoreCase);
                        languageString = languageString[..lastSeparator];
                        continue;
                    }

                    if (_namingOptions.MediaForcedFlags.Any(s => currentSliceWithoutSeparator.Contains(s, StringComparison.OrdinalIgnoreCase)))
                    {
                        pathInfo.IsForced = true;
                        extraString = extraString.Replace(currentSlice, string.Empty, StringComparison.OrdinalIgnoreCase);
                        languageString = languageString[..lastSeparator];
                        continue;
                    }

                    // Try to translate to three character code
                    var culture = _localizationManager.FindLanguageInfo(currentSliceWithoutSeparator);

                    if (culture is not null && pathInfo.Language is null)
                    {
                        pathInfo.Language = culture.ThreeLetterISOLanguageName;
                        extraString = extraString.Replace(currentSlice, string.Empty, StringComparison.OrdinalIgnoreCase);
                    }
                    else if (culture is not null && pathInfo.Language == "hin")
                    {
                        // Hindi language code "hi" collides with a hearing impaired flag - use as Hindi only if no other language is set
                        pathInfo.IsHearingImpaired = true;
                        pathInfo.Language = culture.ThreeLetterISOLanguageName;
                        extraString = extraString.Replace(currentSlice, string.Empty, StringComparison.OrdinalIgnoreCase);
                    }
                    else if (_namingOptions.MediaHearingImpairedFlags.Any(s => currentSliceWithoutSeparator.Contains(s, StringComparison.OrdinalIgnoreCase)))
                    {
                        pathInfo.IsHearingImpaired = true;
                        extraString = extraString.Replace(currentSlice, string.Empty, StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        titleString = currentSlice + titleString;
                    }

                    languageString = languageString[..lastSeparator];
                }

                pathInfo.Title = titleString.Length >= SeparatorLength ? titleString[SeparatorLength..] : null;
            }

            return pathInfo;
        }
    }
}
