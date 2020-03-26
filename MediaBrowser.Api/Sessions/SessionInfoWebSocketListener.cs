using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.Sessions
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
        public SessionInfoWebSocketListener(ILogger<SessionInfoWebSocketListener> logger, ISessionManager sessionManager)
            : base(logger)
        {
            _sessionManager = sessionManager;

            _sessionManager.SessionStarted += SessionManagerSessionStarted;
            _sessionManager.SessionEnded += SessionManagerSessionEnded;
            _sessionManager.PlaybackStart += SessionManagerPlaybackStart;
            _sessionManager.PlaybackStopped += SessionManagerPlaybackStopped;
            _sessionManager.PlaybackProgress += SessionManagerPlaybackProgress;
            _sessionManager.CapabilitiesChanged += SessionManagerCapabilitiesChanged;
            _sessionManager.SessionActivity += SessionManagerSessionActivity;
        }

        private void SessionManagerSessionActivity(object sender, SessionEventArgs e)
        {
            SendData(false);
        }

        private void SessionManagerCapabilitiesChanged(object sender, SessionEventArgs e)
        {
            SendData(true);
        }

        private void SessionManagerPlaybackProgress(object sender, PlaybackProgressEventArgs e)
        {
            SendData(!e.IsAutomated);
        }

        private void SessionManagerPlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            SendData(true);
        }

        private void SessionManagerPlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            SendData(true);
        }

        private void SessionManagerSessionEnded(object sender, SessionEventArgs e)
        {
            SendData(true);
        }

        private void SessionManagerSessionStarted(object sender, SessionEventArgs e)
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
            _sessionManager.SessionStarted -= SessionManagerSessionStarted;
            _sessionManager.SessionEnded -= SessionManagerSessionEnded;
            _sessionManager.PlaybackStart -= SessionManagerPlaybackStart;
            _sessionManager.PlaybackStopped -= SessionManagerPlaybackStopped;
            _sessionManager.PlaybackProgress -= SessionManagerPlaybackProgress;
            _sessionManager.CapabilitiesChanged -= SessionManagerCapabilitiesChanged;
            _sessionManager.SessionActivity -= SessionManagerSessionActivity;

            base.Dispose(dispose);
        }
    }
}
