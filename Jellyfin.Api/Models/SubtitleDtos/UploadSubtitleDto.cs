using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.SubtitleDtos;

/// <summary>
/// Upload subtitles dto.
/// </summary>
public class UploadSubtitleDto
{
    /// <summary>
    /// Gets or sets the subtitle language.
    /// </summary>
    [Required]
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subtitle format.
    /// </summary>
    [Required]
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the subtitle is forced.
    /// </summary>
    [Required]
    public bool IsForced { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the subtitle is for hearing impaired.
    /// </summary>
    [Required]
    public bool IsHearingImpaired { get; set; }

    /// <summary>
    /// Gets or sets the subtitle data.
    /// </summary>
    [Required]
    public string Data { get; set; } = string.Empty;
}
