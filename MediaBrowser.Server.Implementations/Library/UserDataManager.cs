using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Library
{
    /// <summary>
    /// Class UserDataManager
    /// </summary>
    public class UserDataManager : IUserDataManager
    {
        /// <summary>
        /// Gets or sets the repository.
        /// </summary>
        /// <value>The repository.</value>
        public IUserDataRepository Repository { get; set; }

        /// <summary>
        /// Saves the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SaveUserData(Guid userId, string key, UserItemData userData, CancellationToken cancellationToken)
        {
            return Repository.SaveUserData(userId, key, userData, cancellationToken);
        }

        /// <summary>
        /// Gets the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <returns>Task{UserItemData}.</returns>
        public UserItemData GetUserData(Guid userId, string key)
        {
            return Repository.GetUserData(userId, key);
        }
    }
}
