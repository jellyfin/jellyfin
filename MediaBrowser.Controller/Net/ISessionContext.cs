using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Session;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Controller.Net
{
    public interface ISessionContext 
    {
        Task<SessionInfo> GetSession(object requestContext);
        Task<User> GetUser(object requestContext);

        Task<SessionInfo> GetSession(IRequest requestContext);
        Task<User> GetUser(IRequest requestContext);
    }
}
