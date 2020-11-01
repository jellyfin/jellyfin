#nullable enable
#pragma warning disable CS1591

using System;
using System.IO;
using System.Linq;
using Emby.Naming.Common;

namespace Emby.Naming.AudioBook
{
    public class AudioBookResolver
    {
        private readonly NamingOptions _options;

        public AudioBookResolver(NamingOptions options)
        {
            _options = options;
        }

        public AudioBookFileInfo? Resolve(string path)
        {
            if (path.Length == 0)
            {
                // Return null to indicate this path will not be used, instead of stopping whole process with exception
                return null;
            }

            var extension = Path.GetExtension(path);

            // Check supported extensions
            if (!_options.AudioFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            var container = extension.TrimStart('.');

            var parsingResult = new AudioBookFilePathParser(_options).Parse(path);

            return new AudioBookFileInfo(
                path,
                container,
                chapterNumber: parsingResult.ChapterNumber,
                partNumber: parsingResult.PartNumber);
        }
    }
}
