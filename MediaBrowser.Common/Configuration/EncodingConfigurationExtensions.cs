using System;
using System.IO;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Common.Configuration
{
    /// <summary>
    /// Class containing extension methods for working with the encoding configuration.
    /// </summary>
    public static class EncodingConfigurationExtensions
    {
        /// <summary>
        /// Retrieves the transcoding temp path from encoding options, falling back to a default if no path
        /// is specified in configuration. If the directory does not exist, it will be created.
        /// </summary>
        /// <param name="encodingOptions">The encoding options.</param>
        /// <param name="appPaths">The application paths.</param>
        /// <returns>The transcoding temp path.</returns>
        public static string GetTranscodePath(EncodingOptions encodingOptions, IApplicationPaths appPaths)
        {
            var transcodingTempPath = encodingOptions.TranscodingTempPath;
            if (string.IsNullOrEmpty(transcodingTempPath))
            {
                transcodingTempPath = Path.Combine(appPaths.CachePath, "transcodes");
            }

            appPaths.CreateAndCheckMarker(transcodingTempPath, "transcode", true);
            return transcodingTempPath;
        }
    }
}
