using MediaBrowser.Controller;
using MediaBrowser.Controller.Net;
using ServiceStack.Web;
using System.IO;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    public class SwaggerService : IHasResultFactory, IRestfulService
    {
        private readonly IServerApplicationPaths _appPaths;

        public SwaggerService(IServerApplicationPaths appPaths)
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
            var swaggerDirectory = Path.Combine(_appPaths.ApplicationResourcesPath, "swagger-ui");

            var requestedFile = Path.Combine(swaggerDirectory, request.ResourceName.Replace('/', Path.DirectorySeparatorChar));

            return ResultFactory.GetStaticFileResult(Request, requestedFile).Result;
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
        public IRequest Request { get; set; }
    }
}
