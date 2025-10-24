using System;

namespace MediaBrowser.Model.Dto;

/// <summary>
/// Enhanced SyncPlay queue item that includes full BaseItemDto details.
/// </summary>
public class SyncPlayQueueItemDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayQueueItemDto"/> class.
    /// </summary>
    /// <param name="itemId">The item identifier.</param>
    /// <param name="playlistItemId">The playlist item identifier.</param>
    /// <param name="item">The full item details.</param>
    public SyncPlayQueueItemDto(Guid itemId, Guid playlistItemId, BaseItemDto? item)
    {
        ItemId = itemId;
        PlaylistItemId = playlistItemId;
        Item = item;
    }

    /// <summary>
    /// Gets the item identifier.
    /// </summary>
    /// <value>The item identifier.</value>
    public Guid ItemId { get; }

    /// <summary>
    /// Gets the playlist identifier of the item.
    /// </summary>
    /// <value>The playlist identifier of the item.</value>
    public Guid PlaylistItemId { get; }

    /// <summary>
    /// Gets the full item details.
    /// </summary>
    /// <value>The full item details.</value>
    public BaseItemDto? Item { get; }
}
