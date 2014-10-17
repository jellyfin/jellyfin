using MediaBrowser.Model.MediaInfo;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public static class EncodingUtils
    {
        public static string GetInputArgument(List<string> inputFiles, MediaProtocol protocol)
        {
            if (protocol == MediaProtocol.Http)
            {
                var url = inputFiles.First();

                return string.Format("\"{0}\"", url);
            }
            if (protocol == MediaProtocol.Rtmp)
            {
                var url = inputFiles.First();

                return string.Format("\"{0}\"", url);
            }
            if (protocol == MediaProtocol.Rtsp)
            {
                var url = inputFiles.First();

                return string.Format("\"{0}\"", url);
            }

            return GetConcatInputArgument(inputFiles);
        }

        /// <summary>
        /// Gets the concat input argument.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <returns>System.String.</returns>
        private static string GetConcatInputArgument(IReadOnlyList<string> inputFiles)
        {
            // Get all streams
            // If there's more than one we'll need to use the concat command
            if (inputFiles.Count > 1)
            {
                var files = string.Join("|", inputFiles);

                return string.Format("concat:\"{0}\"", files);
            }

            // Determine the input path for video files
            return GetFileInputArgument(inputFiles[0]);
        }

        /// <summary>
        /// Gets the file input argument.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        private static string GetFileInputArgument(string path)
        {
            return string.Format("file:\"{0}\"", path);
        }

        public static string GetProbeSizeArgument(bool isDvd)
        {
            return isDvd ? "-probesize 1G -analyzeduration 200M" : string.Empty;
        }
    }
}
