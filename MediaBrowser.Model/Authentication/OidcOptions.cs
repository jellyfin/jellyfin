using System.Collections.Generic;

namespace MediaBrowser.Model.Authentication;

/// <summary>
/// OpenID Connect server authentication options.
/// </summary>
public class OidcOptions
{
    /// <summary>
    /// Gets or sets the configured OpenID Connect providers.
    /// </summary>
    public IReadOnlyList<OidcProviderOptions> Providers { get; set; } = new List<OidcProviderOptions>();
}
