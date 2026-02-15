using System.ComponentModel.DataAnnotations;
using MediaBrowser.Model.Configuration;

namespace Jellyfin.Api.Models.LibraryStructureDto;

/// <summary>
/// Media Path dto.
/// </summary>
public class MediaPathDto
{
    /// <summary>
    /// Gets or sets the name of the library.
    /// </summary>
    [Required]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the path to add.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the path info.
    /// </summary>
    public MediaPathInfo? PathInfo { get; set; }
}
