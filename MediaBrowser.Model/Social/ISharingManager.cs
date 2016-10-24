using System.Threading.Tasks;

namespace MediaBrowser.Model.Social
{
    public interface ISharingManager
    {
        /// <summary>
        /// Creates the share.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>Task&lt;SocialShareInfo&gt;.</returns>
        Task<SocialShareInfo> CreateShare(string itemId, string userId);
        /// <summary>
        /// Gets the share information.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>SocialShareInfo.</returns>
        SocialShareInfo GetShareInfo(string id);
        /// <summary>
        /// Deletes the share.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task.</returns>
        Task DeleteShare(string id);
    }
}
