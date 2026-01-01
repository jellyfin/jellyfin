namespace Jellyfin.Api.Models.SyncPlayDtos;

/// <summary>
/// Class SetPlaybackSpeedRequestDto.
/// </summary>
public class SetPlaybackSpeedRequestDto
{
    /// <summary>
    /// Gets or sets the playback speed.
    /// </summary>
    /// <value>The playback speed.</value>
    public double PlaybackSpeed { get; set; } = 1.0;
}
