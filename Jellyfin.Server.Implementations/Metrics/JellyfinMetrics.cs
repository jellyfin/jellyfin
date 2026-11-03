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
    }
}
