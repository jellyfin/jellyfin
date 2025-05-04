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
        public static string GetInputArgument(string inputPrefix, string inputFile, MediaProtocol protocol)
        {
            if (protocol != MediaProtocol.File)
            {
                return string.Format(CultureInfo.InvariantCulture, "\"{0}\"", inputFile);
            }

            return GetFileInputArgument(inputFile, inputPrefix);
        }

        public static string GetInputArgument(string inputPrefix, IReadOnlyList<string> inputFiles, MediaProtocol protocol)
        {
            if (protocol != MediaProtocol.File)
            {
                return string.Format(CultureInfo.InvariantCulture, "\"{0}\"", inputFiles[0]);
            }

            return GetConcatInputArgument(inputFiles, inputPrefix);
        }

        /// <summary>
        /// Gets the concat input argument.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="inputPrefix">The input prefix.</param>
        /// <returns>System.String.</returns>
        private static string GetConcatInputArgument(IReadOnlyList<string> inputFiles, string inputPrefix)
        {
            // Get all streams
            // If there's more than one we'll need to use the concat command
            if (inputFiles.Count > 1)
            {
                var files = string.Join('|', inputFiles.Select(NormalizePath));

                return string.Format(CultureInfo.InvariantCulture, "concat:\"{0}\"", files);
            }

            // Determine the input path for video files
            return GetFileInputArgument(inputFiles[0], inputPrefix);
        }

        /// <summary>
        /// Gets the file input argument.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="inputPrefix">The path prefix.</param>
        /// <returns>System.String.</returns>
        private static string GetFileInputArgument(string path, string inputPrefix)
        {
            if (path.Contains("://", StringComparison.Ordinal))
            {
                return string.Format(CultureInfo.InvariantCulture, "\"{0}\"", path);
            }

            // Quotes are valid path characters in linux and they need to be escaped here with a leading \
            path = NormalizePath(path);

            return string.Format(CultureInfo.InvariantCulture, "{1}:\"{0}\"", path, inputPrefix);
        }

        /// <summary>
        /// Normalizes the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        public static string NormalizePath(string path)
        {
            // Quotes are valid path characters in linux and they need to be escaped here with a leading \
            return path.Replace("\"", "\\\"", StringComparison.Ordinal);
        }
    }
}
