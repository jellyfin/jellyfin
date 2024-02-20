using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Model.Lyrics;

/// <summary>
/// Upload lyric dto.
/// </summary>
public class UploadLyricDto
{
    /// <summary>
    /// Gets or sets the lyrics file.
    /// </summary>
    [Required]
    public IFormFile Lyrics { get; set; } = null!;
}
