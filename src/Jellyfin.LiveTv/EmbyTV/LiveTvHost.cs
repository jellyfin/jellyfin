using System.Threading;
using System.Threading.Tasks;
using Jellyfin.LiveTv.Timers;
using MediaBrowser.Controller.LiveTv;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.LiveTv.EmbyTV;

/// <summary>
/// <see cref="IHostedService"/> responsible for initializing Live TV.
/// </summary>
public sealed class LiveTvHost : IHostedService
{
    private readonly IRecordingsManager _recordingsManager;
    private readonly TimerManager _timerManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="LiveTvHost"/> class.
    /// </summary>
    /// <param name="recordingsManager">The <see cref="IRecordingsManager"/>.</param>
    /// <param name="timerManager">The <see cref="TimerManager"/>.</param>
    public LiveTvHost(IRecordingsManager recordingsManager, TimerManager timerManager)
    {
        _recordingsManager = recordingsManager;
        _timerManager = timerManager;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timerManager.RestartTimers();
        return _recordingsManager.CreateRecordingFolders();
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
