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
        Task<UserLinkResult> LinkUser(User user, string connectUsername);

        /// <summary>
        /// Removes the link.
        /// </summary>
        Task RemoveConnect(User user);

        User GetUserFromExchangeToken(string token);

        /// <summary>
        /// Authenticates the specified username.
        /// </summary>
        Task<ConnectAuthenticationResult> Authenticate(string username, string password);

        /// <summary>
        /// Determines whether [is authorization token valid] [the specified token].
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns><c>true</c> if [is authorization token valid] [the specified token]; otherwise, <c>false</c>.</returns>
        bool IsAuthorizationTokenValid(string token);
    }
}
