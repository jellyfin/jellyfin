#nullable disable

using System;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Subtitles
{
    /// <summary>
    /// An event that occurs when subtitle downloading fails.
    /// </summary>
    public class SubtitleDownloadFailureEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        public BaseItem Item { get; set; }

        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Gets or sets the exception.
        /// </summary>
        public Exception Exception { get; set; }
    }
}
