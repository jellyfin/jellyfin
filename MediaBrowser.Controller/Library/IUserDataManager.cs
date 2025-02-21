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
        event EventHandler<UserDataSaveEventArgs>? UserDataSaved;

        /// <summary>
        /// Saves the user data.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="item">The item.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="reason">The reason.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void SaveUserData(User user, BaseItem item, UserItemData userData, UserDataSaveReason reason, CancellationToken cancellationToken);

        /// <summary>
        /// Save the provided user data for the given user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="item">The item.</param>
        /// <param name="userDataDto">The reason for updating the user data.</param>
        /// <param name="reason">The reason.</param>
        void SaveUserData(User user, BaseItem item, UpdateUserItemDataDto userDataDto, UserDataSaveReason reason);

        /// <summary>
        /// Gets the user data.
        /// </summary>
        /// <param name="user">User to use.</param>
        /// <param name="item">Item to use.</param>
        /// <returns>User data.</returns>
        UserItemData? GetUserData(User user, BaseItem item);

        /// <summary>
        /// Gets the user data dto.
        /// </summary>
        /// <param name="item">Item to use.</param>
        /// <param name="user">User to use.</param>
        /// <returns>User data dto.</returns>
        UserItemDataDto? GetUserDataDto(BaseItem item, User user);

        /// <summary>
        /// Gets the user data dto.
        /// </summary>
        /// <param name="item">Item to use.</param>
        /// <param name="itemDto">Item dto to use.</param>
        /// <param name="user">User to use.</param>
        /// <param name="options">Dto options to use.</param>
        /// <returns>User data dto.</returns>
        UserItemDataDto? GetUserDataDto(BaseItem item, BaseItemDto? itemDto, User user, DtoOptions options);

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
