using Prometheus;

namespace Jellyfin.Server.Implementations.Metrics
{
    /// <summary>
    /// Central registry for Jellyfin-specific Prometheus metrics.
    /// </summary>
    /// <remarks>
    /// All Jellyfin metrics are declared here so that names, help texts and label sets are defined in one place.
    /// When adding a metric, follow Prometheus naming conventions: snake_case, a <c>jellyfin_</c> prefix, and a
    /// <c>_total</c> suffix for counters.
    /// </remarks>
    public static class JellyfinMetrics
    {
        /// <summary>
        /// Gets the gauge that tracks the total number of Jellyfin user accounts.
        /// </summary>
        public static Gauge UserAccounts { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_user_accounts",
            "Number of Jellyfin user accounts.");

        /// <summary>
        /// Gets the gauge that tracks currently connected sessions by device type and client.
        /// </summary>
        public static Gauge ConnectedSessions { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_connected_sessions",
            "Number of client sessions currently connected to the server.",
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
            "Number of active streams by play method.",
            "play_method");

        /// <summary>
        /// Gets the gauge that tracks the number of active transcodes by acceleration method and codecs.
        /// </summary>
        public static Gauge ActiveTranscodes { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_active_transcodes",
            "Number of active transcodes by acceleration method and codecs.",
            "acceleration",
            "video_codec",
            "audio_codec");

        /// <summary>
        /// Gets the counter that tracks playbacks by play method and transcode reason.
        /// </summary>
        public static Counter PlayMethodsTotal { get; } = Prometheus.Metrics.CreateCounter(
            "jellyfin_play_methods_total",
            "Total number of playbacks by play method and transcode reason.",
            "play_method",
            "transcode_reason");

        /// <summary>
        /// Gets the counter that tracks transcodes by acceleration method.
        /// </summary>
        public static Counter TranscodesTotal { get; } = Prometheus.Metrics.CreateCounter(
            "jellyfin_transcodes_total",
            "Total number of transcodes by acceleration method.",
            "acceleration");

        /// <summary>
        /// Gets the gauge that tracks the number of libraries by collection type.
        /// </summary>
        public static Gauge Libraries { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_libraries",
            "Number of libraries by collection type.",
            "type");

        /// <summary>
        /// Gets the gauge that tracks the number of library items by media type.
        /// </summary>
        public static Gauge LibraryItems { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_library_items",
            "Number of library items by media type.",
            "type");

        /// <summary>
        /// Gets the gauge that tracks the number of media streams by stream kind and codec.
        /// </summary>
        public static Gauge MediaStreams { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_media_streams",
            "Number of media streams by stream kind and codec.",
            "kind",
            "codec");

        /// <summary>
        /// Gets the gauge that tracks the number of users with a specific permission.
        /// </summary>
        public static Gauge UsersWithPermission { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_users_with_permission",
            "Number of users with a specific permission.",
            "permission");

        /// <summary>
        /// Gets the gauge that tracks the number of users authenticating via an external provider.
        /// </summary>
        public static Gauge UsersAuthenticatingExternal { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_users_authenticating_external",
            "Number of users authenticating via an external authentication provider.",
            "provider");
    }
}
