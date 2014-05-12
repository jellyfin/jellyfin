using System.Collections.Generic;

namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// Class PlaybackStartInfo.
    /// </summary>
    public class PlaybackStartInfo : PlaybackProgressInfo
    {
        public PlaybackStartInfo()
        {
            QueueableMediaTypes = new List<string>();
        }

        /// <summary>
        /// Gets or sets the queueable media types.
        /// </summary>
        /// <value>The queueable media types.</value>
        public List<string> QueueableMediaTypes { get; set; }
    }
}
