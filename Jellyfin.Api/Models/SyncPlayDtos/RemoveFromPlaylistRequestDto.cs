using System;
using System.Collections.Generic;

namespace Jellyfin.Api.Models.SyncPlayDtos;

/// <summary>
/// Class RemoveFromPlaylistRequestDto.
/// </summary>
public class RemoveFromPlaylistRequestDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveFromPlaylistRequestDto"/> class.
    /// </summary>
    public RemoveFromPlaylistRequestDto()
    {
        PlaylistItemIds = Array.Empty<Guid>();
    }

    /// <summary>
    /// Gets or sets the playlist identifiers of the items. Ignored when clearing the playlist.
    /// </summary>
    /// <value>The playlist identifiers of the items.</value>
    public IReadOnlyList<Guid> PlaylistItemIds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the entire playlist should be cleared.
    /// </summary>
    /// <value>Whether the entire playlist should be cleared.</value>
    public bool ClearPlaylist { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the playing item should be removed as well. Used only when clearing the playlist.
    /// </summary>
    /// <value>Whether the playing item should be removed as well.</value>
    public bool ClearPlayingItem { get; set; }
}
