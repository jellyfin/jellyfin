using System;
using Microsoft.Extensions.Configuration;

namespace MediaBrowser.Controller.Extensions
{
    /// <summary>
    /// Configuration extensions for <c>MediaBrowser.Controller</c>.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// The key for a setting that indicates whether the application should host static web content.
        /// </summary>
        public const string NoWebContentKey = "nowebcontent";

        /// <summary>
        /// The key for the FFmpeg probe size option.
        /// </summary>
        public const string FfmpegProbeSizeKey = "FFmpeg:probesize";

        /// <summary>
        /// The key for the FFmpeg analyse duration option.
        /// </summary>
        public const string FfmpegAnalyzeDurationKey = "FFmpeg:analyzeduration";

        /// <summary>
        /// Retrieves a config value indicating whether the application should not host
        /// static web content from the <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="configuration">The configuration to retrieve the value from.</param>
        /// <returns>The parsed config value.</returns>
        /// <exception cref="FormatException">The config value is not a valid bool string. See <see cref="bool.Parse(string)"/>.</exception>
        public static bool IsNoWebContent(this IConfiguration configuration)
            => configuration.ParseBoolean(NoWebContentKey);

        /// <summary>
        /// Retrieves the FFmpeg probe size from the <see cref="IConfiguration" />.
        /// </summary>
        /// <param name="configuration">This configuration.</param>
        /// <returns>The FFmpeg probe size option.</returns>
        public static string GetFFmpegProbeSize(this IConfiguration configuration)
            => configuration[FfmpegProbeSizeKey];

        /// <summary>
        /// Retrieves the FFmpeg analyse duration from the <see cref="IConfiguration" />.
        /// </summary>
        /// <param name="configuration">This configuration.</param>
        /// <returns>The FFmpeg analyse duration option.</returns>
        public static string GetFFmpegAnalyzeDuration(this IConfiguration configuration)
            => configuration[FfmpegAnalyzeDurationKey];

        /// <summary>
        /// Convert the specified configuration string value its <see cref="bool"/> equivalent.
        /// </summary>
        /// <param name="configuration">The configuration to retrieve and parse the setting from.</param>
        /// <param name="key">The key to use to retrieve the string value from the configuration.</param>
        /// <returns>The parsed boolean value.</returns>
        /// <exception cref="FormatException">The config value is not a valid bool string. See <see cref="bool.Parse(string)"/>.</exception>
        public static bool ParseBoolean(this IConfiguration configuration, string key)
        {
            string configValue = configuration[key];
            return bool.TryParse(configValue, out bool result) ?
                result :
                throw new FormatException($"Invalid value for configuration option '{key}' (expected a boolean): {configValue}");
        }
    }
}
