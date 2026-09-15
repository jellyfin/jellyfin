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

    /// <summary>
    /// Gets or sets a value indicating whether the saved configuration differs from the active startup configuration.
    /// </summary>
    public bool RequiresRestart { get; set; }
}
