namespace Jellyfin.Api.Models.PlaylistDtos;

/// <summary>
/// Share link DTO.
/// </summary>
public class ShareLinkDto
{
    /// <summary>
    /// Gets or sets the share token.
    /// </summary>
    public string? ShareToken { get; set; }

    /// <summary>
    /// Gets or sets the share link URL.
    /// </summary>
    public string? ShareLink { get; set; }
}
