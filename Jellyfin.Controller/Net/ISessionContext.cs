using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Session;
using Jellyfin.Model.Services;

namespace Jellyfin.Controller.Net
{
    public interface ISessionContext
    {
        SessionInfo GetSession(object requestContext);
        User GetUser(object requestContext);

        SessionInfo GetSession(IRequest requestContext);
        User GetUser(IRequest requestContext);
    }
}
