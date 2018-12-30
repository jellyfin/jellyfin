using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Session;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaBrowser.Model.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Session
{
    /// <summary>
    /// Class SessionInfoWebSocketListener
    /// </summary>
    class SessionInfoWebSocketListener : BasePeriodicWebSocketListener<IEnumerable<SessionInfo>, WebSocketListenerState>
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
        public SessionInfoWebSocketListener(ILogger logger, ISessionManager sessionManager)
            : base(logger)
        {
            _sessionManager = sessionManager;

            _sessionManager.SessionStarted += _sessionManager_SessionStarted;
            _sessionManager.SessionEnded += _sessionManager_SessionEnded;
            _sessionManager.PlaybackStart += _sessionManager_PlaybackStart;
            _sessionManager.PlaybackStopped += _sessionManager_PlaybackStopped;
            _sessionManager.PlaybackProgress += _sessionManager_PlaybackProgress;
            _sessionManager.CapabilitiesChanged += _sessionManager_CapabilitiesChanged;
            _sessionManager.SessionActivity += _sessionManager_SessionActivity;
        }

        void _sessionManager_SessionActivity(object sender, SessionEventArgs e)
        {
            SendData(false);
        }

        void _sessionManager_CapabilitiesChanged(object sender, SessionEventArgs e)
        {
            SendData(true);
        }

        void _sessionManager_PlaybackProgress(object sender, PlaybackProgressEventArgs e)
        {
            SendData(!e.IsAutomated);
        }

        void _sessionManager_PlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            SendData(true);
        }

        void _sessionManager_PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            SendData(true);
        }

        void _sessionManager_SessionEnded(object sender, SessionEventArgs e)
        {
            SendData(true);
        }

        void _sessionManager_SessionStarted(object sender, SessionEventArgs e)
        {
            SendData(true);
        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>Task{SystemInfo}.</returns>
        protected override Task<IEnumerable<SessionInfo>> GetDataToSend(WebSocketListenerState state, CancellationToken cancellationToken)
        {
            return Task.FromResult(_sessionManager.Sessions);
        }

        protected override void Dispose(bool dispose)
        {
            _sessionManager.SessionStarted -= _sessionManager_SessionStarted;
            _sessionManager.SessionEnded -= _sessionManager_SessionEnded;
            _sessionManager.PlaybackStart -= _sessionManager_PlaybackStart;
            _sessionManager.PlaybackStopped -= _sessionManager_PlaybackStopped;
            _sessionManager.PlaybackProgress -= _sessionManager_PlaybackProgress;
            _sessionManager.CapabilitiesChanged -= _sessionManager_CapabilitiesChanged;
            _sessionManager.SessionActivity -= _sessionManager_SessionActivity;

            base.Dispose(dispose);
        }
    }
}
