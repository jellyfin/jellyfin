using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using MediaBrowser.Common.Telemetry;

namespace MediaBrowser.Controller.Telemetry;

/// <summary>
/// Session instruments published on <see cref="JellyfinTelemetry.Meter"/>. These count every connected
/// client, whether or not it is playing anything.
/// </summary>
public static class SessionMetrics
{
    private const string ClientTag = TelemetryTags.ClientTag;

    private static readonly ConcurrentDictionary<string, string> _sessions = new(StringComparer.Ordinal);

    // Reported at zero once seen, so that a series ends instead of going stale.
    private static readonly ConcurrentDictionary<string, byte> _seenClients = new(StringComparer.Ordinal);

    private static readonly Counter<long> _sessionsStarted = JellyfinTelemetry.Meter.CreateCounter<long>(
        "jellyfin.sessions.started",
        "{session}",
        "Sessions established.");

    private static readonly Counter<long> _sessionsEnded = JellyfinTelemetry.Meter.CreateCounter<long>(
        "jellyfin.sessions.ended",
        "{session}",
        "Sessions closed.");

#pragma warning disable IDE0052 // Held so the gauge is not collected; its callback is the useful part.
    private static readonly ObservableGauge<int> _activeSessions = JellyfinTelemetry.Meter.CreateObservableGauge(
        "jellyfin.sessions.active",
        ObserveActiveSessions,
        "{session}",
        "Sessions currently established.");
#pragma warning restore IDE0052

    /// <summary>
    /// Records that a session was established.
    /// </summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="client">The name of the client application the session belongs to.</param>
    public static void OnSessionStarted(string? sessionId, string? client)
    {
        var clientName = TelemetryTags.Client(client);
        _seenClients.TryAdd(clientName, 0);

        if (!string.IsNullOrEmpty(sessionId))
        {
            _sessions[sessionId] = clientName;
        }

        _sessionsStarted.Add(1, new KeyValuePair<string, object?>(ClientTag, clientName));
    }

    /// <summary>
    /// Records that a session was closed.
    /// </summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="client">The name of the client application the session belongs to.</param>
    public static void OnSessionEnded(string? sessionId, string? client)
    {
        string? clientName = null;
        if (!string.IsNullOrEmpty(sessionId) && _sessions.TryRemove(sessionId, out var tracked))
        {
            clientName = tracked;
        }

        clientName ??= TelemetryTags.Client(client);
        _seenClients.TryAdd(clientName, 0);

        _sessionsEnded.Add(1, new KeyValuePair<string, object?>(ClientTag, clientName));
    }

    private static IEnumerable<Measurement<int>> ObserveActiveSessions()
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var client in _seenClients.Keys)
        {
            counts[client] = 0;
        }

        foreach (var client in _sessions.Values)
        {
            counts.TryGetValue(client, out var current);
            counts[client] = current + 1;
        }

        foreach (var (client, count) in counts)
        {
            yield return new Measurement<int>(count, new KeyValuePair<string, object?>(ClientTag, client));
        }
    }
}
