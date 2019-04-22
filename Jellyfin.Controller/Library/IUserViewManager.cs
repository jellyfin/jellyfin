using System;
using System.Collections.Generic;
using Jellyfin.Controller.Dto;
using Jellyfin.Controller.Entities;
using Jellyfin.Model.Library;
using Jellyfin.Model.Querying;

namespace Jellyfin.Controller.Library
{
    public interface IUserViewManager
    {
        Folder[] GetUserViews(UserViewQuery query);
        UserView GetUserSubView(Guid parentId, string type, string localizationKey, string sortName);

        List<Tuple<BaseItem, List<BaseItem>>> GetLatestItems(LatestItemsQuery request, DtoOptions options);
    }
}
