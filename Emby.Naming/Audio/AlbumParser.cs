#nullable enable
#pragma warning disable CS1591

using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Emby.Naming.Common;

namespace Emby.Naming.Audio
{
    public class AlbumParser
    {
        private readonly NamingOptions _options;

        public AlbumParser(NamingOptions options)
        {
            _options = options;
        }

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
