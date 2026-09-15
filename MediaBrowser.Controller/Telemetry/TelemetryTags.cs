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
    /// The value reported once a bounded tag has seen more distinct values than it tracks.
    /// </summary>
    private const string Other = "other";

    /// <summary>
    /// Client names are taken from a request header, so they are attacker controlled.
    /// </summary>
    private static readonly BoundedTagValues _clients = new(32);

    /// <summary>
    /// Provider names come from installed plugins rather than from a request, so they are bounded in
    /// practice. Capped anyway, so that a plugin generating names per item cannot create series without
    /// limit.
    /// </summary>
    private static readonly BoundedTagValues _providers = new(64);

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
    internal static string Client(string? client) => _clients.Get(client);

    /// <summary>
    /// Returns the value to report for a provider name, keeping the number of distinct names bounded.
    /// </summary>
    /// <param name="provider">The name of the provider.</param>
    /// <returns>The value to report.</returns>
    internal static string Provider(string? provider) => _providers.Get(provider);

    /// <summary>
    /// Maps arbitrary strings onto a bounded set of tag values, so that a caller supplying names
    /// without limit cannot create series without limit. Every casing of a name maps onto the first
    /// casing seen, so that a caller naming itself inconsistently does not get a series per casing.
    /// </summary>
    private sealed class BoundedTagValues
    {
        private readonly ConcurrentDictionary<string, string> _known = new(StringComparer.OrdinalIgnoreCase);
        private readonly int _limit;

        internal BoundedTagValues(int limit) => _limit = limit;

        internal string Get(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Unknown;
            }

            if (_known.TryGetValue(value, out var known))
            {
                return known;
            }

            // Racing callers can push the set a few entries past the cap, which does not matter, the
            // point is that it stops growing.
            return _known.Count >= _limit ? Other : _known.GetOrAdd(value, value);
        }
    }
}
