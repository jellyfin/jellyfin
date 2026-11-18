using System.Collections.Generic;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Persistence;

/// <summary>
/// Result of a batched NextUp query for a single series.
/// </summary>
public sealed class NextUpEpisodeBatchResult
{
    /// <summary>
    /// Gets or sets the last watched episode (highest season/episode that is played).
    /// </summary>
    public BaseItem? LastWatched { get; set; }

    /// <summary>
    /// Gets or sets the next unwatched episode after the last watched position.
    /// </summary>
    public BaseItem? NextUp { get; set; }

    /// <summary>
    /// Gets or sets specials that may air between episodes.
    /// Only populated when includeSpecials is true.
    /// </summary>
    public IReadOnlyList<BaseItem>? Specials { get; set; }

    /// <summary>
    /// Gets or sets the last watched episode for rewatching mode (most recently played).
    /// Only populated when includeWatchedForRewatching is true.
    /// </summary>
    public BaseItem? LastWatchedForRewatching { get; set; }

    /// <summary>
    /// Gets or sets the next played episode for rewatching mode.
    /// Only populated when includeWatchedForRewatching is true.
    /// </summary>
    public BaseItem? NextPlayedForRewatching { get; set; }
}
