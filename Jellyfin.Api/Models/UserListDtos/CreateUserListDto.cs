namespace Jellyfin.Api.Models.UserListDtos;

/// <summary>
/// Creates a user list.
/// </summary>
public class CreateUserListDto
{
    /// <summary>
    /// Gets or sets the name of the new list.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether watched items are automatically removed.
    /// </summary>
    public bool AutoRemoveWatched { get; set; }
}
