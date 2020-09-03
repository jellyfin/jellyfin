#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public static class EncodingUtils
    {
        public static string GetInputArgument(IReadOnlyList<string> inputFiles, MediaProtocol protocol)
        {
            if (protocol != MediaProtocol.File)
            {
                var url = inputFiles[0];

                return string.Format(CultureInfo.InvariantCulture, "\"{0}\"", url);
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
                var files = string.Join("|", inputFiles.Select(NormalizePath));

                return string.Format(CultureInfo.InvariantCulture, "concat:\"{0}\"", files);
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
            if (path.IndexOf("://", StringComparison.Ordinal) != -1)
            {
                return string.Format(CultureInfo.InvariantCulture, "\"{0}\"", path);
            }

            // Quotes are valid path characters in linux and they need to be escaped here with a leading \
            path = NormalizePath(path);

            return string.Format(CultureInfo.InvariantCulture, "file:\"{0}\"", path);
        }

        /// <summary>
        /// Normalizes the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        private static string NormalizePath(string path)
        {
            // Quotes are valid path characters in linux and they need to be escaped here with a leading \
            return path.Replace("\"", "\\\"", StringComparison.Ordinal);
        }
    }
}
