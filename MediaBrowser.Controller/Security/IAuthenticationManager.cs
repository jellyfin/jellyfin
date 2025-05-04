using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Security
{
    /// <summary>
    /// Handles the retrieval and storage of API keys.
    /// </summary>
    public interface IAuthenticationManager
    {
        /// <summary>
        /// Creates an API key.
        /// </summary>
        /// <param name="name">The name of the key.</param>
        /// <returns>A task representing the creation of the key.</returns>
        Task CreateApiKey(string name);

        /// <summary>
        /// Gets the API keys.
        /// </summary>
        /// <returns>A task representing the retrieval of the API keys.</returns>
        Task<IReadOnlyList<AuthenticationInfo>> GetApiKeys();

        /// <summary>
        /// Deletes an API key with the provided access token.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <returns>A task representing the deletion of the API key.</returns>
        Task DeleteApiKey(string accessToken);
    }
}
