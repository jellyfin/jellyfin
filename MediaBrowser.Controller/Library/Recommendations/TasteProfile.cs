using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;

namespace MediaBrowser.Controller.Library.Recommendations;

/// <summary>
/// An immutable per-user, per-kind, per-parent taste profile built by aggregating
/// weighted metadata signals from the user's watched and favorited items.
/// </summary>
public sealed record TasteProfile(
    BaseItemKind Kind,
    DateTime ComputedAt,
    IReadOnlyDictionary<string, float> Genres,
    IReadOnlyDictionary<string, float> Tags,
    IReadOnlyDictionary<Guid, float> People,
    IReadOnlyDictionary<string, float> Studios,
    float TotalSignalMass)
{
    /// <summary>
    /// A cold-start placeholder (no history). Use when the user has watched/favorited nothing.
    /// </summary>
    /// <param name="kind">The kind of item for this empty taste profile.</param>
    /// <returns>An empty taste profile with zero signal mass.</returns>
    public static TasteProfile Empty(BaseItemKind kind) => new(
        kind,
        DateTime.UtcNow,
        new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<Guid, float>(),
        new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase),
        TotalSignalMass: 0);
}
