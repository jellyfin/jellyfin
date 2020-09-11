#nullable disable

namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class UtcTimeResponse.
    /// </summary>
    public class UtcTimeResponse
    {
        /// <summary>
        /// Gets or sets the UTC time when request has been received.
        /// </summary>
        /// <value>The UTC time when request has been received.</value>
        public string RequestReceptionTime { get; set; }

        /// <summary>
        /// Gets or sets the UTC time when response has been sent.
        /// </summary>
        /// <value>The UTC time when response has been sent.</value>
        public string ResponseTransmissionTime { get; set; }
    }
}
