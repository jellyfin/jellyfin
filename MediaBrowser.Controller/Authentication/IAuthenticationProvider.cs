#pragma warning disable CS1591

using System;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller.Authentication
{
    public interface IAuthenticationProvider<T>
    {
        /// <summary>
        /// Gets the name of this authentication provider.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a value indicating whether or not this authentication method is globally enabled.
        /// </summary>
        [Obsolete(TODO)]
        bool IsEnabled { get => ; }

        /// <summary>
        /// Authenticates a user, with a given authentication request.
        /// </summary>
        /// <param name="user">A user. Can be null if the user's identity has yet to be determined, or if the user will be created during the authentication process.</param>
        /// <param name="authenticationRequest">Authentication data.</param>
        /// <returns>A valid, existing user if authentication was successful, or null if not.</returns>
        /// <remarks>An authentication provider is free to create a user during the authentication process,
        /// or return an entirely different user than the one provided as a parameter.</remarks>
        Task<User?> Authenticate(User? user, T authenticationRequest);

        [Obsolete("Deprecated. Implementors, do not implement. Callers, use `Authenticate(User user, T data)` instead.")]
        Task<ProviderAuthenticationResult> Authenticate(string username, string password)
        {
            throw new NotImplementedException();
        }

        UserPolicy? GetNewUserPolicy()
        {
            return null;
        }
    }

    [Obsolete("Deprecated. Please inherit the generic IAuthenticationProvider<T> instead, where T is the expected AuthenticationData type." +
        "IMPORTANT!!: If your authentication provider does not have a new user policy, make sure you DO NOT accidentally implement GetNewUserPolicy.")]
    public interface IAuthenticationProvider : IAuthenticationProvider<object>
    {
        [Obsolete("Deprecated, do not implement.")]
        bool HasPassword(User user);

        [Obsolete("Deprecated, do not implement.")]
        Task ChangePassword(User user, string newPassword);
    }

    [Obsolete("Deprecated. Callers should call `Authenticate(User user, T data)` instead. Authentication providers should not implement this interface anymore, and instead" +
        " implement `Authenticate(User user, T data)` directly through IAuthenticationProvider")]
    public interface IRequiresResolvedUser
    {
        Task<ProviderAuthenticationResult> Authenticate(string username, string password, User? resolvedUser);
    }

    [Obsolete("Deprecated. Implement GetNewUserPolicy() provided by IAuthenticationProvider<T> instead.")]

    public interface IHasNewUserPolicy
    {
        UserPolicy GetNewUserPolicy();
    }

    public class ProviderAuthenticationResult
    {
        public required string Username { get; set; }

        public string? DisplayName { get; set; }
    }
}
