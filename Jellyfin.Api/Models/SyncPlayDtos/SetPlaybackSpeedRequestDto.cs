namespace Jellyfin.Api.Models.SyncPlayDtos;

/// <summary>
/// Class SetPlaybackSpeedRequestDto.
/// </summary>
public class SetPlaybackSpeedRequestDto
{
    /// <summary>
    /// Gets or sets the playback speed, should be between 0.25 and 5.
    /// </summary>
    /// <value>The playback speed.</value>
    public double PlaybackSpeed { get; set; } = 1.0;
}
