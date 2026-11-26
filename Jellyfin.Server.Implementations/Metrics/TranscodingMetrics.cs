using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Session;
using Prometheus;

namespace Jellyfin.Server.Implementations.Metrics;

/// <summary>
/// Exposes Prometheus metrics describing the active transcoding sessions.
/// </summary>
public sealed class TranscodingMetrics : IMetricsCollector
{
    private static readonly Gauge _transcodes = Prometheus.Metrics
        .CreateGauge("jellyfin_transcoding_sessions", "Number of active transcoding sessions grouped by hardware acceleration usage.", "hardware");

    private static readonly Gauge _transcodingBitrate = Prometheus.Metrics
        .CreateGauge("jellyfin_transcoding_bitrate_bps", "Aggregated bitrate of all active transcoding sessions, in bits per second.");

    private readonly ISessionManager _sessionManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscodingMetrics"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager.</param>
    public TranscodingMetrics(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    /// <inheritdoc />
    public string Name => nameof(TranscodingMetrics);

    /// <inheritdoc />
    public Task CollectAsync(CancellationToken cancellationToken)
    {
        var transcodes = _sessionManager.Sessions
            .Where(s => s.TranscodingInfo is not null)
            .Select(s => s.TranscodingInfo)
            .ToList();

        var hardwareAccelerated = transcodes.Count(t => t.HardwareAccelerationType is not null);
        _transcodes.WithLabels("true").Set(hardwareAccelerated);
        _transcodes.WithLabels("false").Set(transcodes.Count - hardwareAccelerated);

        var totalBitrate = transcodes.Sum(t => t.Bitrate ?? 0);
        _transcodingBitrate.Set(totalBitrate);

        return Task.CompletedTask;
    }
}
