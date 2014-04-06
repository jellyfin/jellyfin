using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Session;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.WebSocket
{
    /// <summary>
    /// Class SessionInfoWebSocketListener
    /// </summary>
    class SessionInfoWebSocketListener : BasePeriodicWebSocketListener<IEnumerable<SessionInfoDto>, object>
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        protected override string Name
        {
            get { return "Sessions"; }
        }

        /// <summary>
        /// The _kernel
        /// </summary>
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionInfoWebSocketListener"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="sessionManager">The session manager.</param>
        public SessionInfoWebSocketListener(ILogger logger, ISessionManager sessionManager)
            : base(logger)
        {
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>Task{SystemInfo}.</returns>
        protected override Task<IEnumerable<SessionInfoDto>> GetDataToSend(object state)
        {
            return Task.FromResult(_sessionManager.Sessions.Where(i => i.IsActive).Select(_sessionManager.GetSessionInfoDto));
        }
    }
}
