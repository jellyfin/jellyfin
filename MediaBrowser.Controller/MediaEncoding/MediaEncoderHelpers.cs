using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.MediaEncoding
{
    /// <summary>
    /// Class MediaEncoderHelpers
    /// </summary>
    public static class MediaEncoderHelpers
    {
        /// <summary>
        /// Gets the input argument.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="videoPath">The video path.</param>
        /// <param name="isoMount">The iso mount.</param>
        /// <param name="playableStreamFileNames">The playable stream file names.</param>
        /// <returns>string[].</returns>
        public static string[] GetInputArgument(IFileSystem fileSystem, string videoPath, IIsoMount isoMount, IReadOnlyCollection<string> playableStreamFileNames)
        {
            if (playableStreamFileNames.Count > 0)
            {
                if (isoMount == null)
                {
                    return GetPlayableStreamFiles(fileSystem, videoPath, playableStreamFileNames);
                }

                return GetPlayableStreamFiles(fileSystem, isoMount.MountedPath, playableStreamFileNames);
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
