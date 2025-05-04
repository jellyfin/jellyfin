using System.ComponentModel.DataAnnotations;
using MediaBrowser.Model.Configuration;

namespace Jellyfin.Api.Models.LibraryStructureDto;

/// <summary>
/// Update library options dto.
/// </summary>
public class UpdateMediaPathRequestDto
{
    /// <summary>
    /// Gets or sets the library name.
    /// </summary>
    [Required]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets library folder path information.
    /// </summary>
    [Required]
    public MediaPathInfo PathInfo { get; set; } = null!;
}
