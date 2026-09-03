using System;
using System.Collections.Concurrent;

namespace MediaBrowser.Controller.Telemetry;

/// <summary>
/// Tag names and tag values shared by the metrics the server publishes.
/// </summary>
internal static class TelemetryTags
{
    /// <summary>
    /// The name of the tag carrying the client application a session belongs to.
    /// </summary>
    internal const string ClientTag = "jellyfin.client";

    /// <summary>
    /// The name of the tag carrying the kind of a library item.
    /// </summary>
    internal const string ItemKindTag = "jellyfin.item.kind";

    /// <summary>
    /// The value reported for a tag whose real value is not known.
    /// </summary>
    internal const string Unknown = "unknown";

    /// <summary>
    /// Client names are taken from a request header, so they are attacker controlled. Only the first
    /// <see cref="MaxTrackedClients"/> distinct names get a series of their own, everything after that
    /// collapses into <see cref="OtherClient"/>. That bounds both the memory held here and the number
    /// of series created in the backend.
    /// </summary>
    private const int MaxTrackedClients = 32;

    private const string OtherClient = "other";

    // Maps every casing of a client name onto the first one seen, so that a client naming itself
    // inconsistently does not end up with a series per casing.
    private static readonly ConcurrentDictionary<string, string> _knownClients = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns <paramref name="value"/>, or <see cref="Unknown"/> when it is empty.
    /// </summary>
    /// <param name="value">The tag value.</param>
    /// <returns>The value to report.</returns>
    internal static string Normalize(string? value) => string.IsNullOrEmpty(value) ? Unknown : value;

    /// <summary>
    /// Returns the value to report for a client name, keeping the number of distinct names bounded.
    /// </summary>
    /// <param name="client">The client name as the client reported it.</param>
    /// <returns>The value to report.</returns>
    internal static string Client(string? client)
    {
        if (string.IsNullOrEmpty(client))
        {
            return Unknown;
        }

        if (_knownClients.TryGetValue(client, out var known))
        {
            return known;
        }

        // Racing callers can push the set a few entries past the cap, which does not matter, the point
        // is that it stops growing.
        return _knownClients.Count >= MaxTrackedClients ? OtherClient : _knownClients.GetOrAdd(client, client);
    }
}
