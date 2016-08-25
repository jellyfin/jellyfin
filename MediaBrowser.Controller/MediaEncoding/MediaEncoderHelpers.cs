using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonIO;

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
        /// <param name="protocol">The protocol.</param>
        /// <param name="isoMount">The iso mount.</param>
        /// <param name="playableStreamFileNames">The playable stream file names.</param>
        /// <returns>System.String[][].</returns>
        public static string[] GetInputArgument(IFileSystem fileSystem, string videoPath, MediaProtocol protocol, IIsoMount isoMount, List<string> playableStreamFileNames)
        {
            if (playableStreamFileNames.Count > 0)
            {
                if (isoMount == null)
                {
                    return GetPlayableStreamFiles(fileSystem, videoPath, playableStreamFileNames).ToArray();
                }
                return GetPlayableStreamFiles(fileSystem, isoMount.MountedPath, playableStreamFileNames).ToArray();
            }

            return new[] {videoPath};
        }

        private static List<string> GetPlayableStreamFiles(IFileSystem fileSystem, string rootPath, List<string> filenames)
        {
            if (filenames.Count == 0)
            {
                return new List<string>();
            }

            var allFiles = fileSystem
                .GetFilePaths(rootPath, true)
                .ToList();

            return filenames.Select(name => allFiles.FirstOrDefault(f => string.Equals(Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase)))
                .Where(f => !string.IsNullOrEmpty(f))
                .ToList();
        }
    }
}
