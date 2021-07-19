#pragma warning disable CS1591

using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Session;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller.Net
{
    public interface ISessionContext
    {
        Task<SessionInfo> GetSession(object requestContext);

        Task<User?> GetUser(object requestContext);

        Task<SessionInfo> GetSession(HttpContext requestContext);

        Task<User?> GetUser(HttpContext requestContext);
    }
}
