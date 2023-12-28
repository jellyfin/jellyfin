namespace Jellyfin.Api.Models.EnvironmentDtos;

/// <summary>
/// Validate path object.
/// </summary>
public class ValidatePathDto
{
    /// <summary>
    /// Gets or sets a value indicating whether validate if path is writable.
    /// </summary>
    public bool ValidateWritable { get; set; }

    /// <summary>
    /// Gets or sets the path.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets is path file.
    /// </summary>
    public bool? IsFile { get; set; }
}
