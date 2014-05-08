using ServiceStack;

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
}