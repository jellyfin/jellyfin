using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// IAuthorization context.
    /// </summary>
    public interface IAuthorizationContext
    {
        /// <summary>
        /// Gets the authorization information.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>A task containing the authorization info.</returns>
        Task<AuthorizationInfo> GetAuthorizationInfo(HttpContext requestContext);

        /// <summary>
        /// Gets the authorization information.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>A <see cref="Task"/> containing the authorization info.</returns>
        Task<AuthorizationInfo> GetAuthorizationInfo(HttpRequest requestContext);
    }
}
