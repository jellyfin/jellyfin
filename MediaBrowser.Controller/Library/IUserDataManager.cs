#nullable disable

#pragma warning disable CA1002, CA1707, CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Interface IUserDataManager.
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
        /// <param name="item">The item.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="reason">The reason.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void SaveUserData(Guid userId, BaseItem item, UserItemData userData, UserDataSaveReason reason, CancellationToken cancellationToken);

        void SaveUserData(User user, BaseItem item, UserItemData userData, UserDataSaveReason reason, CancellationToken cancellationToken);

        UserItemData GetUserData(User user, BaseItem item);

        UserItemData GetUserData(Guid userId, BaseItem item);

        /// <summary>
        /// Gets the user data dto.
        /// </summary>
        /// <param name="item">Item to use.</param>
        /// <param name="user">User to use.</param>
        /// <returns>User data dto.</returns>
        UserItemDataDto GetUserDataDto(BaseItem item, User user);

        UserItemDataDto GetUserDataDto(BaseItem item, BaseItemDto itemDto, User user, DtoOptions options);

        /// <summary>
        /// Get all user data for the given user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns>The user item data.</returns>
        List<UserItemData> GetAllUserData(Guid userId);

        /// <summary>
        /// Save the all provided user data for the given user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="userData">The array of user data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void SaveAllUserData(Guid userId, UserItemData[] userData, CancellationToken cancellationToken);

        /// <summary>
        /// Updates playstate for an item and returns true or false indicating if it was played to completion.
        /// </summary>
        /// <param name="item">Item to update.</param>
        /// <param name="data">Data to update.</param>
        /// <param name="reportedPositionTicks">New playstate.</param>
        /// <returns>True if playstate was updated.</returns>
        bool UpdatePlayState(BaseItem item, UserItemData data, long? reportedPositionTicks);
    }
}
