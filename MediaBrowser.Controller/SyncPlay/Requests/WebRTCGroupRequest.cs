namespace MediaBrowser.Controller.SyncPlay.Requests
{
    /// <summary>
    /// Class WebRTCGroupRequest.
    /// </summary>
    public class WebRTCGroupRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebRTCGroupRequest"/> class.
        /// </summary>
        /// <param name="to">The identifier of the session to whom to send the message.</param>
        /// <param name="newSession">Whether this is a new-session message.</param>
        /// <param name="sessionLeaving">Whether this is a session-leaving message.</param>
        /// <param name="iceCandidate">The ICE candidate.</param>
        /// <param name="offer">The WebRTC offer.</param>
        /// <param name="answer">The WebRTC answer.</param>
        public WebRTCGroupRequest(string to, bool newSession, bool sessionLeaving, string iceCandidate, string offer, string answer)
        {
            To = to;
            NewSession = newSession;
            SessionLeaving = sessionLeaving;
            ICECandidate = iceCandidate;
            Offer = offer;
            Answer = answer;
        }

        /// <summary>
        /// Gets the identifier of the session to whom to send the message.
        /// </summary>
        /// <value>The session identifier.</value>
        public string To { get; }

        /// <summary>
        /// Gets a value indicating whether this is a new-session message.
        /// </summary>
        /// <value>Whether this is a new-session message.</value>
        public bool NewSession { get; }

        /// <summary>
        /// Gets a value indicating whether this is a session-leaving message.
        /// </summary>
        /// <value>Whether this is a session-leaving message.</value>
        public bool SessionLeaving { get; }

        /// <summary>
        /// Gets the ICE candidate.
        /// </summary>
        /// <value>The ICE candidate.</value>
        public string ICECandidate { get; }

        /// <summary>
        /// Gets the WebRTC offer.
        /// </summary>
        /// <value>The WebRTC offer.</value>
        public string Offer { get; }

        /// <summary>
        /// Gets the WebRTC answer.
        /// </summary>
        /// <value>The WebRTC answer.</value>
        public string Answer { get; }
    }
}
