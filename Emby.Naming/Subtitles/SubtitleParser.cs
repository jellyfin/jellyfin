using Emby.Naming.Common;
using System;
using System.IO;
using System.Linq;

namespace Emby.Naming.Subtitles
{
    public class SubtitleParser
    {
        private readonly NamingOptions _options;

        public SubtitleParser(NamingOptions options)
        {
            _options = options;
        }

        public SubtitleInfo ParseFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var extension = Path.GetExtension(path);
            if (!_options.SubtitleFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            var flags = GetFlags(path);

            var info = new SubtitleInfo
            {
                Path = path,
                IsDefault = _options.SubtitleDefaultFlags.Any(i => flags.Contains(i, StringComparer.OrdinalIgnoreCase)),
                IsForced = _options.SubtitleForcedFlags.Any(i => flags.Contains(i, StringComparer.OrdinalIgnoreCase))
            };

            var parts = flags.Where(i => !_options.SubtitleDefaultFlags.Contains(i, StringComparer.OrdinalIgnoreCase) && !_options.SubtitleForcedFlags.Contains(i, StringComparer.OrdinalIgnoreCase))
                .ToList();

            // Should have a name, language and file extension
            if (parts.Count >= 3)
            {
                info.Language = parts[parts.Count - 2];
            }

            return info;
        }

        private string[] GetFlags(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            // Note: the tags need be be surrounded be either a space ( ), hyphen -, dot . or underscore _.

            var file = Path.GetFileName(path);

            return file.Split(_options.SubtitleFlagDelimiters, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
