using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
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
        /// <param name="videoPath">The video path.</param>
        /// <param name="protocol">The protocol.</param>
        /// <param name="isoMount">The iso mount.</param>
        /// <param name="playableStreamFileNames">The playable stream file names.</param>
        /// <returns>System.String[][].</returns>
        public static string[] GetInputArgument(string videoPath, MediaProtocol protocol, IIsoMount isoMount, List<string> playableStreamFileNames)
        {
            if (playableStreamFileNames.Count > 0)
            {
                if (isoMount == null)
                {
                    return GetPlayableStreamFiles(videoPath, playableStreamFileNames).ToArray();
                }
                return GetPlayableStreamFiles(isoMount.MountedPath, playableStreamFileNames).ToArray();
            }

            return new[] {videoPath};
        }

        public static List<string> GetPlayableStreamFiles(string rootPath, IEnumerable<string> filenames)
        {
            var allFiles = Directory
                .EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
                .ToList();

            return filenames.Select(name => allFiles.FirstOrDefault(f => string.Equals(Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase)))
                .Where(f => !string.IsNullOrEmpty(f))
                .ToList();
        }
    }
}
