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
        Task<IEnumerable<Folder>> GetUserViews(UserViewQuery query, CancellationToken cancellationToken);

        Task<UserView> GetUserSubView(string name, string parentId, string type, string sortName, CancellationToken cancellationToken);

        Task<UserView> GetUserSubView(string category, string type, string sortName, CancellationToken cancellationToken);

        List<Tuple<BaseItem, List<BaseItem>>> GetLatestItems(LatestItemsQuery request, DtoOptions options);
    }
}
