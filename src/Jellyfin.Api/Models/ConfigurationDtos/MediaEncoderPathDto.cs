namespace Jellyfin.Api.Models.ConfigurationDtos;

/// <summary>
/// Media Encoder Path Dto.
/// </summary>
public class MediaEncoderPathDto
{
    /// <summary>
    /// Gets or sets media encoder path.
    /// </summary>
    public string Path { get; set; } = null!;

    /// <summary>
    /// Gets or sets media encoder path type.
    /// </summary>
    public string PathType { get; set; } = null!;
}
