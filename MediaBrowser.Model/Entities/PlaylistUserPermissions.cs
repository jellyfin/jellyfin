using System;

namespace MediaBrowser.Model.Entities;

/// <summary>
/// Class to hold data on user permissions for playlists.
/// </summary>
public class PlaylistUserPermissions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistUserPermissions"/> class.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="canEdit">Edit permission.</param>
    public PlaylistUserPermissions(Guid userId, bool canEdit = false)
    {
        UserId = userId;
        CanEdit = canEdit;
    }

    /// <summary>
    /// Gets or sets the user id.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user has edit permissions.
    /// </summary>
    public bool CanEdit { get; set; }
}
