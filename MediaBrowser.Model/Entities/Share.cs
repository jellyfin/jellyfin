namespace MediaBrowser.Model.Entities;

/// <summary>
/// Class to hold data on sharing permissions.
/// </summary>
public class Share
{
    /// <summary>
    /// Gets or sets the user id.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user has edit permissions.
    /// </summary>
    public bool CanEdit { get; set; }
}
