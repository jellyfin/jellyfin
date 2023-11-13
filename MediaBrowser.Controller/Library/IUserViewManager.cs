#nullable disable

#pragma warning disable CA1002, CS1591

using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Library
{
    public interface IUserViewManager
    {
        /// <summary>
        /// Gets user views.
        /// </summary>
        /// <param name="query">Query to use.</param>
        /// <returns>Set of folders.</returns>
        Folder[] GetUserViews(UserViewQuery query);

        /// <summary>
        /// Gets user sub views.
        /// </summary>
        /// <param name="parentId">Parent to use.</param>
        /// <param name="type">Type to use.</param>
        /// <param name="localizationKey">Localization key to use.</param>
        /// <param name="sortName">Sort to use.</param>
        /// <returns>User view.</returns>
        UserView GetUserSubView(Guid parentId, CollectionType? type, string localizationKey, string sortName);

        /// <summary>
        /// Gets latest items.
        /// </summary>
        /// <param name="request">Query to use.</param>
        /// <param name="options">Options to use.</param>
        /// <returns>Set of items.</returns>
        List<Tuple<BaseItem, List<BaseItem>>> GetLatestItems(LatestItemsQuery request, DtoOptions options);
    }
}
