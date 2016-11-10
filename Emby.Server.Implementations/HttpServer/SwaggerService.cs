using MediaBrowser.Controller;
using MediaBrowser.Controller.Net;
using System.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Services;

namespace Emby.Server.Implementations.HttpServer
{
    public class SwaggerService : IService, IRequiresRequest
    {
        private readonly IServerApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;

        public SwaggerService(IServerApplicationPaths appPaths, IFileSystem fileSystem, IHttpResultFactory resultFactory)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _resultFactory = resultFactory;
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

            return _resultFactory.GetStaticFileResult(Request, requestedFile).Result;
        }

        /// <summary>
        /// Gets or sets the result factory.
        /// </summary>
        /// <value>The result factory.</value>
        private readonly IHttpResultFactory _resultFactory;

        /// <summary>
        /// Gets or sets the request context.
        /// </summary>
        /// <value>The request context.</value>
        public IRequest Request { get; set; }
    }
}
