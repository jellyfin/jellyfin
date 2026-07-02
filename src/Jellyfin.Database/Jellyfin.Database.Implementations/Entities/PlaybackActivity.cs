using System;

namespace Jellyfin.Database.Implementations.Entities;

/// <summary>
/// Represents a single playback session record.
/// Data types based on the queries used on SessionManager.cs.
/// </summary>
public class PlaybackActivity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the item identifier.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the name of the item.
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the media type (Movie, Episode, Book, Music Video, etc).
    /// </summary>
    public string MediaType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the playback duration in ticks.
    /// </summary>
    public long PlayedTicks { get; set; }

    /// <summary>
    /// Gets or sets the date the playback occurred.
    /// </summary>
    public DateTime DatePlayed { get; set; }

    /// <summary>
    /// Gets or sets the subgroup of the item.
    /// </summary>
    public string ItemSubGroup { get; set; } = string.Empty;
}
