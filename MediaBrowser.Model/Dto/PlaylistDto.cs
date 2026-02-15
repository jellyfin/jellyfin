using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Dto;

/// <summary>
/// DTO for playlists.
/// </summary>
public class PlaylistDto
{
    /// <summary>
    /// Gets or sets a value indicating whether the playlist is publicly readable.
    /// </summary>
    public bool OpenAccess { get; set; }

    /// <summary>
    /// Gets or sets the share permissions.
    /// </summary>
    public required IReadOnlyList<PlaylistUserPermissions> Shares { get; set; }

    /// <summary>
    /// Gets or sets the item ids.
    /// </summary>
    public required IReadOnlyList<Guid> ItemIds { get; set; }
}
