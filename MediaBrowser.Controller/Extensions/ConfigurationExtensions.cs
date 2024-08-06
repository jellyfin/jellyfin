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
        /// The key for a setting that specifies the default redirect path
        /// to use for requests where the URL base prefix is invalid or missing..
        /// </summary>
        public const string DefaultRedirectKey = "DefaultRedirectPath";

        /// <summary>
        /// The key for the address override option.
        /// </summary>
        public const string AddressOverrideKey = "PublishedServerUrl";

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
        /// The key for the FFmpeg path option.
        /// </summary>
        public const string FfmpegPathKey = "ffmpeg";

        /// <summary>
        /// The key for a setting that indicates whether playlists should allow duplicate entries.
        /// </summary>
        public const string PlaylistsAllowDuplicatesKey = "playlists:allowDuplicates";

        /// <summary>
        /// The key for a setting that indicates whether kestrel should bind to a unix socket.
        /// </summary>
        public const string BindToUnixSocketKey = "kestrel:socket";

        /// <summary>
        /// The key for the unix socket path.
        /// </summary>
        public const string UnixSocketPathKey = "kestrel:socketPath";

        /// <summary>
        /// The permissions for the unix socket.
        /// </summary>
        public const string UnixSocketPermissionsKey = "kestrel:socketPermissions";

        /// <summary>
        /// The cache size of the SQL database, see cache_size.
        /// </summary>
        public const string SqliteCacheSizeKey = "sqlite:cacheSize";

        /// <summary>
        /// The key for a setting that indicates whether the application should detect network status change.
        /// </summary>
        public const string DetectNetworkChangeKey = "DetectNetworkChange";

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
        public static string? GetFFmpegProbeSize(this IConfiguration configuration)
            => configuration[FfmpegProbeSizeKey];

        /// <summary>
        /// Gets the FFmpeg analyze duration from the <see cref="IConfiguration" />.
        /// </summary>
        /// <param name="configuration">The configuration to read the setting from.</param>
        /// <returns>The FFmpeg analyze duration option.</returns>
        public static string? GetFFmpegAnalyzeDuration(this IConfiguration configuration)
            => configuration[FfmpegAnalyzeDurationKey];

        /// <summary>
        /// Gets a value indicating whether playlists should allow duplicate entries from the <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="configuration">The configuration to read the setting from.</param>
        /// <returns>True if playlists should allow duplicates, otherwise false.</returns>
        public static bool DoPlaylistsAllowDuplicates(this IConfiguration configuration)
            => configuration.GetValue<bool>(PlaylistsAllowDuplicatesKey);

        /// <summary>
        /// Gets a value indicating whether kestrel should bind to a unix socket from the <see cref="IConfiguration" />.
        /// </summary>
        /// <param name="configuration">The configuration to read the setting from.</param>
        /// <returns><c>true</c> if kestrel should bind to a unix socket, otherwise <c>false</c>.</returns>
        public static bool UseUnixSocket(this IConfiguration configuration)
            => configuration.GetValue<bool>(BindToUnixSocketKey);

        /// <summary>
        /// Gets the path for the unix socket from the <see cref="IConfiguration" />.
        /// </summary>
        /// <param name="configuration">The configuration to read the setting from.</param>
        /// <returns>The unix socket path.</returns>
        public static string? GetUnixSocketPath(this IConfiguration configuration)
            => configuration[UnixSocketPathKey];

        /// <summary>
        /// Gets the permissions for the unix socket from the <see cref="IConfiguration" />.
        /// </summary>
        /// <param name="configuration">The configuration to read the setting from.</param>
        /// <returns>The unix socket permissions.</returns>
        public static string? GetUnixSocketPermissions(this IConfiguration configuration)
            => configuration[UnixSocketPermissionsKey];

        /// <summary>
        /// Gets the cache_size from the <see cref="IConfiguration" />.
        /// </summary>
        /// <param name="configuration">The configuration to read the setting from.</param>
        /// <returns>The sqlite cache size.</returns>
        public static int? GetSqliteCacheSize(this IConfiguration configuration)
            => configuration.GetValue<int?>(SqliteCacheSizeKey);
    }
}
