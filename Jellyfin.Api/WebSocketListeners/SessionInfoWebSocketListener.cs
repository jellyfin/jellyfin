using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Controller.Events.Session;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;
using Rebus.Handlers;

namespace Jellyfin.Api.WebSocketListeners
{
    /// <summary>
    /// Class SessionInfoWebSocketListener.
    /// </summary>
    public class SessionInfoWebSocketListener : BasePeriodicWebSocketListener<IEnumerable<SessionInfo>,
            WebSocketListenerState>,
            IHandleMessages<PlaybackStartEventArgs>,
            IHandleMessages<PlaybackProgressEventArgs>,
            IHandleMessages<PlaybackStopEventArgs>,
            IHandleMessages<SessionStartedEventArgs>,
            IHandleMessages<SessionEndedEventArgs>,
            IHandleMessages<SessionCapabilitiesChangedEventArgs>,
            IHandleMessages<SessionActivityEventArgs>
    {
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionInfoWebSocketListener"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{SessionInfoWebSocketListener}"/> interface.</param>
        /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
        public SessionInfoWebSocketListener(ILogger<SessionInfoWebSocketListener> logger, ISessionManager sessionManager)
            : base(logger)
        {
            _sessionManager = sessionManager;
        }

        /// <inheritdoc />
        protected override SessionMessageType Type => SessionMessageType.Sessions;

        /// <inheritdoc />
        protected override SessionMessageType StartType => SessionMessageType.SessionsStart;

        /// <inheritdoc />
        protected override SessionMessageType StopType => SessionMessageType.SessionsStop;

        /// <inheritdoc />
        public async Task Handle(SessionActivityEventArgs eventArgs)
        {
            await SendData(false).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Handle(SessionCapabilitiesChangedEventArgs eventArgs)
        {
            await SendData(true).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Handle(PlaybackProgressEventArgs eventArgs)
        {
            await SendData(!eventArgs.IsAutomated).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Handle(PlaybackStopEventArgs eventArgs)
        {
            await SendData(true).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Handle(PlaybackStartEventArgs eventArgs)
        {
            await SendData(true).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Handle(SessionStartedEventArgs e)
        {
            await SendData(true).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Handle(SessionEndedEventArgs e)
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
    }
}
