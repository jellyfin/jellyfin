using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Emby.Naming.Common;

namespace Emby.Naming.Audio
{
    /// <summary>
    /// Helper class to determine if Album is multipart.
    /// </summary>
    public class AlbumParser
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
            filename = filename.Replace('-', ' ');
            filename = filename.Replace('.', ' ');
            filename = filename.Replace('(', ' ');
            filename = filename.Replace(')', ' ');
            filename = Regex.Replace(filename, @"\s+", " ");

            ReadOnlySpan<char> trimmedFilename = filename.TrimStart();

            foreach (var prefix in _options.AlbumStackingPrefixes)
            {
                if (!trimmedFilename.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var tmp = trimmedFilename.Slice(prefix.Length).Trim();

                int index = tmp.IndexOf(' ');
                if (index != -1)
                {
                    tmp = tmp.Slice(0, index);
                }

                if (int.TryParse(tmp, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
