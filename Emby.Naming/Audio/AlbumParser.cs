using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Emby.Naming.Common;
using Jellyfin.Extensions;

namespace Emby.Naming.Audio
{
    /// <summary>
    /// Helper class to determine if Album is multipart.
    /// </summary>
    public partial class AlbumParser
    {
        private readonly NamingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlbumParser"/> class.
        /// </summary>
        /// <param name="options">Naming options containing AlbumStackingPrefixes.</param>
        public AlbumParser(NamingOptions options)
        {
            _options = options;
        }

        [GeneratedRegex(@"[-\.\(\)\s]+")]
        private static partial Regex CleanRegex();

        /// <summary>
        /// Function that determines if album is multipart.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>True if album is multipart.</returns>
        public bool IsMultiPart(string path)
        {
            var filename = Path.GetFileName(path);
            if (filename.Length == 0)
            {
                return false;
            }

            // TODO: Move this logic into options object
            // Even better, remove the prefixes and come up with regexes
            // But Kodi documentation seems to be weak for audio

            // Normalize
            // Remove whitespace
            filename = CleanRegex().Replace(filename, " ");

            ReadOnlySpan<char> trimmedFilename = filename.AsSpan().TrimStart();

            foreach (var prefix in _options.AlbumStackingPrefixes)
            {
                if (!trimmedFilename.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var tmp = trimmedFilename.Slice(prefix.Length).Trim();

                if (int.TryParse(tmp.LeftPart(' '), CultureInfo.InvariantCulture, out _))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
