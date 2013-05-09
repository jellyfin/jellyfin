using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using ServiceStack.ServiceHost;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetSessions
    /// </summary>
    [Route("/Sessions", "GET")]
    [Api(("Gets a list of sessions"))]
    public class GetSessions : IReturn<List<SessionInfo>>
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is recent.
        /// </summary>
        /// <value><c>true</c> if this instance is recent; otherwise, <c>false</c>.</value>
        public bool IsRecent { get; set; }
    }

    /// <summary>
    /// Class SessionsService
    /// </summary>
    public class SessionsService : BaseApiService
    {
        /// <summary>
        /// The _session manager
        /// </summary>
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionsService"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public SessionsService(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSessions request)
        {
            var result = request.IsRecent ? _sessionManager.RecentConnections : _sessionManager.AllConnections;

            return ToOptimizedResult(result.ToList());
        }
    }
}
