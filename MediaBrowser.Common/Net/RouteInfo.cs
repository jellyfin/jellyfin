using System;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Class RouteInfo
    /// </summary>
    public class RouteInfo
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the verbs.
        /// </summary>
        /// <value>The verbs.</value>
        public string Verbs { get; set; }

        /// <summary>
        /// Gets or sets the type of the request.
        /// </summary>
        /// <value>The type of the request.</value>
        public Type RequestType { get; set; }
    }
}
