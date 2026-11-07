using Prometheus;

namespace Jellyfin.Server.Implementations.Metrics
{
    /// <summary>
    /// Central registry for Jellyfin-specific Prometheus metrics.
    /// </summary>
    public static class JellyfinMetrics
    {
        /// <summary>
        /// Gets the gauge that tracks the total number of Jellyfin user accounts.
        /// </summary>
        public static Gauge UserAccounts { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_user_accounts",
            "Number of Jellyfin user accounts, including disabled and hidden accounts.");

        /// <summary>
        /// Gets the gauge that tracks currently connected sessions by device type and client.
        /// </summary>
        public static Gauge ConnectedSessions { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_connected_sessions",
            "Number of client sessions currently connected to the server, grouped by the client-reported device_type and client values.",
            "device_type",
            "client");

        /// <summary>
        /// Gets the gauge that tracks registered client device records, including devices not currently connected.
        /// </summary>
        public static Gauge RegisteredDevices { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_registered_devices",
            "Number of registered client device records, including devices not currently connected.");

        /// <summary>
        /// Gets the gauge that tracks the number of active streams by play method.
        /// </summary>
        public static Gauge ActiveStreams { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_active_streams",
            "Number of connected sessions currently playing by method: direct_play, direct_stream, transcode, or unknown.",
            "play_method");

        /// <summary>
        /// Gets the gauge that tracks the number of active transcodes by acceleration method and codecs.
        /// </summary>
        public static Gauge ActiveTranscodes { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_active_transcodes",
            "Number of sessions with transcoding information by configured acceleration mode (software, amf, qsv, nvenc, v4l2m2m, vaapi, videotoolbox, rkmpp, unknown) and lowercase output codecs; this does not confirm hardware use.",
            "acceleration",
            "video_codec",
            "audio_codec");

        /// <summary>
        /// Gets the counter that tracks playback starts by play method and transcode reason.
        /// </summary>
        public static Counter PlayMethodsTotal { get; } = Prometheus.Metrics.CreateCounter(
            "jellyfin_play_methods_total",
            "Number of playback starts by method and stream-transcode reason (not hardware-acceleration selection reason); stable snake_case reasons are joined with |, none means no reason was reported, and unknown means transcode details were unavailable.",
            "play_method",
            "transcode_reason");

        /// <summary>
        /// Gets the counter that tracks transcoding playback starts by acceleration method.
        /// </summary>
        public static Counter TranscodesTotal { get; } = Prometheus.Metrics.CreateCounter(
            "jellyfin_transcodes_total",
            "Number of transcoding playback starts by configured acceleration mode; this does not explain why the mode was selected or confirm hardware use.",
            "acceleration");

        /// <summary>
        /// Gets the gauge that tracks the number of libraries by collection type.
        /// </summary>
        public static Gauge Libraries { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_libraries",
            "Number of configured virtual folders by collection type: movies, tvshows, music, musicvideos, homevideos, boxsets, books, mixed, or unknown.",
            "type");

        /// <summary>
        /// Gets the gauge that tracks the number of library items by media type.
        /// </summary>
        public static Gauge LibraryItems { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_library_items",
            "Number of library items by media type: movie, series, episode, album, song, artist, music_video, book, box_set, trailer, or program.",
            "type");

        /// <summary>
        /// Gets the gauge that tracks media streams by library, stream kind, and codec.
        /// </summary>
        public static Gauge MediaStreams { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_media_streams",
            "Number of indexed media streams by library, kind, and lowercase codec. Library labels contain the configured name and stable item ID.",
            "library",
            "library_id",
            "kind",
            "codec");

        /// <summary>
        /// Gets the gauge that tracks the number of users with a specific permission.
        /// </summary>
        public static Gauge UsersWithPermission { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_users_with_permission",
            "Number of Jellyfin user accounts with each supported permission enabled, labeled by permission.",
            "permission");

        /// <summary>
        /// Gets the gauge that tracks the number of users authenticating via an external provider.
        /// </summary>
        public static Gauge UsersAuthenticatingExternal { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_users_authenticating_external",
            "Number of Jellyfin user accounts using each external authentication provider, labeled by provider ID (type full name), not display name.",
            "provider");
    }
}
