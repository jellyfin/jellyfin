namespace Jellyfin.Api.Models.SyncPlayDtos;

/// <summary>
/// Class SeekRequestDto.
/// </summary>
public class SeekRequestDto
{
    /// <summary>
    /// Gets or sets the position ticks.
    /// </summary>
    /// <value>The position ticks.</value>
    public long PositionTicks { get; set; }
}
