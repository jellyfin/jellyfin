using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
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
        /// <param name="item">The item.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="reason">The reason.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveUserData(Guid userId, IHasUserData item, UserItemData userData, UserDataSaveReason reason, CancellationToken cancellationToken);

        UserItemData GetUserData(IHasUserData user, IHasUserData item);

        UserItemData GetUserData(string userId, IHasUserData item);
        UserItemData GetUserData(Guid userId, IHasUserData item);

        /// <summary>
        /// Gets the user data dto.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="user">The user.</param>
        /// <returns>UserItemDataDto.</returns>
        Task<UserItemDataDto> GetUserDataDto(IHasUserData item, User user);

        Task<UserItemDataDto> GetUserDataDto(IHasUserData item, BaseItemDto itemDto, User user);

        /// <summary>
        /// Get all user data for the given user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        IEnumerable<UserItemData> GetAllUserData(Guid userId);

        /// <summary>
        /// Save the all provided user data for the given user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SaveAllUserData(Guid userId, IEnumerable<UserItemData> userData, CancellationToken cancellationToken);

        /// <summary>
        /// Updates playstate for an item and returns true or false indicating if it was played to completion
        /// </summary>
        bool UpdatePlayState(BaseItem item, UserItemData data, long? positionTicks);
    }
}
