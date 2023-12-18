using System;

namespace Jellyfin.Api.Models.SyncPlayDtos;

/// <summary>
/// Class ReadyRequest.
/// </summary>
public class ReadyRequestDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadyRequestDto"/> class.
    /// </summary>
    public ReadyRequestDto()
    {
        PlaylistItemId = Guid.Empty;
    }

    /// <summary>
    /// Gets or sets when the request has been made by the client.
    /// </summary>
    /// <value>The date of the request.</value>
    public DateTime When { get; set; }

    /// <summary>
    /// Gets or sets the position ticks.
    /// </summary>
    /// <value>The position ticks.</value>
    public long PositionTicks { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the client playback is unpaused.
    /// </summary>
    /// <value>The client playback status.</value>
    public bool IsPlaying { get; set; }

    /// <summary>
    /// Gets or sets the playlist item identifier of the playing item.
    /// </summary>
    /// <value>The playlist item identifier.</value>
    public Guid PlaylistItemId { get; set; }
}
