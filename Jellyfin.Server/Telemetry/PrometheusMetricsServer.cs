using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Jellyfin.Server.Telemetry;

/// <summary>
/// Serves the prometheus metrics endpoint on a dedicated listener.
/// </summary>
internal sealed class PrometheusMetricsServer : IHostedService, IDisposable
{
    private readonly ILogger<PrometheusMetricsServer> _logger;
    private readonly IServerConfigurationManager _configurationManager;

    private IMetricServer? _metricServer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrometheusMetricsServer"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
    /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    public PrometheusMetricsServer(ILogger<PrometheusMetricsServer> logger, IServerConfigurationManager configurationManager)
    {
        _logger = logger;
        _configurationManager = configurationManager;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var configuration = _configurationManager.Configuration;
        var bindAddress = string.IsNullOrWhiteSpace(configuration.MetricsBindAddress)
            ? "127.0.0.1"
            : configuration.MetricsBindAddress;

        try
        {
            _metricServer = new KestrelMetricServer(bindAddress, configuration.MetricsPort).Start();
            _logger.LogInformation(
                "Prometheus metrics are served on http://{BindAddress}:{Port}/metrics",
                bindAddress,
                configuration.MetricsPort);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to start the prometheus metrics server on {BindAddress}:{Port}",
                bindAddress,
                configuration.MetricsPort);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_metricServer is not null)
        {
            await _metricServer.StopAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _metricServer?.Dispose();
        _metricServer = null;
    }
}
