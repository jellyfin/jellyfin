using System;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Lyrics
{
    /// <summary>
    /// An event that occurs when subtitle downloading fails.
    /// </summary>
    public class LyricDownloadFailureEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        public required BaseItem Item { get; set; }

        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        public required string Provider { get; set; }

        /// <summary>
        /// Gets or sets the exception.
        /// </summary>
        public required Exception Exception { get; set; }
    }
}
