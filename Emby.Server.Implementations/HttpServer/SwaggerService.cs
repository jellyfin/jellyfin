using MediaBrowser.Controller;
using MediaBrowser.Controller.Net;
using System.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Services;

namespace Emby.Server.Implementations.HttpServer
{
    public class SwaggerService : IHasResultFactory, IService
    {
        private readonly IServerApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;

        public SwaggerService(IServerApplicationPaths appPaths, IFileSystem fileSystem)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSwaggerResource request)
        {
            var swaggerDirectory = Path.Combine(_appPaths.ApplicationResourcesPath, "swagger-ui");

            var requestedFile = Path.Combine(swaggerDirectory, request.ResourceName.Replace('/', _fileSystem.DirectorySeparatorChar));

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
