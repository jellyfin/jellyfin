using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.PlaybackAccessDtos;

/// <summary>
/// Requests delegated access to an on-demand media source.
/// </summary>
public sealed class CreatePlaybackAccessDto
{
    /// <summary>
    /// Gets or sets the media source identifier.
    /// </summary>
    [Required]
    public required string MediaSourceId { get; set; }

    /// <summary>
    /// Gets or sets the renderer's device identifier.
    /// </summary>
    [Required]
    public required string DeviceId { get; set; }
}
