using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Session
{
    public class PlaybackInfo
    {
        public PlaybackInfo()
        {
            QueueableMediaTypes = new List<string>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can seek.
        /// </summary>
        /// <value><c>true</c> if this instance can seek; otherwise, <c>false</c>.</value>
        public bool CanSeek { get; set; }

        /// <summary>
        /// Gets or sets the queueable media types.
        /// </summary>
        /// <value>The queueable media types.</value>
        public List<string> QueueableMediaTypes { get; set; }

        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public BaseItem Item { get; set; }

        /// <summary>
        /// Gets or sets the session id.
        /// </summary>
        /// <value>The session id.</value>
        public Guid SessionId { get; set; }
    }
}
