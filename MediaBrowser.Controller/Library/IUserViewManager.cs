using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Library;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Library
{
    public interface IUserViewManager
    {
        Task<IEnumerable<Folder>> GetUserViews(UserViewQuery query, CancellationToken cancellationToken);

        Task<UserView> GetUserView(string type, string sortName, CancellationToken cancellationToken);

        Task<UserView> GetUserView(string category, string type, User user, string sortName, CancellationToken cancellationToken);
    }
}
