using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Authentication;

namespace MediaBrowser.Controller.Authentication;

/// <summary>
/// Manages native OpenID Connect configuration.
/// </summary>
public interface IOidcConfigurationManager
{
    /// <summary>
    /// Gets the full configuration, including secrets.
    /// </summary>
    /// <returns>The OpenID Connect configuration.</returns>
    OidcOptions GetOptions();

    /// <summary>
    /// Gets secret-safe provider metadata for clients.
    /// </summary>
    /// <returns>The enabled provider information.</returns>
    IReadOnlyList<OidcProviderInfo> GetProviderInfos();

    /// <summary>
    /// Gets the secret-safe configuration.
    /// </summary>
    /// <returns>The secret-safe configuration.</returns>
    OidcConfigurationDto GetConfiguration();

    /// <summary>
    /// Updates the configuration.
    /// </summary>
    /// <param name="configuration">The update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the update.</returns>
    Task UpdateConfigurationAsync(OidcConfigurationUpdateDto configuration, CancellationToken cancellationToken);

    /// <summary>
    /// Gets an enabled provider by id.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <returns>The provider options, or <c>null</c>.</returns>
    OidcProviderOptions? GetEnabledProvider(string providerId);
}
