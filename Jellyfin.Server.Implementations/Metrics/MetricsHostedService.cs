using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Metrics;

/// <summary>
/// Periodically refreshes every registered <see cref="IMetricsCollector"/> when Prometheus metrics are enabled.
/// </summary>
public sealed class MetricsHostedService : BackgroundService
{
    private static readonly TimeSpan _interval = TimeSpan.FromSeconds(15);

    private readonly IServerConfigurationManager _configurationManager;
    private readonly IEnumerable<IMetricsCollector> _collectors;
    private readonly ILogger<MetricsHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsHostedService"/> class.
    /// </summary>
    /// <param name="configurationManager">The server configuration manager.</param>
    /// <param name="collectors">All registered metrics collectors.</param>
    /// <param name="logger">The logger.</param>
    public MetricsHostedService(
        IServerConfigurationManager configurationManager,
        IEnumerable<IMetricsCollector> collectors,
        ILogger<MetricsHostedService> logger)
    {
        _configurationManager = configurationManager;
        _collectors = collectors;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        do
        {
            if (_configurationManager.Configuration.EnableMetrics)
            {
                foreach (var collector in _collectors)
                {
                    try
                    {
                        await collector.CollectAsync(stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
#pragma warning disable CA1031 // Collectors must never bring down the host: log and keep going.
                    catch (Exception ex)
#pragma warning restore CA1031
                    {
                        _logger.LogWarning(ex, "Failed to refresh metrics from {Collector}", collector.Name);
                    }
                }
            }
        }
        while (await SafeWaitAsync(timer, stoppingToken).ConfigureAwait(false));
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
