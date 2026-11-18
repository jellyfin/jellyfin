#nullable disable

using System;

namespace MediaBrowser.Model.Session;

/// <summary>
/// An item in a play queue.
/// </summary>
public record QueueItem
{
    /// <summary>
    /// Gets or sets the item id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the playlist item id.
    /// </summary>
    public string PlaylistItemId { get; set; }
}
