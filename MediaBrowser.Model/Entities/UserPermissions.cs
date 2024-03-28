namespace MediaBrowser.Model.Entities;

/// <summary>
/// Class to hold data on user permissions for lists.
/// </summary>
public class UserPermissions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserPermissions"/> class.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="canEdit">Edit permission.</param>
    public UserPermissions(string userId, bool canEdit = false)
    {
        UserId = userId;
        CanEdit = canEdit;
    }

    /// <summary>
    /// Gets or sets the user id.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user has edit permissions.
    /// </summary>
    public bool CanEdit { get; set; }
}
