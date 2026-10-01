namespace MediaBrowser.Model.Configuration;

/// <summary>
/// The XBMC metadata options DTO for API use.
/// </summary>
public class XbmcMetadataOptionsDto
{
    /// <summary>
    /// Gets or sets the user id.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the release date format.
    /// </summary>
    public string ReleaseDateFormat { get; set; } = "yyyy-MM-dd";

    /// <summary>
    /// Gets or sets a value indicating whether to save image paths in NFO.
    /// </summary>
    public bool SaveImagePathsInNfo { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable path substitution.
    /// </summary>
    public bool EnablePathSubstitution { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable extra thumbs duplication.
    /// </summary>
    public bool EnableExtraThumbsDuplication { get; set; }
}
