using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Playlists;

/// <summary>
/// A playlist update request.
/// </summary>
public class PlaylistUpdateRequest
{
    /// <summary>
    /// Gets or sets the id of the playlist.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the id of the user updating the playlist.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the name of the playlist.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets item ids to add to the playlist.
    /// </summary>
    public IReadOnlyList<Guid>? Ids { get; set; }

    /// <summary>
    /// Gets or sets the playlist users.
    /// </summary>
    public IReadOnlyList<PlaylistUserPermissions>? Users { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the playlist is public.
    /// </summary>
    public bool? Public { get; set; }
}
