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

        UserItemData GetUserItemData(Guid userId, Guid itemId);

        void SaveUserItemData(UserItemData itemData, UserDataSaveReason reason, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the user data dto.
        /// </summary>
        /// <returns>A dto containing the user's item data.</returns>
        UserItemDataDto GetUserDataDto(User user, BaseItem item);

        UserItemDataDto GetUserDataDto(User user, BaseItem item, BaseItemDto itemDto, DtoOptions dtoOptions);

        /// <summary>
        /// Updates playstate for an item and returns true or false indicating if it was played to completion.
        /// </summary>
        bool UpdatePlayState(BaseItem item, UserItemData data, long? positionTicks);
    }
}
