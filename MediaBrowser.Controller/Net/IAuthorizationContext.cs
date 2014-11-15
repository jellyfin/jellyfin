
namespace MediaBrowser.Controller.Net
{
    public interface IAuthorizationContext
    {
        /// <summary>
        /// Gets the authorization information.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>AuthorizationInfo.</returns>
        AuthorizationInfo GetAuthorizationInfo(object requestContext);
        
        /// <summary>
        /// Gets the authorization information.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>AuthorizationInfo.</returns>
        AuthorizationInfo GetAuthorizationInfo(IServiceRequest requestContext);
    }
}
