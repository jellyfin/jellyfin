using System.ComponentModel.DataAnnotations;

namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// Upload lyric dto.
/// </summary>
public class UploadLyricDto
{
    /// <summary>
    /// Gets or sets the lyric format, typically the file extension.
    /// </summary>
    [Required]
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the lyric data, plaintext.
    /// </summary>
    [Required]
    public string Data { get; set; } = string.Empty;
}
