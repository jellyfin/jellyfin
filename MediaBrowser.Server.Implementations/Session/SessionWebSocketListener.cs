using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Session;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Session
{
    /// <summary>
    /// Class SessionWebSocketListener
    /// </summary>
    public class SessionWebSocketListener : IWebSocketListener
    {
        /// <summary>
        /// The _true task result
        /// </summary>
        private readonly Task _trueTaskResult = Task.FromResult(true);

        /// <summary>
        /// The _session manager
        /// </summary>
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionWebSocketListener"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public SessionWebSocketListener(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>Task.</returns>
        public Task ProcessMessage(WebSocketMessageInfo message)
        {
            if (string.Equals(message.MessageType, "Identity", StringComparison.OrdinalIgnoreCase))
            {
                var vals = message.Data.Split('|');

                var client = vals[0];
                var deviceId = vals[1];

                var session = _sessionManager.AllConnections.FirstOrDefault(i => string.Equals(i.DeviceId, deviceId) && string.Equals(i.Client, client));

                if (session != null)
                {
                    ((SessionManager)_sessionManager).IdentifyWebSocket(session.Id, message.Connection);
                }
            }

            return _trueTaskResult;
        }
    }
}
