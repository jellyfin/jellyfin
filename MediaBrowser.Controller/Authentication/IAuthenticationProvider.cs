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
    /// <typeparam name="TPayloadData">The payload data that authenticates a user. This type is used as a key for signalling if an authentication provider can handle a specific type of authentication data.</typeparam>
    /// <remarks>On the back-end, an authentication step can be abstracted into the following high-level 2-step process:
    /// - Step 1: Challenge: The client requests a challenge from the server, optionally sending some data (TChallengeC2S), the server might perform some actions, and
    /// optionally returns some data (TChallengeS2C).
    /// - Step 2: Response: The client sends a response to the challenge, optionally sending some data (TResponseC2S), the server responds with either denial, or approval, in
    /// which case it returns the User that has just been authenticated.
    ///
    /// Below are some concrete examples of this flow.
    ///
    /// Password-based - challenge/response:
    /// - Challenge: The client requests to log in to an account based on its username (TChallengeC2S is Username), the server returns nothing (TChallengeS2C is null) so
    /// as to not disclose whether or not a given user exists.
    /// - Response: The client sends the password to the server (TResponseC2S is Password). If the User did not exist, it denies the request. If it does exist, it verifies
    /// the provided password against the user's stored password hash, and returns the User if it is valid, or denies the request if the password is invalid.
    ///
    /// Email magic-link sign in:
    /// - Challenge: The client requests to log in to an account based on its username (TChallengeC2S is Username), the server returns nothing (TChallengeS2C is null),
    /// but internally, it looks up if a user by the given username exists, and if it does, it generates an EmailToken, stores it along with an expiry time,
    /// and sends an email with this EmailToken to the user's stored e-mail address.
    /// - Response: If all goes well, the client has received a nice e-mail with a link that embeds a single-use token into it somehow. After clicking, they arrive on a webpage
    /// which then sends this token to the server (TResponseC2S is EmailToken). The server checks if the token exists and is still valid, and either denies or approves
    /// the request.
    ///
    /// OAuth/SSO:
    /// - Challenge: The client requests to log in without sending any data (TChallengeC2S is null), the server returns a redirect url (TChallengeS2C is RedirectURL).
    /// - Response: After the client has performed an external authorization flow, they get redirected to a webpage which sends an authorization code to the server
    /// (TResponseC2S is OAuthAuthorizationCode). The server tries to exchange this authorization code for an access token, and looks up the identity of the user
    /// by asking some trusted third party server. If successful, the authentication succeeds and the user is returned.
    ///
    /// Passkey authentication:
    /// - Challenge: The client requests to log in to an account based on its username (TChallengeC2S is Username), the server returns a passkey challenge (TChallengeS2C
    /// is PassKeyChallenge).
    /// - Response: After the client has locally signed the challenge, it sends the signed challenge to the server (TResponseC2S is SignedPasskeyChallenge). The server
    /// verifies the signed challenge, and if all is well, returns the User.
    ///
    /// TOTP:
    /// - Challenge: The client requests to log in to an account based on its username (TChallengeC2S is Username), the server returns nothing (TChallengeS2C is null) so as to
    /// not disclose whether or not a given user exists.
    /// - Response: The client sends their TOTP token to the server (TResponseC2S is TOTP). If the User did not exist, it denies the request. If it does exist, it verifies
    /// the provided TOTP using the user's stored TOTP secret, and returns the User if it is valid, or denies the request if it is invalid.
    ///
    /// Quick Connect - naive:
    /// - Challenge: The client requests to log in without sending any data (TChallengeC2S is null). The server returns a quick connect code (TChallengeS2C is QuickConnectCode),
    /// and internally stores this code, along with an expiry time.
    /// - Response: After the user fills in their Quick Connect code on a _different device_, they signal this to the server but don't have any data to send. TResponseC2S is null.
    /// The server checks if the quick connect code has indeed been filled in by a user within the validity period, and if so, it returns that user.
    ///
    /// External authenticator - approve/deny by clicking:
    /// - Challenge: The client requests to log in to an account based on its username (TChallengeC2S is Username). The server returns nothing (TChallengeS2C is null),
    /// but internally, it looks up if a user by the given username exists, and if it does, it notifies a third party server that this user is trying to log in.
    /// - Response: After the user approves or denies the authentication request, they signal this to the server but don't have any data to send. TResponseC2S is null.
    /// The server checks whether or not the authentication request has been approved, and based on this it either approves or denies the request.
    ///
    /// External authenticator - approve/deny by clicking a matching number:
    /// - Challenge: The client requests to log in to an account based on its username (TChallengeC2S is Username). The checks if a user by the given username exists,
    /// and if it does, it notifies a third party server that this user is trying to log in. This third party server responds with a number which the back-end server
    /// forwards to the client (TChallengeS2C is Number).
    /// - Response: After the user approves or denies the authentication request, they signal this to the server but don't have any data to send. TResponseC2S is null.
    /// The server checks whether or not the authentication request has been approved, and based on this it either approves or denies the request.
    ///
    /// After reading the above, there are a few things to note:
    /// The flow is implicitly stateful; it is assumed the Response is aware of the Challenge data, without this being explicitly mentioned.
    /// As you can see, there are some issues with the above:
    /// Password based you would like to do in one request, TOTP you would like to do in one request, Quick connect, External authenticator 1 and 2 you would like
    /// to not need polling unless absolutely necessary.
    ///
    /// Password-based - one-shot:
    /// - Challenge is skipped; TChallengeC2S and TChallengeS2C are both null.
    /// - Response: The client sends a username and password to the server (TResponseC2S is UsernamePassword). Internally, the server looks up if a user by the given username
    /// exists, and if it does, it verifies its password against the stored password hash, and returns the User if it is valid, or denies the request if the password is invalid.
    ///
    ///
    /// Besides being able to _handle_ a certain type of data, the data needs to come from somewhere. That is not the responsibility of an authentication provider. Jellyfin
    /// by default implements just 1 type of authentication, which is the classic password-based authentication, the payload for which gets passed in through the normal authentication
    /// flow. To support this, use <see cref="PasswordAuthData"/>. Other types will be implemented in the future.
    /// </remarks>
    public interface IAuthenticationProvider<TPayloadData>
        where TPayloadData : struct
    {
        /// <summary>
        /// Gets the name of this authentication provider.
        /// </summary>
        string Name { get; }

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

    public interface IAuthenticationProvider : IAuthenticationProvider<PasswordAuthData>
    {
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
