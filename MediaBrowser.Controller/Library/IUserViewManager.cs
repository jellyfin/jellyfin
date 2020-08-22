#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Library
{
    public interface IUserViewManager
    {
        Folder[] GetUserViews(UserViewQuery query);

        UserView GetUserSubView(Guid parentId, string type, string localizationKey, string sortName);

        List<Tuple<BaseItem, List<BaseItem>>> GetLatestItems(LatestItemsQuery request, DtoOptions options);
    }
}
