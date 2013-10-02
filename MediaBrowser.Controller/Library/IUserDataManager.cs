using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Interface IUserDataManager
    /// </summary>
    public interface IUserDataManager
    {
        /// <summary>
        /// Occurs when [user data saved].
        /// </summary>
        event EventHandler<UserDataSaveEventArgs> UserDataSaved;

        /// <summary>
        /// Saves the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="reason">The reason.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveUserData(Guid userId, string key, UserItemData userData, UserDataSaveReason reason, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <returns>Task{UserItemData}.</returns>
        UserItemData GetUserData(Guid userId, string key);
    }
}
