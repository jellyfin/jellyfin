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
            if (protocol == MediaProtocol.Udp)
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
                var files = string.Join("|", inputFiles.Select(NormalizePath).ToArray());

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
            // Quotes are valid path characters in linux and they need to be escaped here with a leading \
            path = NormalizePath(path);

            return string.Format("file:\"{0}\"", path);
        }

        /// <summary>
        /// Normalizes the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        private static string NormalizePath(string path)
        {
            // Quotes are valid path characters in linux and they need to be escaped here with a leading \
            return path.Replace("\"", "\\\"");
        }

        public static string GetProbeSizeArgument(int numInputFiles)
        {
            return numInputFiles > 1 ? "-probesize 1G" : "";
        }

        public static string GetAnalyzeDurationArgument(int numInputFiles)
        {
            return numInputFiles > 1 ? "-analyzeduration 200M" : "";
        }
    }
}
