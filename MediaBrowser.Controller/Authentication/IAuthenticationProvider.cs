using System;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller.Authentication
{
    /// <summary>
    /// Interface for authentication providers. Custom authentication providers should generally inherit from
    /// <see cref="AbstractAuthenticationProvider{TResponseC2S, TGlobalData, TUserData}"/> or its subclasses instead, which contain a lot of convenience
    /// logic for authentication providers.
    /// </summary>
    /// <typeparam name="TResponseC2S">The payload data that authenticates a user. This type is used as a key for signalling if an authentication provider can handle a specific type of authentication data.</typeparam>
    /// <remarks>
    /// Besides being able to _handle_ a certain type of data, the data needs to come from somewhere. That is not the responsibility of an authentication provider. Jellyfin
    /// by default implements just 1 type of authentication, which is the classic password-based authentication, the payload for which gets passed in through the normal authentication
    /// flow. To support this, use <see cref="UsernamePasswordAuthData"/>. Other types will be implemented in the future.
    /// </remarks>
    public interface IAuthenticationProvider<TResponseC2S>
        where TResponseC2S : struct
    {
        /// <summary>
        /// Gets the display name of this authentication provider.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets an optional field that can be used for extra disambiguation between IAuthenticationProviders with the same TResponseC2S.
        /// </summary>
        string? AuthenticationType { get; }

        /// <summary>
        /// Attempts to authenticate a user.
        /// </summary>
        /// <param name="authenticationData">The authentication data.</param>
        /// <returns>An authentication result.</returns>
        Task<AuthenticationResult> Authenticate(TResponseC2S authenticationData);
    }

#pragma warning disable CS1591

    [Obsolete("Deprecated. New authentication providers should inherit from the generic IAuthenticationProvider<T> instead.")]
    public interface IAuthenticationProvider
    {
        string Name { get; }

        [Obsolete("Deprecated. Authentication providers' enabled statuses are managed by IUserAuthenticationManager.")]
        bool IsEnabled { get; }

        [Obsolete("Deprecated, do not implement.")]
        bool HasPassword(User user);

        [Obsolete("Deprecated, do not implement.")]
        Task ChangePassword(User user, string newPassword);

        [Obsolete("Deprecated. Implementors, do not implement. Callers, use `Authenticate(User user, T data)` instead.")]
        Task<ProviderAuthenticationResult> Authenticate(string username, string password);
    }

    [Obsolete("Deprecated. Callers should call `Authenticate(User user, T data)` instead. Authentication providers should not implement this interface anymore, and instead" +
        " implement `Authenticate(User user, T data)` directly through IAuthenticationProvider")]
    public interface IRequiresResolvedUser
    {
        [Obsolete("Deprecated. Callers should call `Authenticate(User user, T data)` instead. Authentication providers should not implement this interface anymore, and instead" +
        " implement `Authenticate(User user, T data)` directly through IAuthenticationProvider")]
        Task<ProviderAuthenticationResult> Authenticate(string username, string password, User? resolvedUser);
    }

    [Obsolete("Deprecated. Implement custom creation logic in IAuthenticationProvider<T>#Authenticate(User, T) instead.")]
    public interface IHasNewUserPolicy
    {
        [Obsolete("Deprecated. Implement custom creation logic in IAuthenticationProvider<T>#Authenticate(User, T) instead.")]
        UserPolicy GetNewUserPolicy();
    }

    [Obsolete("Deprecated. Use the new Authenticate(User? user, T authenticationRequest) methods on IAuthenticationProvider<T>.")]
    public class ProviderAuthenticationResult
    {
        public required string Username { get; set; }

        public string? DisplayName { get; set; }
    }
}
