namespace MediaBrowser.Model.Authentication;

/// <summary>
/// Result of updating OpenID Connect configuration.
/// </summary>
public class OidcConfigurationUpdateResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the server must restart before the update is active.
    /// </summary>
    public bool RequiresRestart { get; set; }
}
