using System;
using System.IO;
using System.Linq;
using Emby.Naming.Common;

namespace Emby.Naming.Subtitles
{
    /// <summary>
    /// Subtitle Parser class.
    /// </summary>
    public class SubtitleParser
    {
        private readonly NamingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleParser"/> class.
        /// </summary>
        /// <param name="options"><see cref="NamingOptions"/> object containing SubtitleFileExtensions, SubtitleDefaultFlags, SubtitleForcedFlags and SubtitleFlagDelimiters.</param>
        public SubtitleParser(NamingOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Parse file to determine if is subtitle and <see cref="SubtitleInfo"/>.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>Returns null or <see cref="SubtitleInfo"/> object if parsing is successful.</returns>
        public SubtitleInfo? ParseFile(string path)
        {
            if (path.Length == 0)
            {
                return null;
            }

            var extension = Path.GetExtension(path);
            if (!_options.SubtitleFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            var flags = GetFlags(path);
            var info = new SubtitleInfo(
                path,
                _options.SubtitleDefaultFlags.Any(i => flags.Contains(i, StringComparer.OrdinalIgnoreCase)),
                _options.SubtitleForcedFlags.Any(i => flags.Contains(i, StringComparer.OrdinalIgnoreCase)));

            var parts = flags.Where(i => !_options.SubtitleDefaultFlags.Contains(i, StringComparer.OrdinalIgnoreCase)
                && !_options.SubtitleForcedFlags.Contains(i, StringComparer.OrdinalIgnoreCase))
                .ToList();

            // Should have a name, language and file extension
            if (parts.Count >= 3)
            {
                info.Language = parts[^2];
            }

            return info;
        }

        private string[] GetFlags(string path)
        {
            // Note: the tags need be surrounded be either a space ( ), hyphen -, dot . or underscore _.

            var file = Path.GetFileName(path);

            return file.Split(_options.SubtitleFlagDelimiters, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
