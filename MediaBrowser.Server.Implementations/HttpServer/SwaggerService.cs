using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using ServiceStack.ServiceHost;
using System.IO;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    /// <summary>
    /// Class GetDashboardResource
    /// </summary>
    [Route("/swagger-ui/{ResourceName*}", "GET")]
    public class GetSwaggerResource
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string ResourceName { get; set; }
    }

    public class SwaggerService : IHasResultFactory, IRestfulService
    {
        private readonly IApplicationPaths _appPaths;

        public SwaggerService(IApplicationPaths appPaths)
        {
            _appPaths = appPaths;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSwaggerResource request)
        {
            var runningDirectory = Path.GetDirectoryName(_appPaths.ApplicationPath);

            var swaggerDirectory = Path.Combine(runningDirectory, "swagger-ui");

            var requestedFile = Path.Combine(swaggerDirectory, request.ResourceName.Replace('/', '\\'));

            return ResultFactory.GetStaticFileResult(RequestContext, requestedFile);
        }

        /// <summary>
        /// Gets or sets the result factory.
        /// </summary>
        /// <value>The result factory.</value>
        public IHttpResultFactory ResultFactory { get; set; }

        /// <summary>
        /// Gets or sets the request context.
        /// </summary>
        /// <value>The request context.</value>
        public IRequestContext RequestContext { get; set; }
    }
}
