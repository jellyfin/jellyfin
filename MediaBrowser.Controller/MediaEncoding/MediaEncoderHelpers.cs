#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.MediaEncoding
{
    /// <summary>
    /// Class MediaEncoderHelpers.
    /// </summary>
    public static class MediaEncoderHelpers
    {
        /// <summary>
        /// Gets the input argument.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="videoPath">The video path.</param>
        /// <param name="playableStreamFileNames">The playable stream file names.</param>
        /// <returns>string[].</returns>
        public static string[] GetInputArgument(IFileSystem fileSystem, string videoPath, IReadOnlyCollection<string> playableStreamFileNames)
        {
            if (playableStreamFileNames.Count > 0)
            {
                return GetPlayableStreamFiles(fileSystem, videoPath, playableStreamFileNames);
            }

            return new[] { videoPath };
        }

        private static string[] GetPlayableStreamFiles(IFileSystem fileSystem, string rootPath, IEnumerable<string> filenames)
        {
            var allFiles = fileSystem
                .GetFilePaths(rootPath, true)
                .ToArray();

            return filenames.Select(name => allFiles.FirstOrDefault(f => string.Equals(Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase)))
                .Where(f => !string.IsNullOrEmpty(f))
                .ToArray();
        }
    }
}
