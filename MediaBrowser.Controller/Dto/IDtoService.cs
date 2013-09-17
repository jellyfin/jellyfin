using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Dto
{
    /// <summary>
    /// Interface IDtoService
    /// </summary>
    public interface IDtoService
    {
        /// <summary>
        /// Gets the user dto.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>UserDto.</returns>
        UserDto GetUserDto(User user);

        /// <summary>
        /// Gets the session info dto.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>SessionInfoDto.</returns>
        SessionInfoDto GetSessionInfoDto(SessionInfo session);

        /// <summary>
        /// Gets the base item info.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>BaseItemInfo.</returns>
        BaseItemInfo GetBaseItemInfo(BaseItem item);

        /// <summary>
        /// Gets the dto id.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        string GetDtoId(BaseItem item);

        /// <summary>
        /// Gets the user item data dto.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>UserItemDataDto.</returns>
        UserItemDataDto GetUserItemDataDto(UserItemData data);

        /// <summary>
        /// Gets the item by dto id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>BaseItem.</returns>
        BaseItem GetItemByDtoId(string id, Guid? userId = null);

        /// <summary>
        /// Gets the base item dto.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="fields">The fields.</param>
        /// <param name="user">The user.</param>
        /// <param name="owner">The owner.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        BaseItemDto GetBaseItemDto(BaseItem item, List<ItemFields> fields, User user = null, BaseItem owner = null);
    }
}
