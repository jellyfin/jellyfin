using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Common.Extensions
{
    /// <summary>
    /// Static class containing extension methods for <see cref="HttpContext"/>.
    /// </summary>
    public static class HttpContextExtensions
    {
        private const string SERVICESTACKREQUEST = "ServiceStackRequest";

        /// <summary>
        /// Set the ServiceStack request.
        /// </summary>
        /// <param name="httpContext">The HttpContext instance.</param>
        /// <param name="request">The service stack request instance.</param>
        public static void SetServiceStackRequest(this HttpContext httpContext, IRequest request)
        {
            httpContext.Items[SERVICESTACKREQUEST] = request;
        }

        /// <summary>
        /// Get the ServiceStack request.
        /// </summary>
        /// <param name="httpContext">The HttpContext instance.</param>
        /// <returns>The service stack request instance.</returns>
        public static IRequest GetServiceStackRequest(this HttpContext httpContext)
        {
            return (IRequest)httpContext.Items[SERVICESTACKREQUEST];
        }
    }
}
