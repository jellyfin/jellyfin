using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Helpers;

/// <summary>Starts cache recovery and expiry for the host, even when no download endpoint is visited.</summary>
public sealed class OfflineDownloadHostedService : IHostedService
{
    private readonly IServerConfigurationManager _configuration;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<OfflineDownloadHostedService> _logger;
    private OfflineDownloadJobs? _jobs;

    /// <summary>Initializes a new instance of the <see cref="OfflineDownloadHostedService"/> class.</summary>
    /// <param name="configuration">Existing application cache configuration.</param>
    /// <param name="lifetime">The server host lifetime.</param>
    /// <param name="logger">Private server diagnostics.</param>
    public OfflineDownloadHostedService(IServerConfigurationManager configuration, IHostApplicationLifetime lifetime, ILogger<OfflineDownloadHostedService> logger)
    {
        _configuration = configuration;
        _lifetime = lifetime;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            _jobs = OfflineDownloadRegistry.Get(_configuration, _lifetime, reason => _logger.LogWarning("Offline cache record rejected during recovery: {Reason}", reason));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // Offline cache failure must not stop ordinary Jellyfin playback or original downloads.
            _logger.LogError(error, "Offline cache maintenance is unavailable; check the configured cache location. Ordinary Jellyfin services remain enabled.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _jobs?.Dispose();
        return Task.CompletedTask;
    }
}
