using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;

namespace Emby.Server.Implementations.Services
{
    /// <summary>
    /// Extention to enable the service stack request to be stored in the HttpRequest object.
    /// </summary>
    public static class HttpContextExtension
    {
        private const string SERVICESTACKREQUEST = "ServiceRequestStack";

        /// <summary>
        /// Set the service stack request.
        /// </summary>
        /// <param name="httpContext">The HttpContext instance.</param>
        /// <param name="request">The IRequest instance.</param>
        public static void SetServiceStackRequest(this HttpContext httpContext, IRequest request)
        {
            httpContext.Items[SERVICESTACKREQUEST] = request;
        }

        /// <summary>
        /// Get the service stack request.
        /// </summary>
        /// <param name="httpContext">The HttpContext instance.</param>
        /// <returns>The service stack request instance.</returns>
        public static IRequest GetServiceStack(this HttpContext httpContext)
        {
            return (IRequest)httpContext.Items[SERVICESTACKREQUEST];
        }
    }
}
