using MediaBrowser.Model.SyncPlay;

namespace Jellyfin.Api.Models.SyncPlayDtos;

/// <summary>
/// Class SetRepeatModeRequestDto.
/// </summary>
public class SetRepeatModeRequestDto
{
    /// <summary>
    /// Gets or sets the repeat mode.
    /// </summary>
    /// <value>The repeat mode.</value>
    public GroupRepeatMode Mode { get; set; }
}
