using System;
using System.IO;
using Emby.Naming.Common;
using Jellyfin.Extensions;

namespace Emby.Naming.AudioBook
{
    /// <summary>
    /// Resolve specifics (path, container, partNumber, chapterNumber) about audiobook file.
    /// </summary>
    public class AudioBookResolver
    {
        private readonly NamingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioBookResolver"/> class.
        /// </summary>
        /// <param name="options"><see cref="NamingOptions"/> containing AudioFileExtensions and also used to pass to AudioBookFilePathParser.</param>
        public AudioBookResolver(NamingOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Resolve specifics (path, container, partNumber, chapterNumber) about audiobook file.
        /// </summary>
        /// <param name="path">Path to audiobook file.</param>
        /// <returns>Returns <see cref="AudioBookResolver"/> object.</returns>
        public AudioBookFileInfo? Resolve(string path)
        {
            if (path.Length == 0 || Path.GetFileNameWithoutExtension(path).Length == 0)
            {
                // Return null to indicate this path will not be used, instead of stopping whole process with exception
                return null;
            }

            var extension = Path.GetExtension(path);

            // Check supported extensions
            if (!_options.AudioFileExtensions.Contains(extension, StringComparison.OrdinalIgnoreCase))
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
