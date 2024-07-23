using System;

namespace MediaBrowser.Model.Playlists;

/// <summary>
/// A playlist user update request.
/// </summary>
public class PlaylistUserUpdateRequest
{
    /// <summary>
    /// Gets or sets the id of the playlist.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the id of the updated user.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user can edit the playlist.
    /// </summary>
    public bool? CanEdit { get; set; }
}
