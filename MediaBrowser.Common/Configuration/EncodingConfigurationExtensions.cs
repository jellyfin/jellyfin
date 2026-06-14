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
        /// Gets the encoding options.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <returns>The encoding options.</returns>
        public static EncodingOptions GetEncodingOptions(this IConfigurationManager configurationManager)
            => configurationManager.GetConfiguration<EncodingOptions>("encoding");

        /// <summary>
        /// Retrieves the transcoding temp path from the encoding configuration. Falls back to a default
        /// when no path is configured, or when the configured path exists but cannot be used (for example
        /// it is not writable), so a bad path does not stop the server or transcoding. The directory is
        /// created if it does not exist.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <returns>The transcoding temp path.</returns>
        /// <exception cref="NotSupportedException">If a configured transcoding path is invalid.</exception>
        /// <exception cref="UnauthorizedAccessException">If the default path cannot be created due to missing permissions.</exception>
        /// <exception cref="IOException">If the default path could not be created.</exception>
        public static string GetTranscodePath(this IConfigurationManager configurationManager)
        {
            // Get the configured path and fall back to a default
            var defaultPath = Path.Combine(configurationManager.CommonApplicationPaths.CachePath, "transcodes");
            var transcodingTempPath = configurationManager.GetEncodingOptions().TranscodingTempPath;
            if (string.IsNullOrEmpty(transcodingTempPath))
            {
                transcodingTempPath = defaultPath;
            }

            try
            {
                configurationManager.CommonApplicationPaths.CreateAndCheckMarker(transcodingTempPath, "transcode", true);
            }
            catch (Exception ex) when (transcodingTempPath != defaultPath && ex is IOException or UnauthorizedAccessException)
            {
                // The configured path is not usable (e.g. it exists but is not writable). Fall back to the
                // default so the server still starts and transcoding keeps working. The configured value is
                // left untouched so a transient cause (such as a not-yet-mounted drive) self-heals on the
                // next start.
                configurationManager.CommonApplicationPaths.CreateAndCheckMarker(defaultPath, "transcode", true);
                return defaultPath;
            }

            return transcodingTempPath;
        }
    }
}
