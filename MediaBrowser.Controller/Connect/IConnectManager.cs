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
        /// Gets the user information.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>ConnectUserInfo.</returns>
        ConnectUserLink GetUserInfo(string userId);

        /// <summary>
        /// Links the user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="connectUsername">The connect username.</param>
        /// <returns>Task.</returns>
        Task LinkUser(string userId, string connectUsername);

        /// <summary>
        /// Removes the link.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>Task.</returns>
        Task RemoveLink(string userId);
    }
}
