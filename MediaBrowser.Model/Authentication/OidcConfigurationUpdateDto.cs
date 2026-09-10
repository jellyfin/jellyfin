using System.Collections.Generic;

namespace MediaBrowser.Model.Authentication;

/// <summary>
/// OpenID Connect configuration update DTO.
/// </summary>
public class OidcConfigurationUpdateDto
{
    /// <summary>
    /// Gets or sets the configured providers.
    /// </summary>
    public IReadOnlyList<OidcProviderConfigurationUpdateDto> Providers { get; set; } = new List<OidcProviderConfigurationUpdateDto>();
}
