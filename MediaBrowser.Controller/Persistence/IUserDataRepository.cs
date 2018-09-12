using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using System;
using System.Threading;

namespace MediaBrowser.Controller.Persistence
{
    /// <summary>
    /// Provides an interface to implement a UserData repository
    /// </summary>
    public interface IUserDataRepository : IRepository
    {
        /// <summary>
        /// Saves the user data.
        /// </summary>
        void SaveUserData(long internalUserId, string key, UserItemData userData, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <returns>Task{UserItemData}.</returns>
        UserItemData GetUserData(long internalUserId, string key);

        UserItemData GetUserData(long internalUserId, List<string> keys);

        /// <summary>
        /// Return all user data associated with the given user
        /// </summary>
        List<UserItemData> GetAllUserData(long internalUserId);

        /// <summary>
        /// Save all user data associated with the given user
        /// </summary>
        void SaveAllUserData(long internalUserId, UserItemData[] userData, CancellationToken cancellationToken);

    }
}
