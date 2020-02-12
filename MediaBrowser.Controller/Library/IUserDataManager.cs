using System;
using System.Collections.Generic;
using System.Threading;
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
        void SaveUserData(User userId, BaseItem item, UserItemData userData, UserDataSaveReason reason, CancellationToken cancellationToken);

        UserItemData GetUserData(User user, BaseItem item);

        UserItemData GetUserData(Guid userId, BaseItem item);

        /// <summary>
        /// Gets the user data dto.
        /// </summary>
        UserItemDataDto GetUserDataDto(BaseItem item, User user);

        UserItemDataDto GetUserDataDto(BaseItem item, BaseItemDto itemDto, User user, DtoOptions dto_options);

        /// <summary>
        /// Get all user data for the given user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        List<UserItemData> GetAllUserData(Guid userId);

        /// <summary>
        /// Save the all provided user data for the given user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        void SaveAllUserData(Guid userId, UserItemData[] userData, CancellationToken cancellationToken);

        /// <summary>
        /// Updates playstate for an item and returns true or false indicating if it was played to completion
        /// </summary>
        bool UpdatePlayState(BaseItem item, UserItemData data, long? positionTicks);
    }
}
