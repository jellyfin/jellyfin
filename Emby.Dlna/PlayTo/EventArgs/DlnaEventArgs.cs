using System;

namespace Emby.Dlna.PlayTo.EventArgs
{
    /// <summary>
    /// Argument class for Dlna events.
    /// </summary>
    public class DlnaEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DlnaEventArgs"/> class.
        /// </summary>
        /// <param name="id">Id of device who subscribed.</param>
        /// <param name="response">Response XML message received.</param>
        public DlnaEventArgs(string id, string response)
        {
            Id = id;
            Response = response;
        }

        /// <summary>
        /// Gets the DLNA id of this request.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the DNLA response string.
        /// </summary>
        public string Response { get; }
    }
}
