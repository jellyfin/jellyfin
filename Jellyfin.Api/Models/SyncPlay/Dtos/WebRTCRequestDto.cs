namespace Jellyfin.Api.Models.SyncPlay.Dtos
{
    /// <summary>
    /// Class WebRTCRequestDto.
    /// </summary>
    public class WebRTCRequestDto
    {
        /// <summary>
        /// Gets or sets the identifier of the session to whom to send the message.
        /// </summary>
        /// <value>The session identifier.</value>
        public string? To { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a new-session message.
        /// </summary>
        /// <value>Whether this is a new-session message.</value>
        public bool? NewSession { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a session-leaving message.
        /// </summary>
        /// <value>Whether this is a session-leaving message.</value>
        public bool? SessionLeaving { get; set; }

        /// <summary>
        /// Gets or sets the ICE candidate.
        /// </summary>
        /// <value>The ICE candidate.</value>
        public string? ICECandidate { get; set; }

        /// <summary>
        /// Gets or sets the WebRTC offer.
        /// </summary>
        /// <value>The WebRTC offer.</value>
        public string? Offer { get; set; }

        /// <summary>
        /// Gets or sets the WebRTC answer.
        /// </summary>
        /// <value>The WebRTC answer.</value>
        public string? Answer { get; set; }
    }
}
