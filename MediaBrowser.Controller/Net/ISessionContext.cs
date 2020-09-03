#pragma warning disable CS1591

using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Session;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller.Net
{
    public interface ISessionContext
    {
        SessionInfo GetSession(object requestContext);

        User GetUser(object requestContext);

        SessionInfo GetSession(HttpContext requestContext);

        User GetUser(HttpContext requestContext);
    }
}
