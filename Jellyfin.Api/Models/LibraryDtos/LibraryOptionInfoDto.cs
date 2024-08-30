namespace Jellyfin.Api.Models.LibraryDtos;

/// <summary>
/// Library option info dto.
/// </summary>
public class LibraryOptionInfoDto
{
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether default enabled.
    /// </summary>
    public bool DefaultEnabled { get; set; }
}
