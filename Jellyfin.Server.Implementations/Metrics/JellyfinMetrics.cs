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
        /// Gets the gauge that tracks the number of active sessions by device type and client.
        /// </summary>
        public static Gauge ActiveSessions { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_active_sessions",
            "Number of active sessions.",
            "device_type",
            "client");

        /// <summary>
        /// Gets the gauge that tracks the total number of sessions.
        /// </summary>
        public static Gauge TotalSessions { get; } = Prometheus.Metrics.CreateGauge(
            "jellyfin_total_sessions",
            "Total number of sessions.");

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
    }
}
