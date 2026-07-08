namespace MediaBrowser.Model.Authentication;

/// <summary>
/// Public OpenID Connect provider metadata.
/// </summary>
public class OidcProviderInfo
{
    /// <summary>
    /// Gets or sets the provider identifier.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authority URL.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether device authorization flow is enabled.
    /// </summary>
    public bool DeviceAuthorizationEnabled { get; set; }
}
