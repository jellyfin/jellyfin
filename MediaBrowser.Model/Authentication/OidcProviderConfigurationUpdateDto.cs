namespace MediaBrowser.Model.Authentication;

/// <summary>
/// OpenID Connect provider configuration update DTO.
/// </summary>
public class OidcProviderConfigurationUpdateDto : OidcProviderConfigurationBase
{
    /// <summary>
    /// Gets or sets the client secret. Null or empty values preserve the current stored secret.
    /// </summary>
    public string? ClientSecret { get; set; }
}
