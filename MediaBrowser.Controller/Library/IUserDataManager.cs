using System;
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
        /// Gets the user item data for a specified user and item.
        /// </summary>
        /// <param name="userId">The user's id.</param>
        /// <param name="itemId">The item's id.</param>
        /// <returns>The user item data for that user and item.</returns>
        UserItemData GetUserItemData(Guid userId, Guid itemId);

        /// <summary>
        /// Saves the provided user item data.
        /// </summary>
        /// <param name="itemData">The user item data.</param>
        /// <param name="reason">The save reason.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void SaveUserItemData(UserItemData itemData, UserDataSaveReason reason, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the user data dto.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="item">The item.</param>
        /// <returns>A dto containing the user's item data.</returns>
        UserItemDataDto GetUserDataDto(User user, BaseItem item);

        /// <summary>
        /// Gets a user item data dto for the provided user and item.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="item">The item.</param>
        /// <param name="itemDto">The item's dto.</param>
        /// <param name="dtoOptions">The dto options.</param>
        /// <returns>A dto for the user item data.</returns>
        UserItemDataDto GetUserDataDto(User user, BaseItem item, BaseItemDto itemDto, DtoOptions dtoOptions);

        /// <summary>
        /// Updates the play state for an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="data">The user item data.</param>
        /// <param name="positionTicks">The new position tick value.</param>
        /// <returns><c>true</c> if the item was played to completion.</returns>
        bool UpdatePlayState(BaseItem item, UserItemData data, long? positionTicks);
    }
}
