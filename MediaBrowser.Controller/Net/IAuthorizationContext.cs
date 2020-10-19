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
        /// <returns>AuthorizationInfo.</returns>
        AuthorizationInfo GetAuthorizationInfo(HttpContext requestContext);

        /// <summary>
        /// Gets the authorization information.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>AuthorizationInfo.</returns>
        AuthorizationInfo GetAuthorizationInfo(HttpRequest requestContext);
    }
}
