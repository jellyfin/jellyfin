namespace Jellyfin.Api.Models.UserListDtos;

/// <summary>
/// Updates a user list. Properties set to <see langword="null"/> are left unchanged.
/// </summary>
public class UpdateUserListDto
{
    /// <summary>
    /// Gets or sets the new list name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the new zero-based list sort index.
    /// </summary>
    public int? SortIndex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether watched items are automatically removed.
    /// </summary>
    public bool? AutoRemoveWatched { get; set; }
}
