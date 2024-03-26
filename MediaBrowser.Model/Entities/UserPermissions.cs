namespace MediaBrowser.Model.Entities;

/// <summary>
/// Class to hold data on user permissions for lists.
/// </summary>
/// <param name="userId">The user id.</param>
/// <param name="canEdit">Edit permission.</param>
public class UserPermissions(string userId, bool canEdit = false)
{
    /// <summary>
    /// Gets or sets the user id.
    /// </summary>
    public string UserId { get; set; } = userId;

    /// <summary>
    /// Gets or sets a value indicating whether the user has edit permissions.
    /// </summary>
    public bool CanEdit { get; set; } = canEdit;
}
