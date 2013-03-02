using System.Collections.Generic;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Interface IRestfulService
    /// </summary>
    public interface IRestfulService
    {
        /// <summary>
        /// Gets the routes.
        /// </summary>
        /// <returns>IEnumerable{RouteInfo}.</returns>
        IEnumerable<RouteInfo> GetRoutes();
    }
}
