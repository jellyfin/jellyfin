using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Controller.Net
{
    public interface ISessionContext
    {
        SessionInfo GetSession(object requestContext);
        User GetUser(object requestContext);

        SessionInfo GetSession(IRequest requestContext);
        User GetUser(IRequest requestContext);
    }
}
