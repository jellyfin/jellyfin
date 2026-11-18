using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Server.HostedServices;

/// <summary>
/// Removes abandoned playback admissions and their retired encoding identifiers.
/// </summary>
public sealed class VideoPlaybackCleanupService : BackgroundService
{
    private readonly IVideoPlaybackCoordinator _coordinator;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlaybackCleanupService"/> class.
    /// </summary>
    /// <param name="coordinator">The shared playback registry.</param>
    public VideoPlaybackCleanupService(IVideoPlaybackCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                _coordinator.RemoveExpired();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal server shutdown.
        }
    }
}
