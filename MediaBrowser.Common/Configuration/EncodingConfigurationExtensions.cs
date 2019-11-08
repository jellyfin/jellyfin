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
        /// Retrieves the transcoding temp path from the encoding configuration.
        /// </summary>
        /// <param name="configurationManager">The Configuration manager.</param>
        /// <returns>The transcoding temp path.</returns>
        public static string GetTranscodePath(this IConfigurationManager configurationManager)
            => configurationManager.GetEncodingOptions().TranscodingTempPath
                ?? Path.Combine(configurationManager.CommonApplicationPaths.ProgramDataPath, "transcoding-temp");
    }
}
