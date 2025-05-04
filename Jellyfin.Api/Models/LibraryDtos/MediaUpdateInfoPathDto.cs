namespace Jellyfin.Api.Models.LibraryDtos;

/// <summary>
/// The media update info path.
/// </summary>
public class MediaUpdateInfoPathDto
{
    /// <summary>
    /// Gets or sets media path.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets media update type.
    /// Created, Modified, Deleted.
    /// </summary>
    public string? UpdateType { get; set; }
}
