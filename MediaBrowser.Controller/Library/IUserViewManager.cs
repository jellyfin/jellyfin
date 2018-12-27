using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Dto;

namespace MediaBrowser.Controller.Library
{
    public interface IUserViewManager
    {
        Folder[] GetUserViews(UserViewQuery query);
        UserView GetUserSubView(Guid parentId, string type, string localizationKey, string sortName);

        List<Tuple<BaseItem, List<BaseItem>>> GetLatestItems(LatestItemsQuery request, DtoOptions options);
    }
}
