using ServiceStack.ServiceHost;
using System.Diagnostics;
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
    
    public class SwaggerService : BaseRestService
    {
        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSwaggerResource request)
        {
            var runningDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            var swaggerDirectory = Path.Combine(runningDirectory, "swagger-ui");

            var requestedFile = Path.Combine(swaggerDirectory, request.ResourceName.Replace('/', '\\'));

            return ToStaticFileResult(requestedFile);
        }
    }
}
