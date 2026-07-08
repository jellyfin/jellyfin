using System.Collections.Generic;

namespace MediaBrowser.Model.Authentication;

/// <summary>
/// Secret-safe OpenID Connect configuration DTO.
/// </summary>
public class OidcConfigurationDto
{
    /// <summary>
    /// Gets or sets the configured providers.
    /// </summary>
    public IReadOnlyList<OidcProviderConfigurationDto> Providers { get; set; } = new List<OidcProviderConfigurationDto>();
}
