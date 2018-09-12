using System;
using System.IO;
using System.Linq;
using Emby.Naming.Common;
using Emby.Naming.TV;
using Emby.Naming.Video;

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

        public AudioBookFileInfo Resolve(string path, bool IsDirectory = false)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            if (IsDirectory)
                return null;

            var extension = Path.GetExtension(path) ?? string.Empty;
            // Check supported extensions
            if (!_options.AudioFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            var container = extension.TrimStart('.');

            var parsingResult = new AudioBookFilePathParser(_options)
                .Parse(path, IsDirectory);
            
            return new AudioBookFileInfo
            {
                Path = path,
                Container = container,
                PartNumber = parsingResult.PartNumber,
                ChapterNumber = parsingResult.ChapterNumber,
                IsDirectory = IsDirectory
            };
        }
    }
}
