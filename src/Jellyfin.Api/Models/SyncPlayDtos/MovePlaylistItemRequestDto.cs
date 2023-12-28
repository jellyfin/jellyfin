using System;

namespace Jellyfin.Api.Models.SyncPlayDtos;

/// <summary>
/// Class MovePlaylistItemRequestDto.
/// </summary>
public class MovePlaylistItemRequestDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MovePlaylistItemRequestDto"/> class.
    /// </summary>
    public MovePlaylistItemRequestDto()
    {
        PlaylistItemId = Guid.Empty;
    }

    /// <summary>
    /// Gets or sets the playlist identifier of the item.
    /// </summary>
    /// <value>The playlist identifier of the item.</value>
    public Guid PlaylistItemId { get; set; }

    /// <summary>
    /// Gets or sets the new position.
    /// </summary>
    /// <value>The new position.</value>
    public int NewIndex { get; set; }
}
