using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Holds information about a playback progress event
    /// </summary>
    public class PlaybackProgressEventArgs : EventArgs
    {
        public List<User> Users { get; set; }
        public long? PlaybackPositionTicks { get; set; }
        public BaseItem Item { get; set; }
        public string MediaSourceId { get; set; }

        public PlaybackProgressEventArgs()
        {
            Users = new List<User>();
        }
    }

    public class PlaybackStopEventArgs : PlaybackProgressEventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether [played to completion].
        /// </summary>
        /// <value><c>true</c> if [played to completion]; otherwise, <c>false</c>.</value>
        public bool PlayedToCompletion { get; set; }
    }
}
