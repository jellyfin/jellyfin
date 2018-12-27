using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using System;
using System.IO;
using System.Linq;

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
        public static string[] GetInputArgument(IFileSystem fileSystem, string videoPath, MediaProtocol protocol, IIsoMount isoMount, string[] playableStreamFileNames)
        {
            if (playableStreamFileNames.Length > 0)
            {
                if (isoMount == null)
                {
                    return GetPlayableStreamFiles(fileSystem, videoPath, playableStreamFileNames);
                }
                return GetPlayableStreamFiles(fileSystem, isoMount.MountedPath, playableStreamFileNames);
            }

            return new[] {videoPath};
        }

        private static string[] GetPlayableStreamFiles(IFileSystem fileSystem, string rootPath, string[] filenames)
        {
            if (filenames.Length == 0)
            {
                return new string[]{};
            }

            var allFiles = fileSystem
                .GetFilePaths(rootPath, true)
                .ToArray();

            return filenames.Select(name => allFiles.FirstOrDefault(f => string.Equals(Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase)))
                .Where(f => !string.IsNullOrEmpty(f))
                .ToArray();
        }
    }
}
