using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Prometheus;

namespace Jellyfin.Server.Implementations.Metrics;

/// <summary>
/// Exposes Prometheus metrics describing the live sessions and streams handled by the server.
/// </summary>
public sealed class SessionMetrics : IMetricsCollector
{
    private static readonly Gauge _sessions = Prometheus.Metrics
        .CreateGauge("jellyfin_sessions", "Number of Jellyfin sessions grouped by playback state.", "state");

    private static readonly Gauge _sessionsByClient = Prometheus.Metrics
        .CreateGauge("jellyfin_sessions_clients", "Number of Jellyfin sessions grouped by reported client name.", "client");

    private static readonly Gauge _streams = Prometheus.Metrics
        .CreateGauge("jellyfin_streams", "Number of currently active streams grouped by play method.", "play_method");

    private readonly ISessionManager _sessionManager;

    private readonly HashSet<string> _seenClients = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionMetrics"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager.</param>
    public SessionMetrics(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    /// <inheritdoc />
    public string Name => nameof(SessionMetrics);

    /// <inheritdoc />
    public Task CollectAsync(CancellationToken cancellationToken)
    {
        var sessions = _sessionManager.Sessions.ToList();

        var playing = sessions.Count(s => s.NowPlayingItem is not null);
        _sessions.WithLabels("playing").Set(playing);
        _sessions.WithLabels("idle").Set(sessions.Count - playing);

        var clientCounts = sessions
            .GroupBy(s => string.IsNullOrEmpty(s.Client) ? "Unknown" : s.Client)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        // Zero out clients seen previously but absent now so gauges do not stick at stale values.
        foreach (var oldClient in _seenClients)
        {
            if (!clientCounts.ContainsKey(oldClient))
            {
                _sessionsByClient.WithLabels(oldClient).Set(0);
            }
        }

        foreach (var (client, count) in clientCounts)
        {
            _sessionsByClient.WithLabels(client).Set(count);
            _seenClients.Add(client);
        }

        var transcodes = sessions.Count(s => s.TranscodingInfo is not null);
        var directPlays = sessions.Count(s => s.PlayState?.PlayMethod == PlayMethod.DirectPlay);
        var directStreams = sessions.Count(s => s.PlayState?.PlayMethod == PlayMethod.DirectStream);

        _streams.WithLabels(nameof(PlayMethod.DirectPlay)).Set(directPlays);
        _streams.WithLabels(nameof(PlayMethod.DirectStream)).Set(directStreams);
        _streams.WithLabels(nameof(PlayMethod.Transcode)).Set(transcodes);

        return Task.CompletedTask;
    }
}
