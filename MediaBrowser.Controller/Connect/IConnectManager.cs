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
        Task RemoveLink(string userId);

        /// <summary>
        /// Invites the user.
        /// </summary>
        /// <param name="sendingUserId">The sending user identifier.</param>
        /// <param name="connectUsername">The connect username.</param>
        /// <returns>Task&lt;UserLinkResult&gt;.</returns>
        Task<UserLinkResult> InviteUser(string sendingUserId, string connectUsername);

        /// <summary>
        /// Gets the pending guests.
        /// </summary>
        /// <returns>Task&lt;List&lt;ConnectAuthorization&gt;&gt;.</returns>
        Task<List<ConnectAuthorization>> GetPendingGuests();

        /// <summary>
        /// Cancels the authorization.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task.</returns>
        Task CancelAuthorization(string id);
    }
}
