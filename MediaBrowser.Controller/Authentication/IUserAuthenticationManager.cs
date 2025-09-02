using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Microsoft.Extensions.DependencyInjection;

namespace MediaBrowser.Controller.Authentication
{
    /// <summary>
    /// Handles authentication of users. If you simply want to add a new authentication provider,
    /// you probably want to implement <see cref="IAuthenticationProvider"/> or one of the helper
    /// classes <see cref="AbstractAuthenticationProvider{TResponseC2S, TGlobalData, TUserData}"/> or its inheritors./>.
    /// </summary>
    public interface IUserAuthenticationManager
    {
        /// <summary>
        /// Performs an authentication attempt, with optional payload data. Will use the last registered authentication provider that
        /// matches the <typeparamref name="TResponseC2S"/> and optional <paramref name="authenticationTypeFilter"/> filter.
        /// </summary>
        /// <param name="authenticationData">Authentication data.</param>
        /// <param name="remoteEndpoint">The remote endpoint, if known.</param>
        /// <param name="authenticationTypeFilter">An optional authentication type filter. Mainly useful when the payload data type alone is not enough to resolve an authentication provider, like with externally triggered authentication providers that don't take payload data at all.</param>
        /// <typeparam name="TResponseC2S">The payload data.</typeparam>
        /// <returns>A tuple containing the <see cref="IAuthenticationProvider{TResponseC2S}"/> that responded, and an optional User (if the authentication was successful).
        /// </returns>
        /// <exception cref="NotImplementedException">When there is no registered authentication provider for the given TResponseC2S.</exception>
        async Task<(IAuthenticationProvider<TResponseC2S> Provider, AuthenticationResult Result)> Authenticate<TResponseC2S>(TResponseC2S authenticationData, string? remoteEndpoint, string? authenticationTypeFilter = null)
            where TResponseC2S : struct
        {
            var provider = await ResolveProvider<TResponseC2S>(authenticationTypeFilter).ConfigureAwait(false)
                ?? throw new NotImplementedException("Attempted authentication using '" + typeof(TResponseC2S).Name + "', but found no registered provider that can handle it.");

            return (provider, await provider.Authenticate(authenticationData).ConfigureAwait(false));
        }

        /// <summary>
        /// Registers a provider.
        /// </summary>
        /// <param name="providers">The providers.</param>
        /// <returns>A void Task.</returns>
        Task RegisterProviders(IEnumerable<object> providers);

        /// <summary>
        /// Finds an _enabled_ authentication provider that matches the <typeparamref name="TResponseC2S"/> and optional <paramref name="authenticationTypeFilter"/> filter.
        /// </summary>
        /// <param name="authenticationTypeFilter">An optional authentication type filter. Mainly useful when the payload data type alone is not enough to resolve an authentication provider, like with externally triggered authentication providers that don't take payload data at all.</param>
        /// <typeparam name="TResponseC2S">The payload data.</typeparam>
        /// <returns>The last registered authentication provider that can handle <typeparamref name="TResponseC2S"/>.</returns>
        Task<IAuthenticationProvider<TResponseC2S>?> ResolveProvider<TResponseC2S>(string? authenticationTypeFilter = null)
            where TResponseC2S : struct;

        /// <summary>
        /// Resolves an authentication provider by its concrete implementation type, only if it is enabled.
        /// </summary>
        /// <typeparam name="T">The implementation type to resolve.</typeparam>
        /// <returns>The authentication provider, if found.</returns>
        Task<T?> ResolveConcrete<T>()
            where T : class;

        /// <summary>
        /// Get the enabled authentication providers.
        /// </summary>
        /// <remarks>
        /// This API will change in the future to include more information and configuration options.
        /// It should be the basis for customisation done through the admin panel.
        /// Right now, only a global enable/disable is exposed, and some hardcoded actions for the default
        /// authentication provider are implemented.
        /// </remarks>
        /// <returns>The enabled authentication providers.</returns>
        [Experimental(diagnosticId: "AuthenticationManager_GetAuthenticationProviders")]
        Task<IEnumerable<NameIdPair>> GetAuthenticationProviders();
    }
}
