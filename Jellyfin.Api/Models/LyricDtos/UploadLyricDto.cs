using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.LyricDtos;

/// <summary>
/// Upload lyric dto.
/// </summary>
public class UploadLyricDto
{
    /// <summary>
    /// Gets or sets the subtitle format.
    /// </summary>
    [Required]
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the lyric is synced.
    /// </summary>
    [Required]
    public bool IsSynced { get; set; }

    /// <summary>
    /// Gets or sets the subtitle data, Base64 encoded.
    /// </summary>
    [Required]
    public string Data { get; set; } = string.Empty;
}
