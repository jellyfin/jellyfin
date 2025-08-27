using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;

namespace MediaBrowser.Controller.Authentication
{
    /// <summary>
    /// Handles authentication of users. If you simply want to add a new authentication provider,
    /// you probably want to implement <see cref="IAuthenticationProvider"/> or one of the helper
    /// classes <see cref="AbstractAuthenticationProvider{TData, TPrivateAttemptData, TIntermediateAttemptData, TPersistentUserData}"/> or <see cref="AbstractSimpleAuthenticationProvider{TData, TPersistentUserData}"/>.
    /// </summary>
    public interface IUserAuthenticationManager
    {
        /// <summary>
        /// Performs an authentication attempt, with optional user and payload data.
        /// </summary>
        /// <param name="user">A user. Can be null if the user's identity has yet to be determined, or if the user will be created during the authentication process.</param>
        /// <param name="payloadData">Authentication data.</param>
        /// <typeparam name="TPayload">The payload data.</typeparam>
        /// <returns>Null if the authentication request was unsuccessful,
        /// otherwise a tuple containing the <see cref="IAuthenticationProvider"/> that responded, and the response itself.</returns>
        Task<(IAuthenticationProvider<TPayload> Provider, AuthenticationResponse Response)?> Authenticate<TPayload>(User? user, TPayload payloadData)
            where TPayload : struct;
    }
}
