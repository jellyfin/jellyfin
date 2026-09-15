using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Server.Implementations.Metrics;

/// <summary>
/// Defines a Prometheus metrics collector that refreshes its gauges or counters when invoked.
/// </summary>
public interface IMetricsCollector
{
    /// <summary>
    /// Gets the human-readable name of the collector, used in log output.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Refreshes all metrics owned by the collector.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task CollectAsync(CancellationToken cancellationToken);
}
