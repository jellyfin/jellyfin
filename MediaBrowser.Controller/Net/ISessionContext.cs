using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.Net
{
    public interface ISessionContext 
    {
        SessionInfo GetSession(object requestContext);
        User GetUser(object requestContext);
   
        SessionInfo GetSession(IServiceRequest requestContext);
        User GetUser(IServiceRequest requestContext);
    }
}
