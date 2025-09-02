using System;
using System.Diagnostics.CodeAnalysis;
using Jellyfin.Database.Implementations.Entities;

namespace MediaBrowser.Controller.Authentication
{
    /// <summary>
    /// Represents a response to an authentication attempt via <see cref="IAuthenticationProvider{TResponseC2S}"/> or <see cref="IUserAuthenticationManager"/>.
    /// </summary>
    public class AuthenticationResult
    {
        private AuthenticationResult()
        {
        }

        /// <summary>
        /// Gets the User returned identified by the authentication request. Note that this field being null does _NOT_ mean authentication was successful, per se, only that a User was identified.
        /// </summary>
        public User? User { get; private set; }

        /// <summary>
        /// Gets the optional error code returned from unsuccessful authentication.
        /// </summary>
        public int? ErrorCode { get; private set; }

        /// <summary>
        /// Gets optional additional error data returned from unsuccessful authentication.
        /// </summary>
        public string? ErrorData { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not authentication was successful.
        /// </summary>
        [MemberNotNullWhen(returnValue: true, nameof(User))]
        public bool Authenticated { get; private set; }

        /// <summary>
        /// Creates a new successful <see cref="AuthenticationResult"/> with the given user.
        /// </summary>
        /// <param name="user">The user that successfully authenticated.</param>
        /// <returns>The <see cref="AuthenticationResult"/> instance.</returns>
        public static AuthenticationResult Success(User user)
        {
            return new AuthenticationResult() { User = user, Authenticated = true };
        }

        /// <summary>
        /// Creates a new unsuccessful <see cref="AuthenticationResult"/> with the given user.
        /// </summary>
        /// <param name="user">The user that was identified but did not successfully authenticate.</param>
        /// <param name="errorCode">An optional user-defined error code to convey extra information to the caller.</param>
        /// <param name="errorData">Optional additional error data to convey extra information to the caller.</param>
        /// <returns>The <see cref="AuthenticationResult"/> instance.</returns>
        public static AuthenticationResult Failure(User user, int? errorCode = null, string? errorData = null)
        {
            return new AuthenticationResult() { User = user, Authenticated = false, ErrorCode = errorCode, ErrorData = errorData };
        }

        /// <summary>
        /// Creates a new unsuccessful <see cref="AuthenticationResult"/> without an identified user.
        /// If you know the user that was trying to log in, use <see cref="Failure"/> instead, since it is used for user-based rate limiting and blocking.
        /// </summary>
        /// <param name="errorCode">An optional user-defined error code to convey extra information to the caller.</param>
        /// <param name="errorData">Optional additional error data to convey extra information to the caller.</param>
        /// <returns>The <see cref="AuthenticationResult"/> instance.</returns>
        public static AuthenticationResult AnonymousFailure(int? errorCode = null, string? errorData = null)
        {
            return new AuthenticationResult() { ErrorCode = errorCode, Authenticated = false, ErrorData = errorData };
        }
    }
}
