using System;

namespace Jellyfin.Api.Models.SyncPlayDtos;

/// <summary>
/// Class SetPlaylistItemRequestDto.
/// </summary>
public class SetPlaylistItemRequestDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetPlaylistItemRequestDto"/> class.
    /// </summary>
    public SetPlaylistItemRequestDto()
    {
        PlaylistItemId = Guid.Empty;
    }

    /// <summary>
    /// Gets or sets the playlist identifier of the playing item.
    /// </summary>
    /// <value>The playlist identifier of the playing item.</value>
    public Guid PlaylistItemId { get; set; }
}
