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
        /// The key for a setting that indicates whether the application should host web client content.
        /// </summary>
        public const string HostWebClientKey = "hostwebclient";

        /// <summary>
        /// The key for the FFmpeg probe size option.
        /// </summary>
        public const string FfmpegProbeSizeKey = "FFmpeg:probesize";

        /// <summary>
        /// The key for the FFmpeg analyze duration option.
        /// </summary>
        public const string FfmpegAnalyzeDurationKey = "FFmpeg:analyzeduration";

        /// <summary>
        /// The key for a setting that indicates whether playlists should allow duplicate entries.
        /// </summary>
        public const string PlaylistsAllowDuplicatesKey = "playlists:allowDuplicates";

        /// <summary>
        /// Gets a value indicating whether the application should host static web content from the <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="configuration">The configuration to retrieve the value from.</param>
        /// <returns>The parsed config value.</returns>
        /// <exception cref="FormatException">The config value is not a valid bool string. See <see cref="bool.Parse(string)"/>.</exception>
        public static bool HostWebClient(this IConfiguration configuration)
            => configuration.GetValue<bool>(HostWebClientKey);

        /// <summary>
        /// Gets the FFmpeg probe size from the <see cref="IConfiguration" />.
        /// </summary>
        /// <param name="configuration">The configuration to read the setting from.</param>
        /// <returns>The FFmpeg probe size option.</returns>
        public static string GetFFmpegProbeSize(this IConfiguration configuration)
            => configuration[FfmpegProbeSizeKey];

        /// <summary>
        /// Gets the FFmpeg analyze duration from the <see cref="IConfiguration" />.
        /// </summary>
        /// <param name="configuration">The configuration to read the setting from.</param>
        /// <returns>The FFmpeg analyze duration option.</returns>
        public static string GetFFmpegAnalyzeDuration(this IConfiguration configuration)
            => configuration[FfmpegAnalyzeDurationKey];

        /// <summary>
        /// Gets a value indicating whether playlists should allow duplicate entries from the <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="configuration">The configuration to read the setting from.</param>
        /// <returns>True if playlists should allow duplicates, otherwise false.</returns>
        public static bool DoPlaylistsAllowDuplicates(this IConfiguration configuration)
            => configuration.GetValue<bool>(PlaylistsAllowDuplicatesKey);
    }
}
