namespace Jellyfin.Api.Models.PlaylistDtos;

/// <summary>
/// Update existing playlist user dto. Fields set to `null` will not be updated and keep their current values.
/// </summary>
public class UpdatePlaylistUserDto
{
    /// <summary>
    /// Gets or sets a value indicating whether the user can edit the playlist.
    /// </summary>
    public bool? CanEdit { get; set; }
}
