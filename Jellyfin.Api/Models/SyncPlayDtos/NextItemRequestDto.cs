using System;

namespace Jellyfin.Api.Models.SyncPlayDtos;

/// <summary>
/// Class NextItemRequestDto.
/// </summary>
public class NextItemRequestDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NextItemRequestDto"/> class.
    /// </summary>
    public NextItemRequestDto()
    {
        PlaylistItemId = Guid.Empty;
    }

    /// <summary>
    /// Gets or sets the playing item identifier.
    /// </summary>
    /// <value>The playing item identifier.</value>
    public Guid PlaylistItemId { get; set; }
}
