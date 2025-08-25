#pragma warning disable CS1591

using System;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller.Authentication
{
    /// <summary>
    /// Interface for authentication providers. Custom authentication providers should generally inherit
    /// from <see cref="AbstractAuthenticationProvider{TData, TPrivateAttemptData, TPublicAttemptData, TPersistentUserData}"/> instead.
    /// </summary>
    public interface IAuthenticationProvider
    {
        /// <summary>
        /// Gets the name of this authentication provider.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a value indicating the interval in which this authentication method wants to provide its public data
        /// via <see cref="GetPublicAttemptData(Guid)"/>, in seconds. Returning 0 disables its public data reporting.
        /// </summary>
        /// <remarks>
        /// Public data is mostly useful for time-based 
        /// user intervention, such as Quick Connect, e-mail magic link sign-in or TOTP 2fa.
        /// </remarks>
        int RefreshInterval { get => 0; }

        [Obsolete("Deprecated. Authentication providers' enabled statuses are managed by IUserManager.")]
        bool IsEnabled { get; }

        [Obsolete("Deprecated, do not implement.")]
        bool HasPassword(User user);

        [Obsolete("Deprecated, do not implement.")]
        Task ChangePassword(User user, string newPassword);

        // TODO: remove default `Authenticate` implementation in IAuthenticationProvider<T> and update its xmldoc remark when removing this method.
        [Obsolete("Deprecated. Implementors, do not implement. Callers, use `Authenticate(User user, T data)` instead.")]
        Task<ProviderAuthenticationResult> Authenticate(string username, string password);

        /// <summary>
        /// Attempts to authenticate a user, with given attempt ID and optional user and data.
        /// </summary>
        /// <param name="attemptId">A unique id for this authentication attempt, which is used for progress reporting by <see cref="GetPublicAttemptData(Guid)"/>. Not a user ID or normal session id.</param>
        /// <param name="user">A user. Can be null if the user's identity has yet to be determined, or if the user will be created during the authentication process.</param>
        /// <param name="authenticationData">Authentication data.</param>
        /// <returns>A User if authentication was successful, or null if not.</returns>
        /// <remarks>
        /// Note to callers: An authentication provider is free to return a non existing user, return an existing user with modifications,
        /// or return an entirely different user than the one provided as a parameter. You are expected to take care of persisting these changes.
        /// Note to implementors: This function should have no persistent side effects. If you want to update the user, simply return the modified user.
        /// Users are indexed by their Id, not their name, so you can also change their Username property. If you want to create a new user,
        /// simply return the new user. Callers are expected to deal with persistence of this data.
        /// Despite the default interface implementation, all new IAuthenticationProviders should implement this method.
        /// </remarks>
        async Task<User?> Authenticate(Guid attemptId, User? user, dynamic? authenticationData)
        {
            // NOTE: this default implementation only exists for backwards compatibility with old IAuthenticationProviders,
            // all new IAuthenticationProviders should implement this method themselves (and probably derive from AbstractAuthenticationProvider instead).
            // TODO: Remove this implementation and update xmldoc remark when old IAuthenticationProviders are no longer supported.

#pragma warning disable CS0618 // Type or member is obsolete
            if (authenticationData is not UsernamePasswordData authData) // nor using a null authenticationRequest
            {
                throw new ArgumentNullException(nameof(authenticationData));
            }

            ArgumentNullException.ThrowIfNull(user); // we don't allow new code to call this method on old authenticationproviders without a resolved user

            if (authData.Username != user.Username)
            {
                throw new InvalidOperationException("Resolved user's username does not match login username in `IAuthenticationProvider#Authenticate` fallback.");
            }

            try
            {
                ProviderAuthenticationResult authenticationResult = this is IRequiresResolvedUser requiresResolvedUser
                ? await requiresResolvedUser.Authenticate(authData.Username, authData.Password, user).ConfigureAwait(false)
                : await this.Authenticate(authData.Username, authData.Password).ConfigureAwait(false);

                // I don't think we should accept this in this case, we don't know how the public API is being (ab)used at the moment and this
                // is a critical security check. Failing here is probably fine and a signal for authentication providers to update their logic.
                if (authenticationResult.Username != authData.Username)
                {
                    Console.WriteLine("Authentication provider provided different username {0}", authenticationResult.Username);
                    return null;
                }

                return user;
            }
            catch (AuthenticationException)
            {
                Console.WriteLine("Error authenticating with provider {0}", this.Name);
                return null;
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Get the latest public attempt data on a given authentication session.
        /// </summary>
        /// <param name="attemptId">The attempt ID.</param>
        /// <returns>The latest public attempt data on the authentication session.</returns>
        Task<dynamic?> GetPublicAttemptData(Guid attemptId)
        {
            return Task.FromResult<dynamic?>(null);
        }
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
