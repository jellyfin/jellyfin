using System.Text.Json.Serialization;

namespace MediaBrowser.Model.Branding;

/// <summary>
/// The branding options.
/// </summary>
public class BrandingOptions
{
    /// <summary>
    /// Gets or sets the login disclaimer.
    /// </summary>
    /// <value>The login disclaimer.</value>
    public string? LoginDisclaimer { get; set; }

    /// <summary>
    /// Gets or sets the custom CSS.
    /// </summary>
    /// <value>The custom CSS.</value>
    public string? CustomCss { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable the splashscreen.
    /// </summary>
    public bool SplashscreenEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the splashscreen location on disk.
    /// </summary>
    /// <remarks>
    /// Not served via the API.
    /// Only used to save the custom uploaded user splashscreen in the configuration file.
    /// </remarks>
    [JsonIgnore]
    public string? SplashscreenLocation { get; set; }
}
