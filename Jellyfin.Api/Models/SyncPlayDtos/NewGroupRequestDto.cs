namespace Jellyfin.Api.Models.SyncPlayDtos;

/// <summary>
/// Class NewGroupRequestDto.
/// </summary>
public class NewGroupRequestDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NewGroupRequestDto"/> class.
    /// </summary>
    public NewGroupRequestDto()
    {
        GroupName = string.Empty;
    }

    /// <summary>
    /// Gets or sets the group name.
    /// </summary>
    /// <value>The name of the new group.</value>
    public string GroupName { get; set; }
}
