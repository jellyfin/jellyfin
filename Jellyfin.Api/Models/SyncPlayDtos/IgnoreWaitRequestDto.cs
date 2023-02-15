namespace Jellyfin.Api.Models.SyncPlayDtos;

/// <summary>
/// Class IgnoreWaitRequestDto.
/// </summary>
public class IgnoreWaitRequestDto
{
    /// <summary>
    /// Gets or sets a value indicating whether the client should be ignored.
    /// </summary>
    /// <value>The client group-wait status.</value>
    public bool IgnoreWait { get; set; }
}
