namespace MediaBrowser.Model.Authentication;

/// <summary>
/// Secret-safe OpenID Connect provider configuration DTO.
/// </summary>
public class OidcProviderConfigurationDto : OidcProviderConfigurationBase
{
    /// <summary>
    /// Gets or sets the redirect URI to register with the OpenID Connect provider.
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether a client secret is configured.
    /// </summary>
    public bool HasClientSecret { get; set; }
}
