using System;

namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class UtcTimeResponse.
    /// </summary>
    public class UtcTimeResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UtcTimeResponse"/> class.
        /// </summary>
        /// <param name="requestReceptionTime">The UTC time when request has been received.</param>
        /// <param name="responseTransmissionTime">The UTC time when response has been sent.</param>
        public UtcTimeResponse(DateTime requestReceptionTime, DateTime responseTransmissionTime)
        {
            RequestReceptionTime = requestReceptionTime;
            ResponseTransmissionTime = responseTransmissionTime;
        }

        /// <summary>
        /// Gets the UTC time when request has been received.
        /// </summary>
        /// <value>The UTC time when request has been received.</value>
        public DateTime RequestReceptionTime { get; }

        /// <summary>
        /// Gets the UTC time when response has been sent.
        /// </summary>
        /// <value>The UTC time when response has been sent.</value>
        public DateTime ResponseTransmissionTime { get; }
    }
}
