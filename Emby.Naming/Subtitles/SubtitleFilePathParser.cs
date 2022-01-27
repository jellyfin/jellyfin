using System;
using System.IO;
using System.Linq;
using Emby.Naming.Common;
using Jellyfin.Extensions;

namespace Emby.Naming.Subtitles
{
    /// <summary>
    /// Subtitle Parser class.
    /// </summary>
    public class SubtitleFilePathParser
    {
        private readonly NamingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleFilePathParser"/> class.
        /// </summary>
        /// <param name="options"><see cref="NamingOptions"/> object containing SubtitleFileExtensions, SubtitleDefaultFlags, SubtitleForcedFlags and SubtitleFlagDelimiters.</param>
        public SubtitleFilePathParser(NamingOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Parse file to determine if it is a subtitle and <see cref="SubtitleFileInfo"/>.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>Returns null or <see cref="SubtitleFileInfo"/> object if parsing is successful.</returns>
        public SubtitleFileInfo? ParseFile(string path)
        {
            if (path.Length == 0)
            {
                return null;
            }

            var extension = Path.GetExtension(path);
            if (!_options.SubtitleFileExtensions.Contains(extension, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var flags = GetFileFlags(path);
            var info = new SubtitleFileInfo(
                path,
                _options.SubtitleDefaultFlags.Any(i => flags.Contains(i, StringComparison.OrdinalIgnoreCase)),
                _options.SubtitleForcedFlags.Any(i => flags.Contains(i, StringComparison.OrdinalIgnoreCase)));

            return info;
        }

        private string[] GetFileFlags(string path)
        {
            var file = Path.GetFileNameWithoutExtension(path);

            return file.Split(_options.SubtitleFlagDelimiters, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
