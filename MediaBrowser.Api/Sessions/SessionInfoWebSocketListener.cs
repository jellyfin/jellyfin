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

            _sessionManager.SessionStarted += OnSessionManagerSessionStarted;
            _sessionManager.SessionEnded += OnSessionManagerSessionEnded;
            _sessionManager.PlaybackStart += OnSessionManagerPlaybackStart;
            _sessionManager.PlaybackStopped += OnSessionManagerPlaybackStopped;
            _sessionManager.PlaybackProgress += OnSessionManagerPlaybackProgress;
            _sessionManager.CapabilitiesChanged += OnSessionManagerCapabilitiesChanged;
            _sessionManager.SessionActivity += OnSessionManagerSessionActivity;
        }

        private async void OnSessionManagerSessionActivity(object sender, SessionEventArgs e)
        {
            await SendData(false).ConfigureAwait(false);
        }

        private async void OnSessionManagerCapabilitiesChanged(object sender, SessionEventArgs e)
        {
            await SendData(true).ConfigureAwait(false);
        }

        private async void OnSessionManagerPlaybackProgress(object sender, PlaybackProgressEventArgs e)
        {
            await SendData(!e.IsAutomated).ConfigureAwait(false);
        }

        private async void OnSessionManagerPlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            await SendData(true).ConfigureAwait(false);
        }

        private async void OnSessionManagerPlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            await SendData(true).ConfigureAwait(false);
        }

        private async void OnSessionManagerSessionEnded(object sender, SessionEventArgs e)
        {
            await SendData(true).ConfigureAwait(false);
        }

        private async void OnSessionManagerSessionStarted(object sender, SessionEventArgs e)
        {
            await SendData(true).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <returns>Task{SystemInfo}.</returns>
        protected override Task<IEnumerable<SessionInfo>> GetDataToSend()
        {
            return Task.FromResult(_sessionManager.Sessions);
        }

        /// <inheritdoc />
        protected override void Dispose(bool dispose)
        {
            _sessionManager.SessionStarted -= OnSessionManagerSessionStarted;
            _sessionManager.SessionEnded -= OnSessionManagerSessionEnded;
            _sessionManager.PlaybackStart -= OnSessionManagerPlaybackStart;
            _sessionManager.PlaybackStopped -= OnSessionManagerPlaybackStopped;
            _sessionManager.PlaybackProgress -= OnSessionManagerPlaybackProgress;
            _sessionManager.CapabilitiesChanged -= OnSessionManagerCapabilitiesChanged;
            _sessionManager.SessionActivity -= OnSessionManagerSessionActivity;

            base.Dispose(dispose);
        }
    }
}
