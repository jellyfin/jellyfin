using Emby.Naming.Common;
using Emby.Naming.Video;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Emby.Naming.Audio
{
    public class AlbumParser
    {
        private readonly NamingOptions _options;

        public AlbumParser(NamingOptions options)
        {
            _options = options;
        }

        public MultiPartResult ParseMultiPart(string path)
        {
            var result = new MultiPartResult();

            var filename = Path.GetFileName(path);

            if (string.IsNullOrEmpty(filename))
            {
                return result;
            }

            // TODO: Move this logic into options object
            // Even better, remove the prefixes and come up with regexes
            // But Kodi documentation seems to be weak for audio

            // Normalize
            // Remove whitespace
            filename = filename.Replace("-", " ");
            filename = filename.Replace(".", " ");
            filename = filename.Replace("(", " ");
            filename = filename.Replace(")", " ");
            filename = Regex.Replace(filename, @"\s+", " ");

            filename = filename.TrimStart();

            foreach (var prefix in _options.AlbumStackingPrefixes)
            {
                if (filename.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var tmp = filename.Substring(prefix.Length);

                    tmp = tmp.Trim().Split(' ').FirstOrDefault() ?? string.Empty;

                    int val;
                    if (int.TryParse(tmp, NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
                    {
                        result.IsMultiPart = true;
                        break;
                    }
                }
            }

            return result;
        }
    }
}
