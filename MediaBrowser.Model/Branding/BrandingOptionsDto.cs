namespace MediaBrowser.Model.Branding;

/// <summary>
/// The branding options DTO for API use.
/// This DTO excludes SplashscreenLocation to prevent it from being updated via API.
/// </summary>
public class BrandingOptionsDto
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
}
