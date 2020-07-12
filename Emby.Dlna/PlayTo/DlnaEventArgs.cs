using System;

namespace Emby.Dlna.PlayTo
{
    /// <summary>
    /// Argument class for Dlna events.
    /// </summary>
    public class DlnaEventArgs
    {
        /// <summary>
        /// Constructor for DlnaEventArgs.
        /// </summary>
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
