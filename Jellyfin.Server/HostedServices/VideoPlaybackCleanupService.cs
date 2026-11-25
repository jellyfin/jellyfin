using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Server.HostedServices;

/// <summary>
/// Removes abandoned playback admissions and their retired encoding identifiers.
/// </summary>
public sealed class VideoPlaybackCleanupService : BackgroundService
{
    private readonly IVideoPlaybackCoordinator _coordinator;
    private readonly ITranscodeManager? _transcodes;
    private readonly ILiveStreamPlaybackManager? _liveStreams;
    private readonly ILogger<VideoPlaybackCleanupService> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlaybackCleanupService"/> class.
    /// </summary>
    /// <param name="coordinator">The shared playback registry.</param>
    public VideoPlaybackCleanupService(IVideoPlaybackCoordinator coordinator)
        : this(coordinator, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlaybackCleanupService"/> class.
    /// </summary>
    /// <param name="coordinator">The shared playback registry.</param>
    /// <param name="transcodes">The server's encoding jobs.</param>
    /// <param name="liveStreams">The ownership of opened input consumers.</param>
    public VideoPlaybackCleanupService(IVideoPlaybackCoordinator coordinator, ITranscodeManager? transcodes, ILiveStreamPlaybackManager? liveStreams)
        : this(coordinator, transcodes, liveStreams, NullLogger<VideoPlaybackCleanupService>.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlaybackCleanupService"/> class.
    /// </summary>
    /// <param name="coordinator">The shared playback registry.</param>
    /// <param name="transcodes">The server's encoding jobs.</param>
    /// <param name="liveStreams">The ownership of opened input consumers.</param>
    /// <param name="logger">The cleanup failure logger.</param>
    /// <param name="timeProvider">The timer clock, or the system clock.</param>
    public VideoPlaybackCleanupService(
        IVideoPlaybackCoordinator coordinator,
        ITranscodeManager? transcodes,
        ILiveStreamPlaybackManager? liveStreams,
        ILogger<VideoPlaybackCleanupService> logger,
        TimeProvider? timeProvider = null)
    {
        _coordinator = coordinator;
        _transcodes = transcodes;
        _liveStreams = liveStreams;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30), _timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    var jobs = _transcodes?.GetActiveTranscodingJobs() ?? Array.Empty<TranscodingJob>();
                    var protectedAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (_liveStreams is not null)
                    {
                        protectedAliases.UnionWith(_liveStreams.GetProtectedPlaySessions());
                    }

                    foreach (var job in jobs)
                    {
                        if (!string.IsNullOrEmpty(job.PlaySessionId))
                        {
                            protectedAliases.Add(job.PlaySessionId);
                        }
                    }

                    _coordinator.RemoveExpired(protectedAliases);
                    if (_liveStreams is not null)
                    {
                        await _liveStreams.RemoveExpiredAsync(jobs).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to clean up expired video playback resources");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal server shutdown.
        }
    }
}
