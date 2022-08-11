#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Persistence
{
    /// <summary>
    /// Provides an interface to implement a UserData repository.
    /// </summary>
    public interface IUserDataRepository : IDisposable
    {
        /// <summary>
        /// Saves the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void SaveUserData(long userId, string key, UserItemData userData, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <returns>The user data.</returns>
        UserItemData GetUserData(long userId, string key);

        /// <summary>
        /// Gets the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="keys">The keys.</param>
        /// <returns>The user data.</returns>
        UserItemData GetUserData(long userId, List<string> keys);

        /// <summary>
        /// Return all user data associated with the given user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns>The list of user item data.</returns>
        List<UserItemData> GetAllUserData(long userId);

        /// <summary>
        /// Save all user data associated with the given user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="userData">The user item data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void SaveAllUserData(long userId, UserItemData[] userData, CancellationToken cancellationToken);
    }
}
