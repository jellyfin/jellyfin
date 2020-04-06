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

        public AudioBookFileInfo ParseFile(string path)
        {
            return Resolve(path, false);
        }

        public AudioBookFileInfo ParseDirectory(string path)
        {
            return Resolve(path, true);
        }

        public AudioBookFileInfo Resolve(string path, bool isDirectory = false)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            // TODO
            if (isDirectory)
            {
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

            return new AudioBookFileInfo
            {
                Path = path,
                Container = container,
                PartNumber = parsingResult.PartNumber,
                ChapterNumber = parsingResult.ChapterNumber,
                IsDirectory = isDirectory
            };
        }
    }
}
