using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Connect;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Connect
{
    public interface IConnectManager
    {
        /// <summary>
        /// Gets the wan API address.
        /// </summary>
        /// <value>The wan API address.</value>
        string WanApiAddress { get; }

        /// <summary>
        /// Links the user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="connectUsername">The connect username.</param>
        /// <returns>Task.</returns>
        Task<UserLinkResult> LinkUser(string userId, string connectUsername);

        /// <summary>
        /// Removes the link.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>Task.</returns>
        Task RemoveConnect(string userId);

        /// <summary>
        /// Invites the user.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task&lt;UserLinkResult&gt;.</returns>
        Task<UserLinkResult> InviteUser(ConnectAuthorizationRequest request);

        /// <summary>
        /// Gets the pending guests.
        /// </summary>
        /// <returns>Task&lt;List&lt;ConnectAuthorization&gt;&gt;.</returns>
        Task<List<ConnectAuthorization>> GetPendingGuests();

        /// <summary>
        /// Gets the user from exchange token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns>User.</returns>
        User GetUserFromExchangeToken(string token);

        /// <summary>
        /// Cancels the authorization.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task.</returns>
        Task CancelAuthorization(string id);

        /// <summary>
        /// Authenticates the specified username.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="passwordMd5">The password MD5.</param>
        /// <returns>Task.</returns>
        Task Authenticate(string username, string passwordMd5);

        /// <summary>
        /// Gets the local user.
        /// </summary>
        /// <param name="connectUserId">The connect user identifier.</param>
        /// <returns>Task&lt;User&gt;.</returns>
        Task<User> GetLocalUser(string connectUserId);

        /// <summary>
        /// Determines whether [is authorization token valid] [the specified token].
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns><c>true</c> if [is authorization token valid] [the specified token]; otherwise, <c>false</c>.</returns>
        bool IsAuthorizationTokenValid(string token);
    }
}
