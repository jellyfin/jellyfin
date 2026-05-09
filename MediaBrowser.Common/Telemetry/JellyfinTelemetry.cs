using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MediaBrowser.Common.Telemetry;

/// <summary>
/// The telemetry sources owned by the server itself.
/// </summary>
public static class JellyfinTelemetry
{
    /// <summary>
    /// The name shared by all server owned telemetry sources.
    /// </summary>
    public const string SourceName = "Jellyfin";

    /// <summary>
    /// The wildcard matching every server owned telemetry source, including future ones.
    /// </summary>
    public const string SourceNameWildcard = "Jellyfin*";

    private static readonly string _version = typeof(JellyfinTelemetry).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    /// <summary>
    /// Gets the <see cref="ActivitySource"/> spans of server internals are emitted on.
    /// </summary>
    public static ActivitySource ActivitySource { get; } = new(SourceName, _version);

    /// <summary>
    /// Gets the <see cref="Meter"/> server internal metrics are emitted on.
    /// </summary>
    public static Meter Meter { get; } = new(SourceName, _version);
}
