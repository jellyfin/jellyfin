using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Session;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Net
{
    public interface ISessionContext 
    {
        Task<SessionInfo> GetSession(object requestContext);
        Task<User> GetUser(object requestContext);

        Task<SessionInfo> GetSession(IServiceRequest requestContext);
        Task<User> GetUser(IServiceRequest requestContext);
    }
}
