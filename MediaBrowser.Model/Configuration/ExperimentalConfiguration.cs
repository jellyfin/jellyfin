namespace MediaBrowser.Model.Configuration;

/// <summary>
/// Experimental server configuration options.
/// </summary>
public class ExperimentalConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether plugin assets should require administrator access.
    /// </summary>
    public bool ElevatePluginRoutes { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether all configuration routes should require administrator access.
    /// </summary>
    public bool ElevateConfigurationRoutes { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether audio streams should require authentication.
    /// </summary>
    public bool AuthenticateAudioRoutes { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether video streams should require authentication.
    /// </summary>
    public bool AuthenticateVideoRoutes { get; set; } = false;
}
