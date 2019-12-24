using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.Session
{
    /// <summary>
    /// Class SessionInfoWebSocketListener
    /// </summary>
    public class SessionInfoWebSocketListener : BasePeriodicWebSocketListener<IEnumerable<SessionInfo>, WebSocketListenerState>
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        protected override string Name => "Sessions";

        /// <summary>
        /// The _kernel
        /// </summary>
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionInfoWebSocketListener"/> class.
        /// </summary>
        public SessionInfoWebSocketListener(ILogger logger, ISessionManager sessionManager)
            : base(logger)
        {
            _sessionManager = sessionManager;

            _sessionManager.SessionStarted += SessionManager_SessionStarted;
            _sessionManager.SessionEnded += SessionManager_SessionEnded;
            _sessionManager.PlaybackStart += SessionManager_PlaybackStart;
            _sessionManager.PlaybackStopped += SessionManager_PlaybackStopped;
            _sessionManager.PlaybackProgress += SessionManager_PlaybackProgress;
            _sessionManager.CapabilitiesChanged += SessionManager_CapabilitiesChanged;
            _sessionManager.SessionActivity += SessionManager_SessionActivity;
        }

        private void SessionManager_SessionActivity(object sender, SessionEventArgs e)
        {
            SendData(false);
        }

        private void SessionManager_CapabilitiesChanged(object sender, SessionEventArgs e)
        {
            SendData(true);
        }

        private void SessionManager_PlaybackProgress(object sender, PlaybackProgressEventArgs e)
        {
            SendData(!e.IsAutomated);
        }

        private void SessionManager_PlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            SendData(true);
        }

        private void SessionManager_PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            SendData(true);
        }

        private void SessionManager_SessionEnded(object sender, SessionEventArgs e)
        {
            SendData(true);
        }

        private void SessionManager_SessionStarted(object sender, SessionEventArgs e)
        {
            SendData(true);
        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <returns>Task{SystemInfo}.</returns>
        protected override Task<IEnumerable<SessionInfo>> GetDataToSend()
        {
            return Task.FromResult(_sessionManager.Sessions);
        }

        protected override void Dispose(bool dispose)
        {
            _sessionManager.SessionStarted -= SessionManager_SessionStarted;
            _sessionManager.SessionEnded -= SessionManager_SessionEnded;
            _sessionManager.PlaybackStart -= SessionManager_PlaybackStart;
            _sessionManager.PlaybackStopped -= SessionManager_PlaybackStopped;
            _sessionManager.PlaybackProgress -= SessionManager_PlaybackProgress;
            _sessionManager.CapabilitiesChanged -= SessionManager_CapabilitiesChanged;
            _sessionManager.SessionActivity -= SessionManager_SessionActivity;

            base.Dispose(dispose);
        }
    }
}
