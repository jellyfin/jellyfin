using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Playlists;

/// <summary>
/// A playlist creation request.
/// </summary>
public class PlaylistCreationRequest
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the list of items.
    /// </summary>
    public IReadOnlyList<Guid> ItemIdList { get; set; } = Array.Empty<Guid>();

    /// <summary>
    /// Gets or sets the media type.
    /// </summary>
    public MediaType? MediaType { get; set; }

    /// <summary>
    /// Gets or sets the user id.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the shares.
    /// </summary>
    public Share[]? Shares { get; set; }
}
